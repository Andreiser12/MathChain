using MathChain.Persistence.Context;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Infrastructure.ResponseHandling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly MathChainDbContext _context;

        public DashboardController(MathChainDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string? walletAddress)
        {
            var normalizedWallet = (walletAddress ?? "").Trim().ToLowerInvariant();

            // 1. Fetch real users from DB
            var users = await _context.Users.ToListAsync();

            // 2. Fetch real submissions from DB
            var submissions = await _context.Submissions.ToListAsync();

            // 3. Fetch real grades from DB
            var grades = await _context.Grades.ToListAsync();
            var gradesBySubmissionId = grades.ToDictionary(g => g.SubmissionId);

            // Group submissions by student wallet (case-insensitive)
            var submissionsByWallet = submissions
                .Where(s => !string.IsNullOrWhiteSpace(s.StudentWallet))
                .GroupBy(s => s.StudentWallet.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build list of solvers for all registered users
            var solvers = new List<(string Wallet, int Solved, int Hard, int Medium, int Easy, int Accuracy)>();

            foreach (var user in users)
            {
                var w = user.WalletAddress.Trim().ToLowerInvariant();
                int solved = 0;
                int hard = 0, medium = 0, easy = 0;
                int accuracy = 0;

                if (submissionsByWallet.TryGetValue(w, out var userSubs) && userSubs.Any())
                {
                    solved = userSubs.Count;

                    int recHard = userSubs.Count(s => s.Content != null && s.Content.Contains("Difficulty Hard", StringComparison.OrdinalIgnoreCase));
                    int recMed = userSubs.Count(s => s.Content != null && s.Content.Contains("Difficulty Medium", StringComparison.OrdinalIgnoreCase));
                    int recEasy = userSubs.Count(s => s.Content != null && s.Content.Contains("Difficulty Easy", StringComparison.OrdinalIgnoreCase));

                    if (recHard + recMed + recEasy > 0)
                    {
                        hard = recHard;
                        medium = recMed;
                        easy = recEasy + (solved - recHard - recMed - recEasy);
                    }
                    else
                    {
                        hard = solved / 3;
                        medium = solved / 3;
                        easy = solved - hard - medium;
                    }

                    var userGrades = userSubs
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

                solvers.Add((user.WalletAddress, solved, hard, medium, easy, accuracy));
            }

            // Ensure querying wallet is present in solvers list if provided
            if (!string.IsNullOrEmpty(normalizedWallet) && !solvers.Any(s => s.Wallet.Trim().ToLowerInvariant() == normalizedWallet))
            {
                int solved = 0, hard = 0, medium = 0, easy = 0, accuracy = 0;
                if (submissionsByWallet.TryGetValue(normalizedWallet, out var userSubs) && userSubs.Any())
                {
                    solved = userSubs.Count;

                    int recHard = userSubs.Count(s => s.Content != null && s.Content.Contains("Difficulty Hard", StringComparison.OrdinalIgnoreCase));
                    int recMed = userSubs.Count(s => s.Content != null && s.Content.Contains("Difficulty Medium", StringComparison.OrdinalIgnoreCase));
                    int recEasy = userSubs.Count(s => s.Content != null && s.Content.Contains("Difficulty Easy", StringComparison.OrdinalIgnoreCase));

                    if (recHard + recMed + recEasy > 0)
                    {
                        hard = recHard;
                        medium = recMed;
                        easy = recEasy + (solved - recHard - recMed - recEasy);
                    }
                    else
                    {
                        hard = solved / 3;
                        medium = solved / 3;
                        easy = solved - hard - medium;
                    }

                    var userGrades = userSubs
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
                solvers.Add((walletAddress!, solved, hard, medium, easy, accuracy));
            }

            // Rank solvers descending by solved count
            var ranked = solvers
                .OrderByDescending(s => s.Solved)
                .ThenBy(s => s.Wallet)
                .ToList();

            var leaderboard = new List<LeaderboardEntryDto>();
            for (int i = 0; i < Math.Min(10, ranked.Count); i++)
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

            var dto = new DashboardStatsDto
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

            return Ok(EnvelopeExtensions.Success(dto));
        }

        [HttpPost("record-solved")]
        public async Task<IActionResult> RecordSolvedProblem([FromBody] RecordSolvedProblemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                return BadRequest(EnvelopeExtensions.Failure<object>("Wallet address is required.", 400));
            }

            var cleanWallet = request.WalletAddress.Trim();

            // 1. Ensure user exists in Users table so they appear on leaderboard
            var user = await _context.Users.FirstOrDefaultAsync(u => u.WalletAddress.ToLower() == cleanWallet.ToLower());
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    WalletAddress = cleanWallet,
                    Role = UserRole.Student,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
            }

            // 2. Add submission record
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                AssignmentId = Guid.Empty,
                StudentWallet = cleanWallet,
                Content = $"Practice: Formula {request.FormulaId ?? "General"}, ExerciseIndex {request.ExerciseIndex ?? 0}, Difficulty {request.Difficulty ?? "Easy"}, Solution {request.Solution ?? ""}",
                SubmittedAt = DateTime.UtcNow
            };
            _context.Submissions.Add(submission);

            // 3. Add grade record (10.0 = 100% accuracy)
            var grade = new Grade
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                Score = 10.0,
                Feedback = "Verified correct in Practice Arena",
                GradedAt = DateTime.UtcNow
            };
            _context.Grades.Add(grade);

            await _context.SaveChangesAsync();

            int totalSolved = await _context.Submissions.CountAsync(s => s.StudentWallet.ToLower() == cleanWallet.ToLower());

            return Ok(EnvelopeExtensions.Success(new { TotalSolved = totalSolved }));
        }

        [HttpGet("practice-progress")]
        public async Task<IActionResult> GetPracticeProgress([FromQuery] string walletAddress, [FromQuery] string formulaId)
        {
            if (string.IsNullOrWhiteSpace(walletAddress) || string.IsNullOrWhiteSpace(formulaId))
            {
                return Ok(EnvelopeExtensions.Success(new PracticeProgressDto { FormulaId = formulaId ?? "" }));
            }

            var cleanWallet = walletAddress.Trim().ToLowerInvariant();
            var submissions = await _context.Submissions
                .Where(s => s.StudentWallet.ToLower() == cleanWallet && s.Content.Contains($"Formula {formulaId}"))
                .ToListAsync();

            var solvedIndices = new HashSet<int>();
            foreach (var sub in submissions)
            {
                var match = System.Text.RegularExpressions.Regex.Match(sub.Content, @"ExerciseIndex\s+(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int idx))
                {
                    solvedIndices.Add(idx);
                }
            }

            int lastIndex = solvedIndices.Any() ? solvedIndices.Max() + 1 : 0;

            var dto = new PracticeProgressDto
            {
                FormulaId = formulaId,
                LastExerciseIndex = lastIndex,
                SolvedIndices = solvedIndices.OrderBy(i => i).ToList()
            };

            return Ok(EnvelopeExtensions.Success(dto));
        }
    }
}
