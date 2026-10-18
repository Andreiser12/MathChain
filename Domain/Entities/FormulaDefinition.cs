using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class FormulaDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string Model3D { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Requirement { get; set; }
        public List<ExerciseTemplate> Exercises { get; set; } = new();
    }
}
