using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Gateways;

namespace MathChain.Application.Contexts
{
    public class DashboardContext : IDashboardContext
    {
        private const int LeaderboardSize = 10;

        private readonly IUserGateway _userGateway;
        private readonly ISubmissionGateway _submissionGateway;
        private readonly IGradeGateway _gradeGateway;
        private readonly AccessGuard _access;

        public DashboardContext(IUserGateway userGateway, ISubmissionGateway submissionGateway,
            IGradeGateway gradeGateway, AccessGuard access)
        {
            _userGateway = userGateway;
            _submissionGateway = submissionGateway;
            _gradeGateway = gradeGateway;
            _access = access;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var normalizedWallet = _access.RequireWallet();

            var users = await _userGateway.GetAllAsync();
            var submissions = await _submissionGateway.GetAllAsync();
            var grades = await _gradeGateway.GetAllAsync();
            var gradesBySubmissionId = grades.ToDictionary(g => g.SubmissionId);

            var submissionsByWallet = submissions
                .Where(s => !string.IsNullOrWhiteSpace(s.StudentWallet))
                .GroupBy(s => s.StudentWallet.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.ToList());

            var solvers = new List<(string Wallet, int Solved, int Hard, int Medium, int Easy, int Accuracy)>();

            foreach (var user in users)
            {
                var wallet = user.WalletAddress.Trim().ToLowerInvariant();
                solvers.Add(BuildSolverStats(user.WalletAddress, wallet, submissionsByWallet, gradesBySubmissionId));
            }

            if (!string.IsNullOrEmpty(normalizedWallet) && !solvers.Any(s => s.Wallet.Trim().ToLowerInvariant() == normalizedWallet))
            {
                solvers.Add(BuildSolverStats(normalizedWallet, normalizedWallet, submissionsByWallet, gradesBySubmissionId));
            }

            var ranked = solvers
                .OrderByDescending(s => s.Solved)
                .ThenBy(s => s.Wallet)
                .ToList();

            var leaderboard = new List<LeaderboardEntryDto>();
            for (int i = 0; i < Math.Min(LeaderboardSize, ranked.Count); i++)
            {
                leaderboard.Add(new LeaderboardEntryDto
                {
                    Position = i + 1,
                    WalletAddress = ranked[i].Wallet,
                    ProblemsSolved = ranked[i].Solved,
                    IsCurrentUser = !string.IsNullOrEmpty(normalizedWallet) && ranked[i].Wallet.Trim().ToLowerInvariant() == normalizedWallet
                });
            }

            int globalHard = ranked.Sum(r => r.Hard);
            int globalMedium = ranked.Sum(r => r.Medium);
            int globalEasy = ranked.Sum(r => r.Easy);
            int globalTotal = globalHard + globalMedium + globalEasy;

            var currentUser = ranked.FirstOrDefault(r => !string.IsNullOrEmpty(normalizedWallet) && r.Wallet.Trim().ToLowerInvariant() == normalizedWallet);
            int userRank = currentUser != default ? (ranked.IndexOf(currentUser) + 1) : 1;

            return new DashboardStatsDto
            {
                GlobalTotalSolved = globalTotal,
                GlobalHardSolved = globalHard,
                GlobalMediumSolved = globalMedium,
                GlobalEasySolved = globalEasy,
                UserHardSolved = currentUser != default ? currentUser.Hard : 0,
                UserMediumSolved = currentUser != default ? currentUser.Medium : 0,
                UserEasySolved = currentUser != default ? currentUser.Easy : 0,
                UserTotalSolved = currentUser != default ? currentUser.Solved : 0,
                UserAccuracy = currentUser != default ? currentUser.Accuracy : 0,
                UserRank = userRank,
                Leaderboard = leaderboard
            };
        }

        private static (string Wallet, int Solved, int Hard, int Medium, int Easy, int Accuracy) BuildSolverStats(
            string displayWallet,
            string normalizedWallet,
            Dictionary<string, List<Submission>> submissionsByWallet,
            Dictionary<Guid, Grade> gradesBySubmissionId)
        {
            int solved = 0;
            int hard = 0, medium = 0, easy = 0;
            int accuracy = 0;

            if (submissionsByWallet.TryGetValue(normalizedWallet, out var userSubmissions) && userSubmissions.Any())
            {
                solved = userSubmissions.Count;

                int recordedHard = userSubmissions.Count(s => s.Content != null && s.Content.Contains("Difficulty Hard", StringComparison.OrdinalIgnoreCase));
                int recordedMedium = userSubmissions.Count(s => s.Content != null && s.Content.Contains("Difficulty Medium", StringComparison.OrdinalIgnoreCase));
                int recordedEasy = userSubmissions.Count(s => s.Content != null && s.Content.Contains("Difficulty Easy", StringComparison.OrdinalIgnoreCase));

                if (recordedHard + recordedMedium + recordedEasy > 0)
                {
                    hard = recordedHard;
                    medium = recordedMedium;
                    easy = recordedEasy + (solved - recordedHard - recordedMedium - recordedEasy);
                }
                else
                {
                    hard = solved / 3;
                    medium = solved / 3;
                    easy = solved - hard - medium;
                }

                var userGrades = userSubmissions
                    .Where(s => gradesBySubmissionId.ContainsKey(s.Id))
                    .Select(s => gradesBySubmissionId[s.Id])
                    .ToList();

                if (userGrades.Any())
                {
                    accuracy = (int)Math.Round(userGrades.Average(g => g.Score) * 10);
                    if (accuracy > 100) accuracy = 100;
                    if (accuracy < 0) accuracy = 0;
                }
            }

            return (displayWallet, solved, hard, medium, easy, accuracy);
        }
    }
}
