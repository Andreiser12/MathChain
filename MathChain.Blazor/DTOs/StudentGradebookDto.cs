using MathChain.Blazor.Services;

namespace MathChain.Blazor.DTOs
{
    public class StudentGradebookDto
    {
        public string StudentWallet { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int? FinalGrade { get; set; }
        public List<StudentGradeEntryDto> Grades { get; set; } = new();
    }
}
