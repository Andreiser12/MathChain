using System.Text.Json;
using MathChain.Domain.Entities;

namespace MathChain.Application.Practice
{
    public static class SolutionRenderer
    {
        public static bool TryReadValues(string? variableValues, out Dictionary<string, int> values)
        {
            values = new Dictionary<string, int>();

            if (string.IsNullOrWhiteSpace(variableValues))
            {
                return false;
            }

            try
            {
                values = JsonSerializer.Deserialize<Dictionary<string, int>>(variableValues) ?? new Dictionary<string, int>();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static List<SolutionStep> Render(ExerciseSolution solution, IReadOnlyDictionary<string, int> values)
        {
            return solution.Steps
                .Select(step => new SolutionStep
                {
                    Text = ExerciseGenerator.Substitute(step.Text, values),
                    Latex = string.IsNullOrWhiteSpace(step.Latex) ? null : ExerciseGenerator.Substitute(step.Latex, values)
                })
                .ToList();
        }
    }
}
