using System.Text.Json;
using MathChain.Application.Practice;
using MathChain.Tests.Shared;

namespace MathChain.Application.IntegrationTests.Practice
{
    public class ExerciseCatalogTests
    {
        [Fact]
        public void ForFormula_ShouldProduceUsableExercises_WhenReadingEveryCurriculumFile()
        {
            var directory = RepositoryPaths.CurriculumDirectory;
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

                        Assert.Equal(new[] { "Easy", "Easy", "Easy", "Medium", "Medium", "Medium", "Hard", "Hard", "Hard" },
                            catalog.Exercises.Select(e => e.Difficulty).ToArray());

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

            Assert.Equal(19, withOwnExercises);
        }
    }
}
