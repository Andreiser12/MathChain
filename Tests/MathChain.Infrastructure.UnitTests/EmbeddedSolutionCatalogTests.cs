using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.Solutions;

namespace MathChain.Infrastructure.UnitTests
{
    public class EmbeddedSolutionCatalogTests
    {
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
