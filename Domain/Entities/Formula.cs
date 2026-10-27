using System;

namespace MathChain.Domain.Entities
{
    public class Formula
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FormulaCode { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Requirement { get; set; } = string.Empty;
        public string? ExercisesJson { get; set; }
        public int Order { get; set; } = 0;
    }
}
