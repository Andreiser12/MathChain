using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MathChain.Domain.Entities
{
    public class CurriculumChapter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }
        public string ChapterSlug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IpfsHash { get; set; } = string.Empty;
        public int Order { get; set; } = 0;
        public int FormulasCount { get; set; } = 0;
        public int Version { get; set; } = 1;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [JsonIgnore]
        public CurriculumChapter? Parent { get; set; }

        [JsonIgnore]
        public ICollection<CurriculumChapter> SubChapters { get; set; } = new List<CurriculumChapter>();
    }
}
