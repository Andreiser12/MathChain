using System.Text.Json;
using MathChain.Application.Practice;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Practice
{
    public class ExerciseCatalogTests
    {
        private static string Json(params object[] exercises)
        {
            return JsonSerializer.Serialize(exercises);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("[]")]
        public void ForFormula_WithoutExercises_UsesTheNineGenericExercises(string? json)
        {
            var exercises = ExerciseCatalog.ForFormula(json);

            Assert.True(exercises.UsesGenericExercises);
            Assert.Equal(9, exercises.Exercises.Count);
            Assert.Equal(new[] { "Easy", "Easy", "Easy", "Medium", "Medium", "Medium", "Hard", "Hard", "Hard" }, exercises.Exercises.Select(e => e.Difficulty));
            Assert.All(exercises.Exercises, e => Assert.Equal(AnswerForm.Number, e.Form));
            Assert.Equal(Enumerable.Range(0, 9), exercises.Exercises.Select(e => e.Index));
        }

        [Fact]
        public void ForFormula_OnlyTheoryPlaceholders_UsesTheGenericExercises()
        {
            var json = Json(
                new { difficulty = "Easy", wolframQuery = "simplify |\\pm x|=x", latexTemplate = "|\\pm x|=x", variables = new { a = new[] { 2, 5 } } },
                new { difficulty = "Hard", wolframQuery = "simplify |x|\\geq 0", latexTemplate = "|x|\\geq 0" });

            Assert.True(ExerciseCatalog.ForFormula(json).UsesGenericExercises);
        }

        [Fact]
        public void ForFormula_RealExercises_KeepsOnlyTheVerifiableOnesAndRenumbersThem()
        {
            var json = Json(
                new { difficulty = "Easy", wolframQuery = "expand (x + {a})^2", latexTemplate = "(x + {a})^2", variables = new { a = new[] { 2, 5 } } },
                new { difficulty = "Hard", wolframQuery = "factor x^n - 1", latexTemplate = "x^n - 1", variables = new { a = new[] { 1, 1 } } },
                new { difficulty = "Medium", wolframQuery = "factor x^2 - {a*a}", latexTemplate = "x^2 - {a*a}", variables = new { a = new[] { 2, 9 } } });

            var exercises = ExerciseCatalog.ForFormula(json);

            Assert.False(exercises.UsesGenericExercises);
            Assert.Equal(new[] { AnswerForm.Expanded, AnswerForm.Factored }, exercises.Exercises.Select(e => e.Form));
            Assert.Equal(new[] { "Easy", "Medium" }, exercises.Exercises.Select(e => e.Difficulty));
            Assert.Equal(new[] { 0, 1 }, exercises.Exercises.Select(e => e.Index));
        }

        [Fact]
        public void ForFormula_IsDeterministic()
        {
            var json = Json(new { wolframQuery = "expand (x + {a})^2", latexTemplate = "(x + {a})^2", variables = new { a = new[] { 2, 5 } } });

            Assert.Equal(ExerciseCatalog.ForFormula(json).Exercises.Count, ExerciseCatalog.ForFormula(json).Exercises.Count);
            Assert.Equal("Easy", ExerciseCatalog.ForFormula(json).Exercises.Single().Difficulty);
        }

        [Fact]
        public void ForFormula_EveryCurriculumFile_ProducesUsableExercises()
        {
            var directory = Path.Combine(FindRepositoryRoot(), "MathChain.Blazor", "wwwroot", "data", "curriculum");
            var withOwnExercises = 0;

            foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));

                if (!document.RootElement.TryGetProperty("formulas", out var formulas))
                {
                    continue;
                }

                foreach (var formula in formulas.EnumerateArray())
                {
                    var json = formula.TryGetProperty("exercises", out var exercises) ? exercises.GetRawText() : null;
                    var catalog = ExerciseCatalog.ForFormula(json);

                    Assert.NotEmpty(catalog.Exercises);

                    if (!catalog.UsesGenericExercises)
                    {
                        withOwnExercises++;

                        foreach (var definition in catalog.Exercises)
                        {
                            for (var seed = 0; seed < 5; seed++)
                            {
                                var generated = ExerciseGenerator.Generate(definition.Template, new PracticeRandom(new Random(seed)));
                                Assert.DoesNotContain("{a", generated.Latex);
                                Assert.DoesNotContain("{b", generated.ExpectedAnswer);
                            }
                        }
                    }
                }
            }

            Assert.Equal(10, withOwnExercises);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MathChain.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }
}
