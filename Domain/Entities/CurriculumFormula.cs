using System;
using System.Text.Json.Serialization;

namespace MathChain.Domain.Entities
{
    public class CurriculumFormula
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ChapterId { get; set; }
        public string FormulaCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirement { get; set; }
        public string? Model3D { get; set; }
        public string? ExercisesJson { get; set; }
        public int Order { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public CurriculumChapter? Chapter { get; set; }
    }
}
