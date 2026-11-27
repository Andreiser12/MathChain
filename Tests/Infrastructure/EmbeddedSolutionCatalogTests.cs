using System.Text.Json;
using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.Solutions;

namespace MathChain.Tests.Infrastructure
{
    public class EmbeddedSolutionCatalogTests
    {
        private static string CurriculumDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MathChain.sln")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory!.FullName, "MathChain.Blazor", "wwwroot", "data", "curriculum");
        }

        [Fact]
        public void EveryEmbeddedSolution_ShouldMatchAnExerciseOfTheCurriculum_WhenTheCatalogIsLoaded()
        {
            var catalog = new EmbeddedSolutionCatalog();
            var found = 0;
            var formulasWithSolutions = 0;

            foreach (var file in Directory.GetFiles(CurriculumDirectory(), "*.json"))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));

                if (!document.RootElement.TryGetProperty("formulas", out var formulas))
                {
                    continue;
                }

                foreach (var formula in formulas.EnumerateArray())
                {
                    var code = formula.GetProperty("id").GetString()!;
                    var exercises = ExerciseCatalog.ForFormula(formula.TryGetProperty("exercises", out var raw) ? raw.GetRawText() : null);
                    var withSolution = exercises.UsesGenericExercises
                        ? 0
                        : exercises.Exercises.Count(e => catalog.Find(code, e.Template) != null);

                    found += withSolution;
                    formulasWithSolutions += withSolution > 0 ? 1 : 0;

                    if (withSolution > 0)
                    {
                        Assert.Equal(exercises.Exercises.Count, withSolution);
                    }
                }
            }

            Assert.Equal(catalog.FormulaCount, formulasWithSolutions);
            Assert.Equal(81, found);
        }

        [Fact]
        public void Find_ShouldReturnTheStepsOnlyForTheSameTemplate_WhenTheCodeOrderLatexOrQueryDiffers()
        {
            var file = JsonSerializer.Serialize(new
            {
                formulas = new Dictionary<string, object[]>
                {
                    ["code-f1"] = new object[]
                    {
                        new { order = 2, latexTemplate = "{a}x", wolframQuery = "{a}", steps = new[] { new { text = "Read {a}.", latex = "{a}" } } }
                    }
                }
            });
            var catalog = EmbeddedSolutionCatalog.FromJson(new[] { file });
            var template = new ExerciseTemplate { Order = 2, LatexTemplate = "{a}x", WolframQuery = "{a}" };

            var solution = catalog.Find("code-f1", template);

            Assert.NotNull(solution);
            Assert.Equal("Read {a}.", Assert.Single(solution!.Steps).Text);
            Assert.Null(catalog.Find("code-f2", template));
            Assert.Null(catalog.Find("code-f1", new ExerciseTemplate { Order = 1, LatexTemplate = "{a}x", WolframQuery = "{a}" }));
            Assert.Null(catalog.Find("code-f1", new ExerciseTemplate { Order = 2, LatexTemplate = "{a}y", WolframQuery = "{a}" }));
            Assert.Null(catalog.Find("code-f1", new ExerciseTemplate { Order = 2, LatexTemplate = "{a}x", WolframQuery = "{a}+1" }));
        }

        [Fact]
        public void FromJson_ShouldThrowInvalidOperationException_WhenAFormulaIsDefinedTwice()
        {
            var file = JsonSerializer.Serialize(new { formulas = new Dictionary<string, object[]> { ["code-f1"] = Array.Empty<object>() } });

            Assert.Throws<InvalidOperationException>(() => EmbeddedSolutionCatalog.FromJson(new[] { file, file }));
        }
    }
}
