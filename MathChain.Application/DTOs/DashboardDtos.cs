using System.Collections.Generic;

namespace MathChain.Application.DTOs
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

    public class RecordSolvedProblemRequest
    {
        public string WalletAddress { get; set; } = string.Empty;
        public string? FormulaId { get; set; }
        public string? Difficulty { get; set; }
        public string? Solution { get; set; }
        public int? ExerciseIndex { get; set; }
    }

    public class RecordSolvedProblemResultDto
    {
        public int TotalSolved { get; set; }
    }

    public class PracticeProgressDto
    {
        public string FormulaId { get; set; } = string.Empty;
        public int LastExerciseIndex { get; set; } = 0;
        public List<int> SolvedIndices { get; set; } = new();
    }
}
