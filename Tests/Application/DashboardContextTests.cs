using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class DashboardContextTests
    {
        private readonly InMemoryUserGateway _users = new();
        private readonly InMemorySubmissionGateway _submissions = new();
        private readonly InMemoryGradeGateway _grades = new();
        private readonly DashboardContext _context;

        public DashboardContextTests()
        {
            _context = new DashboardContext(_users, _submissions, _grades, new InMemoryPracticeGateway(_users, _submissions, _grades));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RecordSolvedProblemAsync_MissingWallet_ThrowsBusinessRuleException(string wallet)
        {
            var request = new RecordSolvedProblemRequest { WalletAddress = wallet };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.RecordSolvedProblemAsync(request));
            Assert.Empty(_submissions.Submissions);
        }

        [Fact]
        public async Task RecordSolvedProblemAsync_NewWallet_CreatesStudentAndRecordsAttempt()
        {
            var request = new RecordSolvedProblemRequest { WalletAddress = " 0xNew ", FormulaId = "alg-f1", Difficulty = "Hard", ExerciseIndex = 2 };

            var result = await _context.RecordSolvedProblemAsync(request);

            Assert.Equal(1, result.TotalSolved);
            var user = Assert.Single(_users.Users);
            Assert.Equal("0xNew", user.WalletAddress);
            Assert.Equal(UserRole.Student, user.Role);
            var submission = Assert.Single(_submissions.Submissions);
            Assert.Contains("Formula alg-f1, ExerciseIndex 2, Difficulty Hard", submission.Content);
            Assert.Equal(10.0, Assert.Single(_grades.Grades).Score);
        }

        [Fact]
        public async Task RecordSolvedProblemAsync_ExistingWallet_DoesNotCreateUser()
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xknown", Role = UserRole.Teacher });

            var result = await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { WalletAddress = "0xknown" });

            Assert.Equal(1, result.TotalSolved);
            Assert.Single(_users.Users);
        }

        [Fact]
        public async Task GetPracticeProgressAsync_SolvedExercises_ReturnsSortedIndicesAndNextIndex()
        {
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { WalletAddress = "0xa", FormulaId = "alg-f1", ExerciseIndex = 4 });
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { WalletAddress = "0xa", FormulaId = "alg-f1", ExerciseIndex = 1 });
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { WalletAddress = "0xa", FormulaId = "alg-f2", ExerciseIndex = 7 });

            var progress = await _context.GetPracticeProgressAsync("0xA", "alg-f1");

            Assert.Equal(new[] { 1, 4 }, progress.SolvedIndices);
            Assert.Equal(5, progress.LastExerciseIndex);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_CurrentUser_IsMarkedAndRanked()
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xa" });
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xb" });
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { WalletAddress = "0xb", Difficulty = "Medium" });

            var stats = await _context.GetDashboardStatsAsync("0xB");

            Assert.Equal(1, stats.UserRank);
            Assert.Equal(1, stats.UserTotalSolved);
            Assert.Equal(1, stats.UserMediumSolved);
            Assert.Equal(100, stats.UserAccuracy);
            Assert.True(stats.Leaderboard.Single(e => e.WalletAddress == "0xb").IsCurrentUser);
        }
    }
}
