using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Blockchain.Certificates;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class CertificateContextTests
    {
        private const string Teacher = "0x1111111111111111111111111111111111111111";
        private const string OtherTeacher = "0x2222222222222222222222222222222222222222";
        private const string Student = "0x3333333333333333333333333333333333333333";
        private const string OtherStudent = "0x4444444444444444444444444444444444444444";
        private const string UngradedStudent = "0x5555555555555555555555555555555555555555";
        private const string Admin = "0x6666666666666666666666666666666666666666";

        private readonly InMemoryClassRoomGateway _classRooms = new();
        private readonly InMemoryCertificateGateway _certificates = new();
        private readonly InMemoryCurriculumGateway _curriculum = new();
        private readonly InMemoryPracticeExerciseGateway _practice = new();
        private readonly FakeCertificateRegistry _registry = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ManualTimeProvider _time = new();
        private readonly CertificateContext _context;
        private readonly ClassRoom _classRoom;

        public CertificateContextTests()
        {
            _context = new CertificateContext(_certificates, _registry, _classRooms, _curriculum, _practice, new AccessGuard(_currentUser, _classRooms), _time);

            _classRoom = new ClassRoom { Id = Guid.NewGuid(), Name = "Algebra XI A", TeacherWallet = Teacher, JoinCode = "ABC234" };
            _classRooms.ClassRooms.Add(_classRoom);
            Enroll(Student, 9);
            Enroll(OtherStudent, 7);
            Enroll(UngradedStudent, null);
            _currentUser.As(Teacher, UserRole.Teacher);
        }

        private Enrollment Enroll(string wallet, int? finalGrade)
        {
            var enrollment = new Enrollment { Id = Guid.NewGuid(), ClassRoomId = _classRoom.Id, StudentWallet = wallet, FinalGrade = finalGrade };
            _classRooms.Enrollments.Add(enrollment);
            return enrollment;
        }

        private Task<List<ClassCertificateDto>> IssueAsync(params string[] wallets)
        {
            return _context.IssueClassCertificatesAsync(_classRoom.Id, wallets.Length == 0 ? null : new IssueCertificatesRequest { StudentWallets = wallets.ToList() });
        }

        private Certificate Stored(string wallet)
        {
            return _certificates.Certificates.Single(c => c.StudentWallet == wallet && c.Status != CertificateStatus.Revoked);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldIssueOneCertificatePerGradedStudentInOneTransaction_WhenTheUserOwnsTheClassRoom()
        {
            var issued = await IssueAsync();

            Assert.Equal(new[] { Student, OtherStudent }, issued.Select(c => c.StudentWallet));
            Assert.All(issued, c => Assert.Equal(CertificateStatus.Issued, c.Status));
            Assert.Equal(1, _registry.IssueCalls);
            Assert.Equal(2, _registry.Tokens.Count);
            Assert.Single(issued.Select(c => c.TransactionHash).Distinct());
            Assert.DoesNotContain(_certificates.Certificates, c => c.StudentWallet == UngradedStudent);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldStoreDataThatReproducesTheOnChainHash_WhenTheCertificatesAreIssued()
        {
            await IssueAsync();

            var certificate = Stored(Student);
            var token = _registry.Tokens.Single(t => t.TokenId == certificate.TokenId);

            Assert.Equal(9, certificate.Grade);
            Assert.Equal("Algebra XI A", certificate.SubjectTitle);
            Assert.Equal(Teacher, certificate.TeacherWallet);
            Assert.Equal(_time.Now.UtcDateTime, certificate.IssuedAt);
            Assert.True(CertificateEncoding.IsBytes32(certificate.Salt));
            Assert.Equal(Student, token.StudentAddress);
            Assert.Equal(CertificateKind.Class, token.Kind);
            Assert.Equal(_classRoom.Id, token.SubjectId);
            Assert.Equal(token.DataHash, CertificateEncoding.ComputeDataHash(new CertificateCommitment
            {
                StudentAddress = Student,
                Kind = CertificateKind.Class,
                SubjectId = _classRoom.Id,
                Grade = 9,
                TeacherAddress = Teacher,
                Salt = certificate.Salt
            }));
            Assert.NotEqual(certificate.Salt, Stored(OtherStudent).Salt);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldHaveNobodyLeftToCertify_WhenItIsCalledASecondTime()
        {
            await IssueAsync();

            await Assert.ThrowsAsync<BusinessRuleException>(() => IssueAsync());

            Assert.Equal(2, _registry.Tokens.Count);
            Assert.Equal(2, _certificates.Certificates.Count);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldIssueOnlyForThoseStudents_WhenStudentsAreSelected()
        {
            var issued = await IssueAsync(OtherStudent.ToUpperInvariant().Replace("0X", "0x"), OtherStudent);

            Assert.Equal(OtherStudent, Assert.Single(issued).StudentWallet);
            Assert.Single(_registry.Tokens);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldThrowBusinessRuleException_WhenASelectedStudentHasNoFinalGrade()
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => IssueAsync(Student, UngradedStudent));

            Assert.Empty(_registry.Tokens);
            Assert.Empty(_certificates.Certificates);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldThrowNotFoundException_WhenASelectedStudentIsNotEnrolled()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => IssueAsync(Admin));
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldThrowBusinessRuleException_WhenASelectedStudentIsAlreadyCertified()
        {
            await IssueAsync(Student);

            await Assert.ThrowsAsync<BusinessRuleException>(() => IssueAsync(Student));

            Assert.Single(_registry.Tokens);
        }

        [Theory]
        [InlineData(Student, UserRole.Student)]
        [InlineData(OtherTeacher, UserRole.Teacher)]
        public async Task IssueClassCertificatesAsync_ShouldThrowForbiddenAndIssueNothing_WhenTheUserDoesNotOwnTheClassRoom(string wallet, UserRole role)
        {
            _currentUser.As(wallet, role);

            await Assert.ThrowsAsync<ForbiddenException>(() => IssueAsync());

            Assert.Empty(_registry.Tokens);
            Assert.Empty(_certificates.Certificates);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldThrowUnauthorizedException_WhenTheUserIsAnonymous()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => IssueAsync());
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldThrowNotFoundException_WhenTheClassRoomDoesNotExist()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.IssueClassCertificatesAsync(Guid.NewGuid(), null));
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldIssueInTheNameOfTheClassTeacher_WhenTheUserIsAnAdmin()
        {
            _currentUser.As(Admin, UserRole.Admin);

            await IssueAsync(Student);

            Assert.Equal(Teacher, Stored(Student).TeacherWallet);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldThrowServiceUnavailableException_WhenTheBlockchainIsNotConfigured()
        {
            _registry.CanIssue = false;

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => IssueAsync());

            Assert.Empty(_certificates.Certificates);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldKeepThePendingRecordAndReuseItsSecretOnRetry_WhenTheNetworkFails()
        {
            _registry.FailBeforeMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => IssueAsync(Student));
            var pending = Stored(Student);
            Assert.Equal(CertificateStatus.Pending, pending.Status);
            Assert.Null(pending.TokenId);

            _registry.FailBeforeMining = false;
            await IssueAsync(Student);

            var issued = Stored(Student);
            Assert.Equal(pending.Id, issued.Id);
            Assert.Equal(pending.Salt, issued.Salt);
            Assert.Equal(CertificateStatus.Issued, issued.Status);
            Assert.Single(_certificates.Certificates);
            Assert.Single(_registry.Tokens);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldAdoptTheOnChainCertificate_WhenTheTransactionWasMinedButTheResponseWasLost()
        {
            _registry.FailAfterMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => IssueAsync(Student));
            Assert.Equal(CertificateStatus.Pending, Stored(Student).Status);

            _registry.FailAfterMining = false;
            var issued = await IssueAsync(Student);

            Assert.Equal(1, _registry.IssueCalls);
            Assert.Single(_registry.Tokens);
            Assert.Equal(CertificateStatus.Issued, Assert.Single(issued).Status);
            Assert.Equal(_registry.Tokens[0].TokenId, Stored(Student).TokenId);
            Assert.Empty(_registry.RevokedTokenIds);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldCommitToTheNewGrade_WhenTheGradeChangesWhilePending()
        {
            _registry.FailBeforeMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => IssueAsync(Student));
            var oldHash = Stored(Student).DataHash;
            _classRooms.Enrollments.Single(e => e.StudentWallet == Student).FinalGrade = 10;

            _registry.FailBeforeMining = false;
            await IssueAsync(Student);

            Assert.Equal(10, Stored(Student).Grade);
            Assert.NotEqual(oldHash, Stored(Student).DataHash);
            Assert.Equal(Stored(Student).DataHash, _registry.Tokens.Single().DataHash);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldReplaceTheCertificate_WhenAnUnknownActiveCertificateExistsOnChain()
        {
            _registry.Tokens.Add(new OnChainCertificate
            {
                TokenId = 1,
                StudentAddress = Student,
                IssuerAddress = FakeCertificateRegistry.IssuerAddress,
                Kind = CertificateKind.Class,
                SubjectId = _classRoom.Id,
                DataHash = "0x" + new string('a', 64)
            });

            await IssueAsync(Student);

            Assert.Equal(new long[] { 1 }, _registry.RevokedTokenIds);
            Assert.Equal(2, Stored(Student).TokenId);
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldSplitIntoSeveralTransactions_WhenTheClassIsLarge()
        {
            for (var index = 0; index < 60; index++)
            {
                Enroll($"0x{index:x40}", 8);
            }

            var issued = await IssueAsync();

            Assert.Equal(62, issued.Count);
            Assert.Equal(2, _registry.IssueCalls);
            Assert.Equal(2, issued.Select(c => c.TransactionHash).Distinct().Count());
            Assert.All(_certificates.Certificates, c => Assert.Equal(CertificateStatus.Issued, c.Status));
        }

        [Fact]
        public async Task IssueClassCertificatesAsync_ShouldStoreALowercaseAddress_WhenTheEnrollmentHasAnUppercaseWallet()
        {
            Enroll("0xABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD", 10);

            await IssueAsync("0xabcdefabcdefabcdefabcdefabcdefabcdefabcd");

            Assert.Equal("0xabcdefabcdefabcdefabcdefabcdefabcdefabcd", Assert.Single(_registry.Tokens).StudentAddress);
        }

        [Fact]
        public async Task RevokeAsync_ShouldRevokeOnChainAndAllowACorrectedCertificate_WhenTheUserOwnsTheClassRoom()
        {
            await IssueAsync(Student);
            var original = Stored(Student);

            await _context.RevokeAsync(original.Id);

            var revoked = _certificates.Certificates.Single(c => c.Id == original.Id);
            Assert.Equal(CertificateStatus.Revoked, revoked.Status);
            Assert.Equal(_time.Now.UtcDateTime, revoked.RevokedAt);
            Assert.True(_registry.Tokens.Single().Revoked);

            _classRooms.Enrollments.Single(e => e.StudentWallet == Student).FinalGrade = 10;
            await IssueAsync(Student);

            Assert.Equal(2, _certificates.Certificates.Count);
            Assert.Equal(10, Stored(Student).Grade);
            Assert.NotEqual(original.TokenId, Stored(Student).TokenId);
        }

        [Theory]
        [InlineData(Student, UserRole.Student)]
        [InlineData(OtherTeacher, UserRole.Teacher)]
        public async Task RevokeAsync_ShouldThrowForbiddenAndKeepTheCertificate_WhenTheUserDoesNotOwnTheClassRoom(string wallet, UserRole role)
        {
            await IssueAsync(Student);
            _currentUser.As(wallet, role);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.RevokeAsync(Stored(Student).Id));

            Assert.False(_registry.Tokens.Single().Revoked);
            Assert.Equal(CertificateStatus.Issued, Stored(Student).Status);
        }

        [Fact]
        public async Task RevokeAsync_ShouldRevokeAnyCertificate_WhenTheUserIsAnAdmin()
        {
            await IssueAsync(Student);
            _currentUser.As(Admin, UserRole.Admin);

            await _context.RevokeAsync(_certificates.Certificates.Single().Id);

            Assert.True(_registry.Tokens.Single().Revoked);
        }

        [Fact]
        public async Task RevokeAsync_ShouldRejectTheRequest_WhenTheCertificateIsUnknownOrAlreadyRevoked()
        {
            await IssueAsync(Student);
            var id = Stored(Student).Id;
            await _context.RevokeAsync(id);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.RevokeAsync(id));
            await Assert.ThrowsAsync<NotFoundException>(() => _context.RevokeAsync(Guid.NewGuid()));
            Assert.Single(_registry.RevokedTokenIds);
        }

        [Fact]
        public async Task RevokeAsync_ShouldRevokeItOnChainToo_WhenAPendingCertificateWasMined()
        {
            _registry.FailAfterMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => IssueAsync(Student));
            _registry.FailAfterMining = false;

            await _context.RevokeAsync(_certificates.Certificates.Single().Id);

            Assert.True(_registry.Tokens.Single().Revoked);
            Assert.Equal(CertificateStatus.Revoked, _certificates.Certificates.Single().Status);
        }

        [Fact]
        public async Task RevokeAsync_ShouldOnlyCloseItInTheDatabase_WhenAPendingCertificateWasNeverMined()
        {
            _registry.FailBeforeMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => IssueAsync(Student));

            await _context.RevokeAsync(_certificates.Certificates.Single().Id);

            Assert.Empty(_registry.RevokedTokenIds);
            Assert.Equal(CertificateStatus.Revoked, _certificates.Certificates.Single().Status);
        }

        [Fact]
        public async Task GetClassCertificatesAsync_ShouldReturnTheStatusOfEveryCertificate_WhenTheUserOwnsTheClassRoom()
        {
            await IssueAsync();

            var certificates = await _context.GetClassCertificatesAsync(_classRoom.Id);

            Assert.Equal(2, certificates.Count);
            Assert.All(certificates, c => Assert.NotNull(c.TokenId));
        }

        [Fact]
        public async Task GetClassCertificatesAsync_ShouldThrowForbiddenException_WhenTheUserIsAStudent()
        {
            _currentUser.As(Student, UserRole.Student);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetClassCertificatesAsync(_classRoom.Id));
        }

        [Fact]
        public async Task GetMyCertificatesAsync_ShouldReturnOnlyTheirOwnCertificatesWithTheVerificationKey_WhenTheUserIsAStudent()
        {
            await IssueAsync();
            _currentUser.As(Student.ToUpperInvariant().Replace("0X", "0x"), UserRole.Student);

            var mine = Assert.Single(await _context.GetMyCertificatesAsync());

            Assert.Equal(Stored(Student).Salt, mine.VerificationKey);
            Assert.Equal("Algebra XI A", mine.SubjectTitle);
            Assert.Equal(9, mine.Grade);
            Assert.Equal(CertificateKind.Class, mine.Kind);
        }

        [Fact]
        public async Task GetMyCertificatesAsync_ShouldHideTheCertificate_WhenItIsStillPending()
        {
            _registry.FailBeforeMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => IssueAsync(Student));
            _currentUser.As(Student, UserRole.Student);

            Assert.Empty(await _context.GetMyCertificatesAsync());
        }

        [Fact]
        public async Task VerifyAsync_ShouldShowOnlyPublicBlockchainFacts_WhenAnAnonymousUserHasNoKey()
        {
            await IssueAsync(Student);
            _currentUser.Anonymous();

            var verification = await _context.VerifyAsync(1, null);

            Assert.True(verification.IsValid);
            Assert.False(verification.DetailsRevealed);
            Assert.Equal(Student, verification.StudentAddress);
            Assert.Equal(FakeCertificateRegistry.IssuerAddress, verification.IssuerAddress);
            Assert.Equal(CertificateKind.Class, verification.Kind);
            Assert.Null(verification.Grade);
            Assert.Null(verification.SubjectTitle);
            Assert.Null(verification.TeacherAddress);
            Assert.Null(verification.DataMatchesBlockchain);
            Assert.Equal(FakeCertificateRegistry.ContractAddress, verification.ContractAddress);
            Assert.Equal(31337, verification.ChainId);
        }

        [Fact]
        public async Task VerifyAsync_ShouldRevealDetailsThatMatchTheBlockchain_WhenTheVerificationKeyIsGiven()
        {
            await IssueAsync(Student);
            _currentUser.Anonymous();

            var verification = await _context.VerifyAsync(1, $"  {Stored(Student).Salt.ToUpperInvariant().Replace("0X", "0x")} ");

            Assert.True(verification.IsValid);
            Assert.True(verification.DetailsRevealed);
            Assert.True(verification.DataMatchesBlockchain);
            Assert.Equal(9, verification.Grade);
            Assert.Equal("Algebra XI A", verification.SubjectTitle);
            Assert.Equal(Teacher, verification.TeacherAddress);
        }

        [Fact]
        public async Task VerifyAsync_ShouldReportAMismatch_WhenTheGradeChangedInTheDatabaseAfterIssuing()
        {
            await IssueAsync(Student);
            var key = Stored(Student).Salt;
            _certificates.Certificates.Single().Grade = 10;

            var verification = await _context.VerifyAsync(1, key);

            Assert.False(verification.IsValid);
            Assert.False(verification.DataMatchesBlockchain);
            Assert.False(verification.Revoked);
        }

        [Fact]
        public async Task VerifyAsync_ShouldReportAMismatch_WhenTheTeacherChangedInTheDatabaseAfterIssuing()
        {
            await IssueAsync(Student);
            var key = Stored(Student).Salt;
            _certificates.Certificates.Single().TeacherWallet = OtherTeacher;

            Assert.False((await _context.VerifyAsync(1, key)).DataMatchesBlockchain);
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowBusinessRuleExceptionWithoutRevealingDetails_WhenTheKeyIsWrong()
        {
            await IssueAsync();

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.VerifyAsync(1, Stored(OtherStudent).Salt));
            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.VerifyAsync(1, "0x" + new string('0', 64)));
        }

        [Fact]
        public async Task VerifyAsync_ShouldReportTheCertificateAsInvalid_WhenItWasRevoked()
        {
            await IssueAsync(Student);
            var certificate = Stored(Student);
            await _context.RevokeAsync(certificate.Id);

            var withoutKey = await _context.VerifyAsync(1, null);
            var withKey = await _context.VerifyAsync(1, certificate.Salt);

            Assert.True(withoutKey.Revoked);
            Assert.False(withoutKey.IsValid);
            Assert.False(withKey.IsValid);
            Assert.True(withKey.DataMatchesBlockchain);
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowNotFoundException_WhenTheTokenDoesNotExist()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.VerifyAsync(42, null));
        }

        [Fact]
        public async Task VerifyAsync_ShouldThrowServiceUnavailableException_WhenTheBlockchainIsNotConfigured()
        {
            _registry.CanRead = false;

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.VerifyAsync(1, null));
        }

        [Fact]
        public async Task GetSetupAsync_ShouldReturnTheIssuerStatusAndTheGrantRoleCallData_WhenTheUserIsAnAdmin()
        {
            _currentUser.As(Admin, UserRole.Admin);
            _registry.HasIssuerRole = false;

            var setup = await _context.GetSetupAsync();

            Assert.True(setup.CanIssue);
            Assert.False(setup.HasIssuerRole);
            Assert.Equal(FakeCertificateRegistry.IssuerAddress, setup.IssuerAddress);
            Assert.Equal(0.005m, setup.IssuerBalance);
            Assert.StartsWith("0x2f2ff15d", setup.GrantIssuerRoleCallData);
            Assert.EndsWith(FakeCertificateRegistry.IssuerAddress[2..], setup.GrantIssuerRoleCallData);
        }

        [Fact]
        public async Task GetSetupAsync_ShouldReportThatCertificatesCannotBeIssued_WhenThereIsNoIssuerKey()
        {
            _currentUser.As(Admin, UserRole.Admin);
            _registry.CanIssue = false;

            var setup = await _context.GetSetupAsync();

            Assert.False(setup.CanIssue);
            Assert.Null(setup.IssuerAddress);
            Assert.Null(setup.GrantIssuerRoleCallData);
        }

        [Fact]
        public async Task GetSetupAsync_ShouldThrowForbiddenException_WhenTheUserIsATeacher()
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetSetupAsync());
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnTheNetworkBalance_WhenTheWalletIsTheirOwn()
        {
            _registry.Balances[Teacher] = 0.25m;

            Assert.Equal(0.25m, await _context.GetBalanceAsync(Teacher.ToUpperInvariant().Replace("0X", "0x")));
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldThrowForbiddenException_WhenTheWalletBelongsToSomeoneElse()
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => _context.GetBalanceAsync(Student));
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnZero_WhenTheBlockchainIsNotConfigured()
        {
            _registry.CanRead = false;

            Assert.Equal(0m, await _context.GetBalanceAsync(Teacher));
        }
    }
}
