using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class ChapterCurriculum
    {
        public string ChapterId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = string.Empty;
        public List<FormulaDefinition> Formulas { get; set; } = new();
    }
}
