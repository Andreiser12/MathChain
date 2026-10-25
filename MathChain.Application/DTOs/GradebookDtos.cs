namespace MathChain.Application.DTOs
{
    public class StudentGradeEntryDto
    {
        public DateTime GradedAt { get; set; }
        public double Score { get; set; }
    }

    public class StudentGradebookDto
    {
        public string StudentWallet { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int? FinalGrade { get; set; }
        public List<StudentGradeEntryDto> Grades { get; set; } = new();
    }

    public class SetFinalGradeDto
    {
        public int FinalGrade { get; set; }
    }

    public class ClassStatisticsDto
    {
        public double ClassAverage { get; set; }
        public double SubmissionRate { get; set; }
        public double HighestGrade { get; set; }
        public int StudentsAtRiskCount { get; set; }
        public int TotalEnrolledStudents { get; set; }
        public int TotalAssignments { get; set; }

        public List<int> GradeDistributionCounts { get; set; } = new();
        public List<string> GradeDistributionLabels { get; set; } = new();

        public int SubmissionsOnTime { get; set; }
        public int SubmissionsLate { get; set; }
        public int SubmissionsMissing { get; set; }

        public List<string> WeeklyLabels { get; set; } = new();
        public List<double> WeeklyAverages { get; set; } = new();

        public List<StudentPerformanceSummaryDto> TopPerformers { get; set; } = new();
        public List<StudentPerformanceSummaryDto> AtRiskStudents { get; set; } = new();
    }

    public class StudentPerformanceSummaryDto
    {
        public string StudentWallet { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public double AverageGrade { get; set; }
        public int MissingAssignmentsCount { get; set; }
        public string StatusBadge { get; set; } = string.Empty;
    }
}
