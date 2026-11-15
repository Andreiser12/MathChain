using MathChain.Application.Practice;

namespace MathChain.Tests.Practice
{
    public class MathExpressionParserTests
    {
        private static double Evaluate(string input, double x = 2, double y = 3, double z = 5)
        {
            return MathExpressionParser.Parse(input, AnswerChecker.Variables)
                .Evaluate(new Dictionary<char, double> { ['x'] = x, ['y'] = y, ['z'] = z });
        }

        [Theory]
        [InlineData("2+3*4", 14)]
        [InlineData("(2+3)*4", 20)]
        [InlineData("2^3^2", 512)]
        [InlineData("-2^2", -4)]
        [InlineData("2*-3", -6)]
        [InlineData("7/2", 3.5)]
        [InlineData("2,5 + 1", 3.5)]
        [InlineData("sqrt(16) + cbrt(27)", 7)]
        [InlineData("2**3", 8)]
        [InlineData("3² − 1", 8)]
        [InlineData("[1+2]*3", 9)]
        [InlineData("ln(e^3)", 3)]
        [InlineData("2e - e", Math.E)]
        [InlineData("E", Math.E)]
        [InlineData("4arctan(1)", Math.PI)]
        [InlineData("2arcsin(1)", Math.PI)]
        [InlineData("arccos(-1)", Math.PI)]
        [InlineData("exp(1)", Math.E)]
        [InlineData("3pi/3", Math.PI)]
        [InlineData("4e/2", 2 * Math.E)]
        public void Parse_Arithmetic_EvaluatesWithUsualPrecedence(string input, double expected)
        {
            Assert.Equal(expected, Evaluate(input), 10);
        }

        [Theory]
        [InlineData("3x", 6)]
        [InlineData("3x^2", 12)]
        [InlineData("xy", 6)]
        [InlineData("2xy^2", 36)]
        [InlineData("(x-3)(x+3)", -5)]
        [InlineData("x(x+1)", 6)]
        [InlineData("sqrt(4)x", 4)]
        [InlineData("2(x+1)^2", 18)]
        [InlineData("1/2x", 1)]
        [InlineData("x^-1", 0.5)]
        [InlineData("X + Y", 5)]
        [InlineData("x + y + z", 10)]
        public void Parse_ImplicitMultiplication_FollowsWrittenMathematics(string input, double expected)
        {
            Assert.Equal(expected, Evaluate(input), 10);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("x +")]
        [InlineData("(x + 1")]
        [InlineData("x + 1)")]
        [InlineData("2 3")]
        [InlineData("n + 1")]
        [InlineData("x = 3")]
        [InlineData("\\frac{1}{2}")]
        [InlineData("sqrt 4")]
        [InlineData("1.2.3")]
        [InlineData("2 pi 3")]
        public void Parse_InvalidInput_Throws(string input)
        {
            Assert.Throws<MathExpressionParseException>(() => MathExpressionParser.Parse(input, AnswerChecker.Variables));
        }

        [Fact]
        public void Parse_TooLongOrTooDeep_IsRejected()
        {
            Assert.Throws<MathExpressionParseException>(() => MathExpressionParser.Parse(new string('1', MathExpressionParser.MaximumLength + 1), AnswerChecker.Variables));
            Assert.Throws<MathExpressionParseException>(() => MathExpressionParser.Parse(new string('(', 100) + "x" + new string(')', 100), AnswerChecker.Variables));
            Assert.Throws<MathExpressionParseException>(() => MathExpressionParser.Parse(string.Concat(Enumerable.Repeat("-", 100)) + "x", AnswerChecker.Variables));
        }

        [Fact]
        public void Parse_VariablesNotAllowed_AreRejected()
        {
            Assert.Throws<MathExpressionParseException>(() => MathExpressionParser.Parse("x + 1", Array.Empty<char>()));
        }
    }
}
