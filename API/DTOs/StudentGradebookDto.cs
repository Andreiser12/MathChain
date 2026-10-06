namespace MathChain.API.DTOs
{
    public class StudentGradebookDto
    {
        public string StudentWallet { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int? FinalGrade { get; set; }
        public List<StudentGradeEntryDto> Grades { get; set; } = new();
    }

    public class StudentGradeEntryDto
    {
        public DateTime GradedAt { get; set; }
        public double Score { get; set; }
    }
}
