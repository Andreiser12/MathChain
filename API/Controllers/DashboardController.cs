using MathChain.Persistence.Context;
using MathChain.Application.DTOs;
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
                    hard = solved / 3;
                    medium = solved / 3;
                    easy = solved - hard - medium;

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
                    hard = solved / 3;
                    medium = solved / 3;
                    easy = solved - hard - medium;

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
    }
}
