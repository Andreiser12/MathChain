using System.Text.Json;
using MathChain.Application.Contexts;
using MathChain.Blockchain.Certificates;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class SmartAccountCertificateTests
    {
        private const string MetaMaskStudent = "0x3333333333333333333333333333333333333333";
        private const string NoTeacher = "0x0000000000000000000000000000000000000000";

        private static readonly string TwoExercises = JsonSerializer.Serialize(new object[]
        {
            new { difficulty = "Easy", wolframQuery = "expand (x + {a})^2", latexTemplate = "(x + {a})^2", variables = new { a = new[] { 2, 5 } } },
            new { difficulty = "Hard", wolframQuery = "factor x^2 - {a*a}", latexTemplate = "x^2 - {a*a}", variables = new { a = new[] { 2, 9 } } }
        });

        private readonly InMemoryClassRoomGateway _classRooms = new();
        private readonly InMemoryCertificateGateway _certificates = new();
        private readonly InMemoryCurriculumGateway _curriculum = new();
        private readonly InMemoryPracticeExerciseGateway _practice = new();
        private readonly InMemoryUserGateway _users = new();
        private readonly FakeCertificateRegistry _registry = new();
        private readonly FakeSmartAccountService _smartAccounts;
        private readonly FakeCustodialAddressProvider _custody = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ManualTimeProvider _time = new();
        private readonly CertificateContext _context;
        private readonly CurriculumChapter _chapter;
        private readonly User _googleStudent;
        private readonly string _googleWallet;
        private readonly string _googleAccount;

        public SmartAccountCertificateTests()
        {
            _smartAccounts = new FakeSmartAccountService(_registry);
            _context = new CertificateContext(_certificates, _registry, _classRooms, _curriculum, _practice,
                new AccessGuard(_currentUser, _classRooms), _time, _smartAccounts, _users, _custody);

            _chapter = new CurriculumChapter { Id = Guid.NewGuid(), Title = "Formule de calcul prescurtat", ChapterSlug = "calcul_prescurtat", Version = 1 };
            _curriculum.Chapters.Add(_chapter);
            _curriculum.Formulas.Add(new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = _chapter.Id, Title = "Patratul sumei", ExercisesJson = TwoExercises });

            var googleId = Guid.NewGuid();
            _googleWallet = _custody.GetOwnerAddress(googleId);
            _googleAccount = FakeSmartAccountService.AccountOf(_googleWallet);
            _googleStudent = new User { Id = googleId, WalletAddress = _googleWallet, Role = UserRole.Student };
            _users.Users.Add(_googleStudent);
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = MetaMaskStudent, Role = UserRole.Student });
        }

        private void SolveEverything(string wallet)
        {
            foreach (var formula in _curriculum.Formulas)
            {
                _practice.MarkSolved(wallet, formula.Id, 0);
                _practice.MarkSolved(wallet, formula.Id, 1);
            }
        }

        private void SignInAsGoogleStudent()
        {
            _currentUser.As(_googleWallet, UserRole.Student);
            SolveEverything(_googleWallet);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_GoogleStudent_IsClaimedByTheSmartAccount()
        {
            SignInAsGoogleStudent();

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(CertificateStatus.Issued, certificate.Status);
            Assert.Equal(_googleAccount, certificate.HolderAddress);
            Assert.Equal(0, _registry.IssueCalls);
            var claim = Assert.Single(_smartAccounts.Claims);
            Assert.Equal(_googleStudent.Id, claim.UserId);
            Assert.Equal(_googleWallet, claim.OwnerAddress);
            Assert.Equal(CertificateKind.Chapter, claim.Kind);
            Assert.Equal(_chapter.Id, claim.SubjectId);
            var token = Assert.Single(_registry.Tokens);
            Assert.Equal(_googleAccount, token.StudentAddress);
            Assert.Equal(certificate.TokenId, token.TokenId);
            Assert.Equal(token.DataHash, CertificateEncoding.ComputeDataHash(new CertificateCommitment
            {
                StudentAddress = _googleAccount,
                Kind = CertificateKind.Chapter,
                SubjectId = _chapter.Id,
                Grade = 2,
                TeacherAddress = NoTeacher,
                Salt = certificate.VerificationKey
            }));
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_GoogleStudent_KeepsTheOwnerAsStudentWallet()
        {
            SignInAsGoogleStudent();

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);
            var stored = Assert.Single(_certificates.Certificates);
            var progress = await _context.GetChapterProgressAsync(_chapter.Id);
            var mine = await _context.GetMyCertificatesAsync();

            Assert.Equal(_googleWallet, stored.StudentWallet);
            Assert.Equal(_googleAccount, stored.HolderAddress);
            Assert.Equal(certificate.Id, progress.Certificate!.Id);
            Assert.Equal(_googleAccount, Assert.Single(mine).HolderAddress);
        }

        [Fact]
        public async Task VerifyAsync_CertificateHeldBySmartAccount_MatchesTheBlockchain()
        {
            SignInAsGoogleStudent();
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);
            _currentUser.Anonymous();

            var verification = await _context.VerifyAsync(certificate.TokenId!.Value, certificate.VerificationKey);

            Assert.True(verification.IsValid);
            Assert.True(verification.DataMatchesBlockchain);
            Assert.Equal(_googleAccount, verification.StudentAddress);
        }

        [Fact]
        public async Task VerifyAsync_TokenOfAnotherAccountWithTheSameData_DoesNotMatch()
        {
            SignInAsGoogleStudent();
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);
            _registry.Tokens.Single().StudentAddress = _googleWallet;
            _currentUser.Anonymous();

            var verification = await _context.VerifyAsync(certificate.TokenId!.Value, certificate.VerificationKey);

            Assert.False(verification.DataMatchesBlockchain);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_MetaMaskStudent_UsesThePlatformIssuer()
        {
            _currentUser.As(MetaMaskStudent, UserRole.Student);
            SolveEverything(MetaMaskStudent);

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Null(certificate.HolderAddress);
            Assert.Equal(1, _registry.IssueCalls);
            Assert.Empty(_smartAccounts.Claims);
            Assert.Equal(MetaMaskStudent, Assert.Single(_registry.Tokens).StudentAddress);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_GoogleStudentWithLinkedWallet_UsesThePlatformIssuer()
        {
            _googleStudent.WalletAddress = MetaMaskStudent;
            _users.Users.RemoveAll(u => u.Id != _googleStudent.Id);
            _currentUser.As(MetaMaskStudent, UserRole.Student);
            SolveEverything(MetaMaskStudent);

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Null(certificate.HolderAddress);
            Assert.Empty(_smartAccounts.Claims);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_SmartAccountsNotConfigured_UsesThePlatformIssuer()
        {
            _smartAccounts.IsConfigured = false;
            SignInAsGoogleStudent();

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Null(certificate.HolderAddress);
            Assert.Empty(_smartAccounts.Claims);
            Assert.Equal(_googleWallet, Assert.Single(_registry.Tokens).StudentAddress);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_NetworkFailsBeforeMining_KeepsAPendingCertificateAndRetries()
        {
            SignInAsGoogleStudent();
            _smartAccounts.FailBeforeMining = true;

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));

            var pending = Assert.Single(_certificates.Certificates);
            Assert.Equal(CertificateStatus.Pending, pending.Status);
            Assert.Empty(_registry.Tokens);

            _smartAccounts.FailBeforeMining = false;
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(pending.Id, certificate.Id);
            Assert.Equal(CertificateStatus.Issued, certificate.Status);
            Assert.Single(_certificates.Certificates);
            Assert.Single(_registry.Tokens);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ResponseLostAfterMining_RecoversTheMinedToken()
        {
            SignInAsGoogleStudent();
            _smartAccounts.FailAfterMining = true;

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));

            _smartAccounts.FailAfterMining = false;
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(CertificateStatus.Issued, certificate.Status);
            Assert.Equal(1, certificate.TokenId);
            Assert.Single(_smartAccounts.Claims);
            Assert.Empty(_smartAccounts.RevokedTokenIds);
            Assert.Single(_registry.Tokens);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_UnknownActiveToken_IsRevokedBeforeANewClaim()
        {
            SignInAsGoogleStudent();
            _registry.Tokens.Add(new OnChainCertificate
            {
                TokenId = 1,
                StudentAddress = _googleAccount,
                IssuerAddress = FakeCertificateRegistry.IssuerAddress,
                Kind = CertificateKind.Chapter,
                SubjectId = _chapter.Id,
                DataHash = "0x" + new string('7', 64),
                IssuedAt = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc)
            });

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(new long[] { 1 }, _smartAccounts.RevokedTokenIds);
            Assert.Equal(2, certificate.TokenId);
            Assert.Equal(CertificateStatus.Issued, certificate.Status);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ActiveTokenWithOtherData_IsNotAdoptedByThePendingCertificate()
        {
            SignInAsGoogleStudent();
            var pending = new Certificate
            {
                Id = Guid.NewGuid(),
                Kind = CertificateKind.Chapter,
                SubjectId = _chapter.Id,
                StudentWallet = _googleWallet,
                HolderAddress = _googleAccount,
                Status = CertificateStatus.Pending,
                DataHash = "0x" + new string('1', 64),
                Salt = "0x" + new string('2', 64)
            };
            _certificates.Certificates.Add(pending);
            _registry.Tokens.Add(new OnChainCertificate
            {
                TokenId = 1,
                StudentAddress = _googleAccount,
                IssuerAddress = FakeCertificateRegistry.IssuerAddress,
                Kind = CertificateKind.Chapter,
                SubjectId = _chapter.Id,
                DataHash = "0x" + new string('7', 64),
                IssuedAt = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc)
            });

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(pending.Id, certificate.Id);
            Assert.Equal(new long[] { 1 }, _smartAccounts.RevokedTokenIds);
            Assert.Equal(2, certificate.TokenId);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_GoogleStudentSecondClaim_ThrowsBusinessRuleException()
        {
            SignInAsGoogleStudent();
            await _context.ClaimChapterCertificateAsync(_chapter.Id);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));

            Assert.Single(_smartAccounts.Claims);
        }

        [Fact]
        public async Task RevokeAsync_CertificateClaimedBySmartAccount_IsRevokedThroughTheClaimsContract()
        {
            SignInAsGoogleStudent();
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);
            _currentUser.As("0x6666666666666666666666666666666666666666", UserRole.Admin);

            await _context.RevokeAsync(certificate.Id);

            Assert.Equal(new long[] { certificate.TokenId!.Value }, _smartAccounts.RevokedTokenIds);
            Assert.Empty(_registry.RevokedTokenIds);
            Assert.Equal(CertificateStatus.Revoked, Assert.Single(_certificates.Certificates).Status);
        }

        [Fact]
        public async Task RevokeAsync_PendingClaimMinedWithoutTokenId_FindsTheTokenOfTheSmartAccount()
        {
            SignInAsGoogleStudent();
            _smartAccounts.FailAfterMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));
            _currentUser.As("0x6666666666666666666666666666666666666666", UserRole.Admin);

            await _context.RevokeAsync(_certificates.Certificates.Single().Id);

            Assert.Equal(new long[] { 1 }, _smartAccounts.RevokedTokenIds);
            Assert.True(_registry.Tokens.Single().Revoked);
        }

        [Fact]
        public async Task RevokeAsync_SmartAccountsNoLongerConfigured_ThrowsServiceUnavailableException()
        {
            SignInAsGoogleStudent();
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);
            _smartAccounts.IsConfigured = false;
            _currentUser.As("0x6666666666666666666666666666666666666666", UserRole.Admin);

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.RevokeAsync(certificate.Id));

            Assert.False(_registry.Tokens.Single().Revoked);
        }

        [Fact]
        public async Task RevokeAsync_MetaMaskCertificate_IsRevokedByThePlatformIssuer()
        {
            _currentUser.As(MetaMaskStudent, UserRole.Student);
            SolveEverything(MetaMaskStudent);
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);
            _currentUser.As("0x6666666666666666666666666666666666666666", UserRole.Admin);

            await _context.RevokeAsync(certificate.Id);

            Assert.Equal(new long[] { 1 }, _registry.RevokedTokenIds);
            Assert.Empty(_smartAccounts.RevokedTokenIds);
        }

        [Fact]
        public async Task GetMySmartAccountAsync_GoogleStudent_ReturnsTheCounterfactualAccount()
        {
            _currentUser.As(_googleWallet, UserRole.Student);

            var before = await _context.GetMySmartAccountAsync();
            SolveEverything(_googleWallet);
            await _context.ClaimChapterCertificateAsync(_chapter.Id);
            var after = await _context.GetMySmartAccountAsync();

            Assert.True(before.Available);
            Assert.Equal(_googleAccount, before.Address);
            Assert.False(before.Deployed);
            Assert.Equal($"https://explorer.example/address/{_googleAccount}", before.ExplorerUrl);
            Assert.True(after.Deployed);
        }

        [Fact]
        public async Task GetMySmartAccountAsync_MetaMaskStudent_IsNotAvailable()
        {
            _currentUser.As(MetaMaskStudent, UserRole.Student);

            var account = await _context.GetMySmartAccountAsync();

            Assert.False(account.Available);
            Assert.Null(account.Address);
        }

        [Fact]
        public async Task GetSetupAsync_IncludesTheSmartAccountContracts()
        {
            _currentUser.As("0x6666666666666666666666666666666666666666", UserRole.Admin);

            var setup = await _context.GetSetupAsync();

            Assert.True(setup.SmartAccountsConfigured);
            Assert.Equal("0x0000000071727de22e5e9d8baf0edac6f37da032", setup.EntryPointAddress);
            Assert.Equal(0.02m, setup.PaymasterDeposit);
            Assert.True(setup.ClaimsCanIssue);
            Assert.Equal("0xd0e30db0", setup.PaymasterDepositCallData);
        }
    }
}
