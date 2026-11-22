using System.Reflection;
using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Interfaces;

namespace MathChain.Infrastructure.Solutions
{
    public class EmbeddedSolutionCatalog : ISolutionCatalog
    {
        private const string ResourcePrefix = "MathChain.Infrastructure.Solutions.Data.";

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly Dictionary<string, List<SolutionEntry>> _entries;

        public EmbeddedSolutionCatalog() : this(LoadEmbeddedFiles().ToList())
        {
        }

        private EmbeddedSolutionCatalog(IReadOnlyList<string> jsonFiles)
        {
            _entries = new Dictionary<string, List<SolutionEntry>>(StringComparer.Ordinal);

            foreach (var json in jsonFiles)
            {
                var file = JsonSerializer.Deserialize<SolutionFile>(json, JsonOptions)
                    ?? throw new InvalidOperationException("A solution file is empty.");

                foreach (var (formulaCode, solutions) in file.Formulas)
                {
                    if (_entries.ContainsKey(formulaCode))
                    {
                        throw new InvalidOperationException($"The solutions of formula {formulaCode} are defined twice.");
                    }

                    _entries[formulaCode] = solutions;
                }
            }
        }

        public int FormulaCount => _entries.Count;

        public static EmbeddedSolutionCatalog FromJson(IEnumerable<string> jsonFiles)
        {
            return new EmbeddedSolutionCatalog(jsonFiles.ToList());
        }

        public ExerciseSolution? Find(string formulaCode, ExerciseTemplate template)
        {
            if (string.IsNullOrEmpty(formulaCode) || !_entries.TryGetValue(formulaCode, out var solutions))
            {
                return null;
            }

            var entry = solutions.FirstOrDefault(s => s.Order == template.Order
                && s.LatexTemplate == template.LatexTemplate
                && s.WolframQuery == template.WolframQuery);

            if (entry == null || entry.Steps.Count == 0)
            {
                return null;
            }

            return new ExerciseSolution
            {
                Steps = entry.Steps.Select(step => new SolutionStep { Text = step.Text, Latex = step.Latex }).ToList()
            };
        }

        private static IEnumerable<string> LoadEmbeddedFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)).OrderBy(n => n))
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                yield return reader.ReadToEnd();
            }
        }

        private sealed class SolutionFile
        {
            public Dictionary<string, List<SolutionEntry>> Formulas { get; set; } = new();
        }

        private sealed class SolutionEntry
        {
            public int Order { get; set; }
            public string LatexTemplate { get; set; } = string.Empty;
            public string WolframQuery { get; set; } = string.Empty;
            public List<SolutionEntryStep> Steps { get; set; } = new();
        }

        private sealed class SolutionEntryStep
        {
            public string Text { get; set; } = string.Empty;
            public string? Latex { get; set; }
        }
    }
}
