using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class ExerciseTemplate
    {
        public string Difficulty { get; set; } = "Easy";
        public int Order { get; set; }
        public Dictionary<string, int[]> Variables { get; set; } = new();
        public string LatexTemplate { get; set; } = string.Empty;
        public string WolframQuery { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string? SolutionIpfs { get; set; }
    }
}
