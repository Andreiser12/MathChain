using System.Text.RegularExpressions;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Application.Practice
{
    public sealed record GeneratedExercise(string Requirement, string Latex, string? Hint, AnswerForm Form, string ExpectedAnswer,
        IReadOnlyDictionary<string, int> Values);

    public class PracticeRandom
    {
        private readonly Random _random;

        public PracticeRandom() : this(Random.Shared)
        {
        }

        public PracticeRandom(Random random)
        {
            _random = random;
        }

        public virtual int Next(int minimum, int maximumExclusive)
        {
            return _random.Next(minimum, maximumExclusive);
        }
    }

    public static class ExerciseGenerator
    {
        private const string LegacyRequirementMarker = "evaluate at x = 1";

        private static readonly Regex Placeholder = new(@"\{([^{}]*)\}", RegexOptions.Compiled);
        private static readonly Regex Identifier = new("[A-Za-z]+", RegexOptions.Compiled);
        private static readonly Regex ArithmeticContent = new(@"^[A-Za-z0-9+\-*/^().\s]+$", RegexOptions.Compiled);

        private static readonly (string Verb, AnswerForm Form)[] Verbs =
        {
            ("expand ", AnswerForm.Expanded),
            ("factor ", AnswerForm.Factored),
            ("simplify ", AnswerForm.Simplified)
        };

        public static GeneratedExercise Generate(ExerciseTemplate template, PracticeRandom random)
        {
            var values = SampleValues(template, random);
            var query = (template.WolframQuery ?? string.Empty).Trim();

            if (query.Length == 0)
            {
                throw new MathExpressionParseException("The exercise has no answer definition.");
            }

            var form = AnswerForm.Number;

            foreach (var (verb, verbForm) in Verbs)
            {
                if (query.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                {
                    form = verbForm;
                    query = query[verb.Length..];
                    break;
                }
            }

            var expected = Substitute(query, values);
            string expectedAnswer;

            if (form == AnswerForm.Number)
            {
                expectedAnswer = AnswerChecker.FormatNumber(AnswerChecker.EvaluateNumber(expected));
            }
            else
            {
                var expression = MathExpressionParser.Parse(expected, AnswerChecker.Variables);

                if (expression.IsConstant)
                {
                    throw new MathExpressionParseException("The exercise does not contain a variable.");
                }

                expectedAnswer = MathExpressionParser.Normalize(expected);
            }

            var latex = Substitute(template.LatexTemplate ?? string.Empty, values);

            var hint = string.IsNullOrWhiteSpace(template.Hint) ? null : Substitute(template.Hint, values);

            return new GeneratedExercise(BuildRequirement(template.Requirement, form), latex, hint, form, expectedAnswer, values);
        }

        public static Dictionary<string, int> SampleValues(ExerciseTemplate template, PracticeRandom random)
        {
            var values = new Dictionary<string, int>();

            if (template.IsRandom && template.Variables != null && template.Variables.Count > 0)
            {
                foreach (var (name, range) in template.Variables)
                {
                    var minimum = range != null && range.Length > 0 ? range[0] : 1;
                    var maximum = range != null && range.Length > 1 ? range[1] : 10;

                    if (maximum < minimum)
                    {
                        (minimum, maximum) = (maximum, minimum);
                    }

                    values[name] = random.Next(minimum, maximum + 1);
                }
            }
            else
            {
                values["a"] = random.Next(2, 10);
            }

            return values;
        }

        public static string Substitute(string text, IReadOnlyDictionary<string, int> values)
        {
            return Placeholder.Replace(text, match =>
            {
                var content = match.Groups[1].Value.Trim();

                if (values.TryGetValue(content, out var value))
                {
                    return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                if (content.Length == 0 || !ArithmeticContent.IsMatch(content))
                {
                    return match.Value;
                }

                var identifiers = Identifier.Matches(content).Select(m => m.Value).ToList();

                if (identifiers.Count == 0 || identifiers.Any(name => !values.ContainsKey(name)))
                {
                    return match.Value;
                }

                var arithmetic = Identifier.Replace(content, m => $"({values[m.Value]})");

                try
                {
                    return AnswerChecker.FormatNumber(AnswerChecker.EvaluateNumber(arithmetic));
                }
                catch (MathExpressionParseException)
                {
                    return match.Value;
                }
            });
        }

        private static string BuildRequirement(string? requirement, AnswerForm form)
        {
            if (!string.IsNullOrWhiteSpace(requirement) && !requirement.Contains(LegacyRequirementMarker, StringComparison.OrdinalIgnoreCase))
            {
                return requirement.Trim();
            }

            return form switch
            {
                AnswerForm.Expanded => "Expand the expression:",
                AnswerForm.Factored => "Factor the expression:",
                AnswerForm.Simplified => "Simplify the expression:",
                _ => "Calculate the coefficient of x in the primitive function:"
            };
        }
    }
}
