using System.Text.RegularExpressions;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;

namespace MathChain.Application.Contexts
{
    public class DashboardContext : IDashboardContext
    {
        private const int LeaderboardSize = 10;

        private readonly IUserGateway _userGateway;
        private readonly ISubmissionGateway _submissionGateway;
        private readonly IGradeGateway _gradeGateway;
        private readonly IPracticeGateway _practiceGateway;

        public DashboardContext(IUserGateway userGateway, ISubmissionGateway submissionGateway,
            IGradeGateway gradeGateway, IPracticeGateway practiceGateway)
        {
            _userGateway = userGateway;
            _submissionGateway = submissionGateway;
            _gradeGateway = gradeGateway;
            _practiceGateway = practiceGateway;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(string? walletAddress)
        {
            var normalizedWallet = (walletAddress ?? "").Trim().ToLowerInvariant();

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
                solvers.Add(BuildSolverStats(walletAddress!, normalizedWallet, submissionsByWallet, gradesBySubmissionId));
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

        public async Task<RecordSolvedProblemResultDto> RecordSolvedProblemAsync(RecordSolvedProblemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                throw new BusinessRuleException("Wallet address is required.");
            }

            var cleanWallet = request.WalletAddress.Trim();

            var existingUser = await _userGateway.GetByWalletAddressAsync(cleanWallet);
            User? newUser = null;

            if (existingUser == null)
            {
                newUser = new User
                {
                    Id = Guid.NewGuid(),
                    WalletAddress = cleanWallet,
                    Role = UserRole.Student,
                    CreatedAt = DateTime.UtcNow
                };
            }

            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                AssignmentId = Guid.Empty,
                StudentWallet = cleanWallet,
                Content = $"Practice: Formula {request.FormulaId ?? "General"}, ExerciseIndex {request.ExerciseIndex ?? 0}, Difficulty {request.Difficulty ?? "Easy"}, Solution {request.Solution ?? ""}",
                SubmittedAt = DateTime.UtcNow
            };

            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                Score = 10.0,
                Feedback = "Verified correct in Practice Arena",
                GradedAt = DateTime.UtcNow
            };

            await _practiceGateway.RecordAttemptAsync(newUser, submission, grade);

            var totalSolved = await _submissionGateway.CountByStudentWalletAsync(cleanWallet);

            return new RecordSolvedProblemResultDto { TotalSolved = totalSolved };
        }

        public async Task<PracticeProgressDto> GetPracticeProgressAsync(string walletAddress, string formulaId)
        {
            if (string.IsNullOrWhiteSpace(walletAddress) || string.IsNullOrWhiteSpace(formulaId))
            {
                return new PracticeProgressDto { FormulaId = formulaId ?? "" };
            }

            var submissions = await _submissionGateway.GetPracticeSubmissionsAsync(walletAddress, formulaId);

            var solvedIndices = new HashSet<int>();
            foreach (var submission in submissions)
            {
                var match = Regex.Match(submission.Content, @"ExerciseIndex\s+(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
                {
                    solvedIndices.Add(index);
                }
            }

            int lastIndex = solvedIndices.Any() ? solvedIndices.Max() + 1 : 0;

            return new PracticeProgressDto
            {
                FormulaId = formulaId,
                LastExerciseIndex = lastIndex,
                SolvedIndices = solvedIndices.OrderBy(i => i).ToList()
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
