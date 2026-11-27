using System.Text.Json;
using MathChain.Application.Contexts;
using MathChain.Blockchain.Certificates;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class ChapterCertificateTests
    {
        private const string Student = "0x3333333333333333333333333333333333333333";
        private const string OtherStudent = "0x4444444444444444444444444444444444444444";
        private const string Teacher = "0x1111111111111111111111111111111111111111";
        private const string Admin = "0x6666666666666666666666666666666666666666";
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
        private readonly FakeCertificateRegistry _registry = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ManualTimeProvider _time = new();
        private readonly CertificateContext _context;
        private readonly CurriculumChapter _chapter;
        private readonly CurriculumFormula _first;
        private readonly CurriculumFormula _second;

        public ChapterCertificateTests()
        {
            _context = new CertificateContext(_certificates, _registry, _classRooms, _curriculum, _practice, new AccessGuard(_currentUser, _classRooms), _time);

            _chapter = new CurriculumChapter { Id = Guid.NewGuid(), Title = "Formule de calcul prescurtat", ChapterSlug = "calcul_prescurtat", Version = 1 };
            _curriculum.Chapters.Add(_chapter);
            _first = AddFormula("Patratul sumei", TwoExercises);
            _second = AddFormula("Diferenta patratelor", TwoExercises);
            _currentUser.As(Student, UserRole.Student);
        }

        private CurriculumFormula AddFormula(string title, string? exercisesJson, Guid? chapterId = null)
        {
            var formula = new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = chapterId ?? _chapter.Id, Title = title, ExercisesJson = exercisesJson, Order = _curriculum.Formulas.Count };
            _curriculum.Formulas.Add(formula);
            return formula;
        }

        private void SolveEverything(string wallet = Student)
        {
            foreach (var formula in new[] { _first, _second })
            {
                _practice.MarkSolved(wallet, formula.Id, 0);
                _practice.MarkSolved(wallet, formula.Id, 1);
            }
        }

        [Fact]
        public async Task GetChapterProgressAsync_ShouldCountTheSolvedExercisesPerFormula_WhenTheStudentHasSolvedSomeExercises()
        {
            _practice.MarkSolved(Student, _first.Id, 0);
            _practice.MarkSolved(Student, _first.Id, 0);
            _practice.MarkSolved(Student, _first.Id, 1);
            _practice.MarkSolved(Student, _second.Id, 1);
            _practice.MarkSolved(OtherStudent, _second.Id, 0);

            var progress = await _context.GetChapterProgressAsync(_chapter.Id);

            Assert.True(progress.CertificatesAvailable);
            Assert.False(progress.Completed);
            Assert.Equal(4, progress.TotalExercises);
            Assert.Equal(3, progress.SolvedExercises);
            Assert.Equal(new[] { 2, 1 }, progress.Formulas.Select(f => f.SolvedExercises));
            Assert.Null(progress.Certificate);
        }

        [Fact]
        public async Task GetChapterProgressAsync_ShouldMakeCertificatesUnavailable_WhenAFormulaUsesGenericExercises()
        {
            AddFormula("Teorie", JsonSerializer.Serialize(new[] { new { wolframQuery = "simplify |x|\\geq 0", latexTemplate = "|x|\\geq 0" } }));

            var progress = await _context.GetChapterProgressAsync(_chapter.Id);

            Assert.False(progress.CertificatesAvailable);
            Assert.True(progress.Formulas.Last().UsesGenericExercises);
        }

        [Fact]
        public async Task GetChapterProgressAsync_ShouldMakeCertificatesUnavailable_WhenTheChapterHasNoFormulas()
        {
            var empty = new CurriculumChapter { Id = Guid.NewGuid(), Title = "Gol", ChapterSlug = "gol", Version = 1 };
            _curriculum.Chapters.Add(empty);

            var progress = await _context.GetChapterProgressAsync(empty.Id);

            Assert.False(progress.CertificatesAvailable);
            Assert.Equal(0, progress.TotalExercises);
        }

        [Fact]
        public async Task GetChapterProgressAsync_ShouldThrowNotFoundException_WhenTheChapterDoesNotExist()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetChapterProgressAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldIssueACertificateForTheStudent_WhenEveryExerciseIsSolved()
        {
            SolveEverything();

            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(CertificateKind.Chapter, certificate.Kind);
            Assert.Equal(_chapter.Id, certificate.SubjectId);
            Assert.Equal("Formule de calcul prescurtat", certificate.SubjectTitle);
            Assert.Equal(CertificateStatus.Issued, certificate.Status);
            Assert.Equal(4, certificate.Grade);
            var token = Assert.Single(_registry.Tokens);
            Assert.Equal(Student, token.StudentAddress);
            Assert.Equal(CertificateKind.Chapter, token.Kind);
            Assert.Equal(token.DataHash, CertificateEncoding.ComputeDataHash(new CertificateCommitment
            {
                StudentAddress = Student,
                Kind = CertificateKind.Chapter,
                SubjectId = _chapter.Id,
                Grade = 4,
                TeacherAddress = NoTeacher,
                Salt = certificate.VerificationKey
            }));
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldShowTheCertificateInTheProgressAndLetItBeVerified_WhenItIsClaimed()
        {
            SolveEverything();
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            var progress = await _context.GetChapterProgressAsync(_chapter.Id);
            _currentUser.Anonymous();
            var verification = await _context.VerifyAsync(certificate.TokenId!.Value, certificate.VerificationKey);

            Assert.Equal(certificate.Id, progress.Certificate!.Id);
            Assert.True(verification.IsValid);
            Assert.True(verification.DataMatchesBlockchain);
            Assert.Equal(CertificateKind.Chapter, verification.Kind);
            Assert.Equal("Formule de calcul prescurtat", verification.SubjectTitle);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldThrowBusinessRuleException_WhenNotEverythingIsSolved()
        {
            SolveEverything();
            _practice.Exercises.RemoveAt(_practice.Exercises.Count - 1);

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));

            Assert.Contains("3/4", exception.Message);
            Assert.Empty(_registry.Tokens);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldIgnoreTheExercises_WhenTheyWereSolvedByAnotherStudent()
        {
            SolveEverything(OtherStudent);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldThrowBusinessRuleException_WhenTheChapterHasGenericExercises()
        {
            var generic = AddFormula("Teorie", null);
            SolveEverything();
            for (var index = 0; index < 9; index++)
            {
                _practice.MarkSolved(Student, generic.Id, index);
            }

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));

            Assert.Contains("own exercises", exception.Message);
            Assert.Empty(_registry.Tokens);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldThrowBusinessRuleException_WhenTheCertificateIsClaimedASecondTime()
        {
            SolveEverything();
            await _context.ClaimChapterCertificateAsync(_chapter.Id);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));

            Assert.Single(_registry.Tokens);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldRetryWithTheSameRecord_WhenTheNetworkFails()
        {
            SolveEverything();
            _registry.FailBeforeMining = true;
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));
            var pending = Assert.Single(_certificates.Certificates);

            _registry.FailBeforeMining = false;
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(pending.Id, certificate.Id);
            Assert.Equal(pending.Salt, certificate.VerificationKey);
            Assert.Single(_registry.Tokens);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldIssueSeparateCertificates_WhenTwoStudentsClaimTheSameChapter()
        {
            SolveEverything();
            SolveEverything(OtherStudent);

            await _context.ClaimChapterCertificateAsync(_chapter.Id);
            _currentUser.As(OtherStudent, UserRole.Student);
            await _context.ClaimChapterCertificateAsync(_chapter.Id);

            Assert.Equal(new[] { Student, OtherStudent }, _registry.Tokens.Select(t => t.StudentAddress));
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldThrowForbiddenException_WhenTheUserIsATeacher()
        {
            SolveEverything(Teacher);
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldThrowUnauthorizedException_WhenTheUserIsAnonymous()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldThrowServiceUnavailableException_WhenTheBlockchainIsNotConfigured()
        {
            SolveEverything();
            _registry.CanIssue = false;

            await Assert.ThrowsAsync<ServiceUnavailableException>(() => _context.ClaimChapterCertificateAsync(_chapter.Id));
        }

        [Fact]
        public async Task RevokeAsync_ShouldLetOnlyAnAdminRevokeAndTheStudentClaimAgain_WhenTheCertificateIsForAChapter()
        {
            SolveEverything();
            var certificate = await _context.ClaimChapterCertificateAsync(_chapter.Id);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.RevokeAsync(certificate.Id));

            _currentUser.As(Admin, UserRole.Admin);
            await _context.RevokeAsync(certificate.Id);
            Assert.True(_registry.Tokens.Single().Revoked);

            _currentUser.As(Student, UserRole.Student);
            var renewed = await _context.ClaimChapterCertificateAsync(_chapter.Id);
            Assert.NotEqual(certificate.TokenId, renewed.TokenId);
        }
    }
}
