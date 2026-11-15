using System.Text.Json;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Application.Practice
{
    public sealed record PracticeExerciseDefinition(int Index, ExerciseTemplate Template, AnswerForm Form, string Difficulty);

    public sealed record FormulaExercises(IReadOnlyList<PracticeExerciseDefinition> Exercises, bool UsesGenericExercises);

    public static class ExerciseCatalog
    {
        private const int ValidationSeed = 20261115;

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public static FormulaExercises ForFormula(string? exercisesJson)
        {
            var own = ParseTemplates(exercisesJson)
                .Select(Describe)
                .Where(definition => definition != null)
                .Select((definition, index) => definition! with { Index = index })
                .ToList();

            if (own.Count > 0)
            {
                return new FormulaExercises(own, false);
            }

            var generic = DefaultExercises()
                .Select(Describe)
                .Select((definition, index) => definition! with { Index = index })
                .ToList();

            return new FormulaExercises(generic, true);
        }

        public static List<ExerciseTemplate> DefaultExercises()
        {
            const string primitive = "Calculate the coefficient of x in the primitive function:";

            return new List<ExerciseTemplate>
            {
                Template(1, "Easy", primitive, @"\int {a} \,dx", "{a}", "The primitive is F(x) = a*x + C. The coefficient of x is simply a.", ("a", 2, 10)),
                Template(2, "Easy", primitive, @"\int {a} \,dx", "{a}", "The primitive is F(x) = a*x + C. The coefficient of x is a.", ("a", 11, 30)),
                Template(3, "Easy", primitive, @"\int {a} \,dx", "{a}", "The coefficient of x in the primitive is the constant under the integral.", ("a", 31, 99)),
                Template(4, "Medium", "Calculate the coefficient of x in the primitive of the negative constant:", @"\int (-{a}) \,dx", "-{a}", "The primitive is -a*x + C. Don't forget the negative sign.", ("a", 2, 25)),
                Template(5, "Medium", "Calculate the coefficient of x in the primitive function (enter decimal or integer):", @"\int \frac{{a}}{{b}} \,dx", "({a}/{b})", "The primitive is (a/b)*x + C. Compute the value of the fraction a/b.", ("a", 6, 30), ("b", 2, 5)),
                Template(6, "Medium", primitive, @"\int -(-{a}) \,dx", "{a}", "-(-a) = a. The two minus signs simplify to positive a.", ("a", 3, 20)),
                Template(7, "Hard", primitive, @"\int ({a} + {b}) \,dx", "({a} + {b})", "First add the two constants (a + b). The coefficient of x will be their sum.", ("a", 10, 40), ("b", 5, 30)),
                Template(8, "Hard", primitive, @"\int ({a} - {b}) \,dx", "({a} - {b})", "Compute (a - b). The result may be a negative number.", ("a", 5, 25), ("b", 10, 35)),
                Template(9, "Hard", primitive, @"\int (2 \cdot {a} + {b}) \,dx", "(2*{a} + {b})", "Calculate 2*a + b first. That is the constant term multiplied by x in the primitive.", ("a", 3, 15), ("b", 2, 20))
            };
        }

        private static List<ExerciseTemplate> ParseTemplates(string? exercisesJson)
        {
            if (string.IsNullOrWhiteSpace(exercisesJson))
            {
                return new List<ExerciseTemplate>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<ExerciseTemplate>>(exercisesJson, JsonOptions)?
                    .Where(template => template != null)
                    .ToList() ?? new List<ExerciseTemplate>();
            }
            catch (JsonException)
            {
                return new List<ExerciseTemplate>();
            }
        }

        private static PracticeExerciseDefinition? Describe(ExerciseTemplate template)
        {
            try
            {
                var generated = ExerciseGenerator.Generate(template, new PracticeRandom(new Random(ValidationSeed)));
                var difficulty = string.IsNullOrWhiteSpace(template.Difficulty) ? "Easy" : template.Difficulty.Trim();

                return new PracticeExerciseDefinition(0, template, generated.Form, difficulty);
            }
            catch (MathExpressionParseException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static ExerciseTemplate Template(int order, string difficulty, string requirement, string latex, string query, string hint,
            params (string Name, int Minimum, int Maximum)[] variables)
        {
            return new ExerciseTemplate
            {
                Order = order,
                Difficulty = difficulty,
                Requirement = requirement,
                IsRandom = true,
                Variables = variables.ToDictionary(v => v.Name, v => new[] { v.Minimum, v.Maximum }),
                LatexTemplate = latex,
                WolframQuery = query,
                Hint = hint
            };
        }
    }
}
