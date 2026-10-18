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
}
