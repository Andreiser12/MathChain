using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Domain.Entities
{
    public class CurriculumChapter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ChapterSlug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
