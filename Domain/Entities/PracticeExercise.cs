using MathChain.Domain.Enums;

namespace MathChain.Domain.Entities
{
    public class PracticeExercise
    {
        public Guid Id { get; set; }
        public string StudentWallet { get; set; } = string.Empty;
        public Guid FormulaId { get; set; }
        public int ExerciseIndex { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string Requirement { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public AnswerForm Form { get; set; }
        public string ExpectedAnswer { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? SolvedAt { get; set; }
    }

    public class SolvedPracticeExercise
    {
        public Guid FormulaId { get; set; }
        public int ExerciseIndex { get; set; }
    }
}
