using MathChain.Application.Contexts;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Shared.Fakes;

namespace MathChain.Application.UnitTests
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
                new AccessGuard(_currentUser, new InMemoryClassRoomGateway()));
        }

        private void AddSolvedPractice(string wallet, string difficulty)
        {
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                AssignmentId = Guid.Empty,
                StudentWallet = wallet,
                Content = $"Practice: Formula {Guid.NewGuid()}, ExerciseIndex 0, Difficulty {difficulty}, Solution 5",
                SubmittedAt = DateTime.UtcNow
            };
            _submissions.Submissions.Add(submission);
            _grades.Grades.Add(new Grade { Id = Guid.NewGuid(), SubmissionId = submission.Id, Score = 10.0 });
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldThrowUnauthorizedException_WhenTheUserIsAnonymous()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.GetDashboardStatsAsync());
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldMarkAndRankTheCurrentUser_WhenTheUserIsLoggedIn()
        {
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xa" });
            _users.Users.Add(new User { Id = Guid.NewGuid(), WalletAddress = "0xb" });
            _currentUser.As("0xb", UserRole.Student);
            AddSolvedPractice("0xb", "Medium");

            var stats = await _context.GetDashboardStatsAsync();

            Assert.Equal(1, stats.UserRank);
            Assert.Equal(1, stats.UserTotalSolved);
            Assert.Equal(1, stats.UserMediumSolved);
            Assert.Equal(100, stats.UserAccuracy);
            Assert.True(stats.Leaderboard.Single(e => e.WalletAddress == "0xb").IsCurrentUser);
        }
    }
}
