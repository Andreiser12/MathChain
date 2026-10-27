using System;
using System.Collections.Generic;

namespace MathChain.Application.DTOs
{
    public class CurriculumChapterResponseDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string ChapterSlug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IpfsHash { get; set; }
        public int Order { get; set; }
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int SubChaptersCount { get; set; }
        public int TotalSubChaptersCount { get; set; }
        public int FormulasCount { get; set; }
        public int TotalFormulasCount { get; set; }
    }

    public class BreadcrumbDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    public class ChapterDetailResponseDto
    {
        public CurriculumChapterResponseDto Chapter { get; set; } = new();
        public List<CurriculumChapterResponseDto> SubChapters { get; set; } = new();
        public List<BreadcrumbDto> Breadcrumbs { get; set; } = new();
    }

    public class CreateChapterRequest
    {
        public Guid? ParentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ChapterSlug { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IpfsHash { get; set; }
        public int Order { get; set; } = 0;
        public int FormulasCount { get; set; } = 0;
    }

    public class UpdateChapterRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? ChapterSlug { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IpfsHash { get; set; }
        public int Order { get; set; }
        public int FormulasCount { get; set; } = 0;
    }
}
