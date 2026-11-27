using System.Text.Json;
using MathChain.Application.Practice;
using MathChain.Infrastructure.Solutions;
using MathChain.Tests.Shared;

namespace MathChain.Infrastructure.IntegrationTests
{
    public class EmbeddedSolutionCatalogTests
    {
        [Fact]
        public void EveryEmbeddedSolution_ShouldMatchAnExerciseOfTheCurriculum_WhenTheCatalogIsLoaded()
        {
            var catalog = new EmbeddedSolutionCatalog();
            var found = 0;
            var formulasWithSolutions = 0;

            foreach (var file in Directory.GetFiles(RepositoryPaths.CurriculumDirectory, "*.json"))
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
    }
}
