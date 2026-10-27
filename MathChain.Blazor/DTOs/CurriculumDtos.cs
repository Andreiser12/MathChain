using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MathChain.Blazor.DTOs
{
    public class ChapterCurriculumDto
    {
        [JsonPropertyName("chapterId")]
        public string ChapterId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("formulas")]
        public List<FormulaDefinitionDto> Formulas { get; set; } = new();
    }

    public class FormulaDefinitionDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("latex")]
        public string Latex { get; set; } = string.Empty;

        [JsonPropertyName("model3D")]
        public string Model3D { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("requirement")]
        public string? Requirement { get; set; }

        [JsonPropertyName("exercises")]
        public List<ExerciseTemplateDto> Exercises { get; set; } = new();
    }

    public class ExerciseTemplateDto
    {
        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "Easy";

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("variables")]
        public Dictionary<string, int[]> Variables { get; set; } = new();

        [JsonPropertyName("latexTemplate")]
        public string LatexTemplate { get; set; } = string.Empty;

        [JsonPropertyName("wolframQuery")]
        public string WolframQuery { get; set; } = string.Empty;

        [JsonPropertyName("hint")]
        public string? Hint { get; set; }

        [JsonPropertyName("solutionIpfs")]
        public string? SolutionIpfs { get; set; }
    }

    public class CurriculumChapterDto
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

    public class ChapterDetailDto
    {
        public CurriculumChapterDto Chapter { get; set; } = new();
        public List<CurriculumChapterDto> SubChapters { get; set; } = new();
        public List<BreadcrumbDto> Breadcrumbs { get; set; } = new();
    }

    public class CreateChapterDto
    {
        public Guid? ParentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ChapterSlug { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IpfsHash { get; set; }
        public int Order { get; set; } = 0;
    }

    public class UpdateChapterDto
    {
        public string Title { get; set; } = string.Empty;
        public string? ChapterSlug { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IpfsHash { get; set; }
        public int Order { get; set; }
    }

    public class CurriculumFormulaDto
    {
        public Guid Id { get; set; }
        public Guid ChapterId { get; set; }
        public string FormulaCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirement { get; set; }
        public string? Model3D { get; set; }
        public string? ExercisesJson { get; set; }
        public int Order { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateFormulaDto
    {
        public string Title { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string? FormulaCode { get; set; }
        public string? Description { get; set; }
        public string? Requirement { get; set; }
        public string? Model3D { get; set; }
        public string? ExercisesJson { get; set; }
        public int Order { get; set; } = 0;
    }

    public class UpdateFormulaDto
    {
        public string Title { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
        public string? FormulaCode { get; set; }
        public string? Description { get; set; }
        public string? Requirement { get; set; }
        public string? Model3D { get; set; }
        public string? ExercisesJson { get; set; }
        public int Order { get; set; } = 0;
    }
}
