using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Application.UnitTests.Practice
{
    public class ExerciseGeneratorTests
    {
        private sealed class SequenceRandom : PracticeRandom
        {
            private readonly Queue<int> _values;

            public SequenceRandom(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public List<(int Minimum, int MaximumExclusive)> Calls { get; } = new();

            public override int Next(int minimum, int maximumExclusive)
            {
                Calls.Add((minimum, maximumExclusive));
                return _values.Dequeue();
            }
        }

        private static ExerciseTemplate Template(string query, string latex, params (string Name, int Minimum, int Maximum)[] variables)
        {
            return new ExerciseTemplate
            {
                Difficulty = "Easy",
                WolframQuery = query,
                LatexTemplate = latex,
                Variables = variables.ToDictionary(v => v.Name, v => new[] { v.Minimum, v.Maximum })
            };
        }

        [Fact]
        public void Generate_ShouldSubstituteTheValuesAndKeepTheLatexGroups_WhenTheQueryIsAnExpansion()
        {
            var template = Template("expand ((1/{a})*x + {b})^2", @"(\frac{1}{{a}}x + {b})^2", ("a", 2, 4), ("b", 1, 3));

            var exercise = ExerciseGenerator.Generate(template, new SequenceRandom(3, 2));

            Assert.Equal(AnswerForm.Expanded, exercise.Form);
            Assert.Equal("((1/3)*x + 2)^2", exercise.ExpectedAnswer);
            Assert.Equal(@"(\frac{1}{3}x + 2)^2", exercise.Latex);
            Assert.Equal("Expand the expression:", exercise.Requirement);
        }

        [Fact]
        public void Generate_ShouldKeepBothSides_WhenTheLatexIsAnEquation()
        {
            var template = Template("{a}", @"2^{x} = 2^{{a}}", ("a", 3, 3));

            var exercise = ExerciseGenerator.Generate(template, new SequenceRandom(3));

            Assert.Equal(AnswerForm.Number, exercise.Form);
            Assert.Equal("3", exercise.ExpectedAnswer);
            Assert.Equal(@"2^{x} = 2^{3}", exercise.Latex);
        }

        [Fact]
        public void Generate_ShouldEvaluateThePlaceholders_WhenTheyAreComputed()
        {
            var template = Template("factor x^4 - {a**4} + {a*a*a}", @"x^4 - {a**4} + {a*a*a}", ("a", 2, 3));

            var exercise = ExerciseGenerator.Generate(template, new SequenceRandom(3));

            Assert.Equal(AnswerForm.Factored, exercise.Form);
            Assert.Equal("x^4 - 81 + 27", exercise.ExpectedAnswer);
            Assert.Equal("x^4 - 81 + 27", exercise.Latex);
        }

        [Fact]
        public void Generate_ShouldLeaveTheLatexGroupsAlone_WhenTheyHaveNoTemplateVariables()
        {
            var template = Template("expand ({a}x + {b}/x)^2", @"({a}x + \frac{{{b}}}{{x}})^{2}", ("a", 2, 4), ("b", 1, 3));

            var exercise = ExerciseGenerator.Generate(template, new SequenceRandom(4, 1));

            Assert.Equal(@"(4x + \frac{{1}}{{x}})^{2}", exercise.Latex);
            Assert.Equal("(4x + 1/x)^2", exercise.ExpectedAnswer);
        }

        [Fact]
        public void Generate_ShouldStoreARoundedNumber_WhenTheQueryIsNumeric()
        {
            var template = Template("({a}/{b})", @"\int \frac{{a}}{{b}} \,dx", ("a", 6, 30), ("b", 2, 5));

            var exercise = ExerciseGenerator.Generate(template, new SequenceRandom(7, 3));

            Assert.Equal(AnswerForm.Number, exercise.Form);
            Assert.Equal("2.333", exercise.ExpectedAnswer);
            Assert.Equal(@"\int \frac{7}{3} \,dx", exercise.Latex);
        }

        [Fact]
        public void Generate_ShouldSampleEveryVariableInsideItsInclusiveRange_WhenTheTemplateHasVariables()
        {
            var random = new SequenceRandom(5, 9);

            ExerciseGenerator.Generate(Template("expand ({a}x + {b})^2", "({a}x + {b})^2", ("a", 2, 5), ("b", 9, 1)), random);

            Assert.Equal(new[] { (2, 6), (1, 10) }, random.Calls);
        }

        [Fact]
        public void Generate_ShouldUseTheLegacyRandomA_WhenTheTemplateHasNoVariables()
        {
            var random = new SequenceRandom(4);
            var template = new ExerciseTemplate { WolframQuery = "{a}", LatexTemplate = @"\int {a} \,dx", IsRandom = false };

            var exercise = ExerciseGenerator.Generate(template, random);

            Assert.Equal((2, 10), Assert.Single(random.Calls));
            Assert.Equal("4", exercise.ExpectedAnswer);
        }

        [Fact]
        public void Generate_ShouldKeepTheRequirementAndTheHintWithSubstitutedValues_WhenTheyAreCustom()
        {
            var template = Template("expand ({a}x + 1)^2", "({a}x + 1)^2", ("a", 2, 4));
            template.Requirement = "Dezvolta:";
            template.Hint = "({a}x)^2 = {a*a}x^2";

            var exercise = ExerciseGenerator.Generate(template, new SequenceRandom(3));

            Assert.Equal("Dezvolta:", exercise.Requirement);
            Assert.Equal("(3x)^2 = 9x^2", exercise.Hint);
        }

        [Theory]
        [InlineData("simplify |\\pm x|=x", "|\\pm x|=x")]
        [InlineData("factor x^n - 1", "x^n - 1")]
        [InlineData("", "x")]
        [InlineData("expand ({a} + 1)^2", "({a} + 1)^2")]
        [InlineData("limit x->0 sin(x)/x", "x")]
        public void Generate_ShouldThrow_WhenTheExerciseCannotBeVerified(string query, string latex)
        {
            Assert.Throws<MathExpressionParseException>(() => ExerciseGenerator.Generate(Template(query, latex, ("a", 2, 5)), new SequenceRandom(2)));
        }
    }
}
