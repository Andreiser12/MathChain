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
        private readonly FakeCurrentUser _currentUser = new();
        private readonly DashboardContext _context;

        public DashboardContextTests()
        {
            _context = new DashboardContext(_users, _submissions, _grades,
                new InMemoryPracticeGateway(_users, _submissions, _grades),
                new AccessGuard(_currentUser, new InMemoryClassRoomGateway()));
        }

        [Fact]
        public async Task RecordSolvedProblemAsync_Anonymous_ThrowsUnauthorizedException()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest()));
            Assert.Empty(_submissions.Submissions);
        }

        [Fact]
        public async Task RecordSolvedProblemAsync_OtherWalletInRequest_IsRecordedForTokenWallet()
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xme", Role = UserRole.Student });
            _currentUser.As("0xme", UserRole.Student);

            var result = await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { WalletAddress = "0xvictim", FormulaId = "alg-f1" });

            Assert.Equal(1, result.TotalSolved);
            Assert.Equal("0xme", Assert.Single(_submissions.Submissions).StudentWallet);
        }

        [Fact]
        public async Task RecordSolvedProblemAsync_NewWallet_CreatesStudentAndRecordsAttempt()
        {
            _currentUser.As("0xnew", UserRole.Student);

            var result = await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { FormulaId = "alg-f1", Difficulty = "Hard", ExerciseIndex = 2 });

            Assert.Equal(1, result.TotalSolved);
            var user = Assert.Single(_users.Users);
            Assert.Equal("0xnew", user.WalletAddress);
            Assert.Equal(UserRole.Student, user.Role);
            var submission = Assert.Single(_submissions.Submissions);
            Assert.Contains("Formula alg-f1, ExerciseIndex 2, Difficulty Hard", submission.Content);
            Assert.Equal(10.0, Assert.Single(_grades.Grades).Score);
        }

        [Fact]
        public async Task RecordSolvedProblemAsync_ExistingWallet_DoesNotCreateUser()
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xknown", Role = UserRole.Teacher });
            _currentUser.As("0xknown", UserRole.Teacher);

            var result = await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest());

            Assert.Equal(1, result.TotalSolved);
            Assert.Single(_users.Users);
        }

        [Fact]
        public async Task GetPracticeProgressAsync_SolvedExercises_ReturnsSortedIndicesAndNextIndex()
        {
            _currentUser.As("0xa", UserRole.Student);
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { FormulaId = "alg-f1", ExerciseIndex = 4 });
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { FormulaId = "alg-f1", ExerciseIndex = 1 });
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { FormulaId = "alg-f2", ExerciseIndex = 7 });

            var progress = await _context.GetPracticeProgressAsync("alg-f1");

            Assert.Equal(new[] { 1, 4 }, progress.SolvedIndices);
            Assert.Equal(5, progress.LastExerciseIndex);
        }

        [Fact]
        public async Task GetPracticeProgressAsync_OtherStudentsProgress_IsNotVisible()
        {
            _currentUser.As("0xa", UserRole.Student);
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { FormulaId = "alg-f1", ExerciseIndex = 4 });
            _currentUser.As("0xb", UserRole.Student);

            var progress = await _context.GetPracticeProgressAsync("alg-f1");

            Assert.Empty(progress.SolvedIndices);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_CurrentUser_IsMarkedAndRanked()
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xa" });
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xb" });
            _currentUser.As("0xb", UserRole.Student);
            await _context.RecordSolvedProblemAsync(new RecordSolvedProblemRequest { Difficulty = "Medium" });

            var stats = await _context.GetDashboardStatsAsync();

            Assert.Equal(1, stats.UserRank);
            Assert.Equal(1, stats.UserTotalSolved);
            Assert.Equal(1, stats.UserMediumSolved);
            Assert.Equal(100, stats.UserAccuracy);
            Assert.True(stats.Leaderboard.Single(e => e.WalletAddress == "0xb").IsCurrentUser);
        }
    }
}
