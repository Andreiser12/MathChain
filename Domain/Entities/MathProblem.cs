using MathChain.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public abstract class MathProblem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DifficultyLevel Difficulty { get; set; }
        public DateTime CreatedAt { get; set; }

        public abstract string GetProblemStatement();
        public abstract bool ValidateSolution(string solution);

    }
}
