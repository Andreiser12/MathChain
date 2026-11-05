using System.Collections.Generic;

namespace MathChain.Blazor.DTOs
{
    public class DashboardStatsDto
    {
        public int GlobalTotalSolved { get; set; }
        public int GlobalHardSolved { get; set; }
        public int GlobalMediumSolved { get; set; }
        public int GlobalEasySolved { get; set; }

        public int UserHardSolved { get; set; }
        public int UserMediumSolved { get; set; }
        public int UserEasySolved { get; set; }
        public int UserTotalSolved { get; set; }
        public int UserAccuracy { get; set; }
        public int UserRank { get; set; }

        public List<LeaderboardEntryDto> Leaderboard { get; set; } = new();
    }

    public class LeaderboardEntryDto
    {
        public int Position { get; set; }
        public string WalletAddress { get; set; } = string.Empty;
        public int ProblemsSolved { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}
