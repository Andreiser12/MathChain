using MathChain.Domain.Enums;

namespace MathChain.Application.DTOs
{
    public class PracticeFormulaDto
    {
        public Guid FormulaId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool UsesGenericExercises { get; set; }
        public List<PracticeExerciseSummaryDto> Exercises { get; set; } = new();
        public List<int> SolvedIndices { get; set; } = new();
    }

    public class PracticeExerciseSummaryDto
    {
        public int Index { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public AnswerForm Form { get; set; }
    }

    public class PracticeExerciseDto
    {
        public Guid ExerciseId { get; set; }
        public Guid FormulaId { get; set; }
        public int Index { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string Requirement { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public AnswerForm Form { get; set; }
        public int AttemptsLeft { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class SubmitPracticeAnswerRequest
    {
        public string Answer { get; set; } = string.Empty;
    }

    public class PracticeAnswerResultDto
    {
        public bool Correct { get; set; }
        public string Verdict { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int AttemptsLeft { get; set; }
        public List<int> SolvedIndices { get; set; } = new();
        public int TotalExercises { get; set; }
    }
}
