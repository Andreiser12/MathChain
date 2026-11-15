using MathChain.Application.Practice;
using MathChain.Domain.Enums;

namespace MathChain.Tests.Practice
{
    public class AnswerCheckerTests
    {
        private static AnswerVerdict Check(AnswerForm form, string expected, string answer)
        {
            return AnswerChecker.Check(form, expected, answer).Verdict;
        }

        [Theory]
        [InlineData("x^2 + 6x + 9")]
        [InlineData("9 + 6*x + x^2")]
        [InlineData("x*x + 3x + 3x + 9")]
        [InlineData("x² + 6x + 9")]
        public void Expanded_EquivalentPolynomial_IsCorrect(string answer)
        {
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Expanded, "(x + 3)^2", answer));
        }

        [Theory]
        [InlineData("(x + 3)^2")]
        [InlineData("(x + 3)(x + 3)")]
        [InlineData("x(x + 6) + 9")]
        [InlineData("-(-x^2 - 6x - 9)")]
        public void Expanded_EquivalentButNotExpanded_IsWrongForm(string answer)
        {
            Assert.Equal(AnswerVerdict.WrongForm, Check(AnswerForm.Expanded, "(x + 3)^2", answer));
        }

        [Fact]
        public void Expanded_ConstantFractionsAndRoots_AreAllowed()
        {
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Expanded, "((1/3)*x + 2)^2", "(1/9)x^2 + (4/3)x + 4"));
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Expanded, "(sqrt(5)*x + 2)^2", "5x^2 + 4sqrt(5)x + 4"));
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Expanded, "(3x + 2/x)^2", "9x^2 + 12 + 4/x^2"));
        }

        [Theory]
        [InlineData("x^2 + 6x + 8")]
        [InlineData("x^2 + 9")]
        [InlineData("x^2 + 6y + 9")]
        public void Expanded_DifferentPolynomial_IsIncorrect(string answer)
        {
            Assert.Equal(AnswerVerdict.Incorrect, Check(AnswerForm.Expanded, "(x + 3)^2", answer));
        }

        [Fact]
        public void Expanded_RoundedRoot_IsIncorrect()
        {
            Assert.Equal(AnswerVerdict.Incorrect, Check(AnswerForm.Expanded, "(sqrt(5)*x + 2)^2", "5x^2 + 8.944x + 4"));
        }

        [Theory]
        [InlineData("(x - 3)(x + 3)")]
        [InlineData("(x + 3)*(x - 3)")]
        [InlineData("-(3 - x)(x + 3)")]
        [InlineData("2(x/2 - 3/2)(x + 3)")]
        public void Factored_ProductOfFactors_IsCorrect(string answer)
        {
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Factored, "x^2 - 9", answer));
        }

        [Theory]
        [InlineData("x^2 - 9")]
        [InlineData("(x^2 - 9)")]
        [InlineData("1*(x^2 - 9)")]
        [InlineData("x*x - 9")]
        public void Factored_EquivalentButNotFactored_IsWrongForm(string answer)
        {
            Assert.Equal(AnswerVerdict.WrongForm, Check(AnswerForm.Factored, "x^2 - 9", answer));
        }

        [Fact]
        public void Factored_RepeatedFactorAndIrrationalRoots_AreCorrect()
        {
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Factored, "x^2 - 6x + 9", "(x - 3)^2"));
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Factored, "x^2 - 3", "(x - sqrt(3))(x + sqrt(3))"));
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Factored, "x^3 + 5", "(x + cbrt(5))(x^2 - cbrt(5)x + cbrt(5)^2)"));
        }

        [Fact]
        public void Factored_WrongFactors_AreIncorrect()
        {
            Assert.Equal(AnswerVerdict.Incorrect, Check(AnswerForm.Factored, "x^2 - 9", "(x - 3)(x - 3)"));
        }

        [Fact]
        public void Simplified_RejectsTheOriginalPowersOfSums()
        {
            const string expected = "(3x + 2)^2 - (x - 1)^2";

            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Simplified, expected, "8x^2 + 14x + 3"));
            Assert.Equal(AnswerVerdict.Correct, Check(AnswerForm.Simplified, expected, "(2x + 3)(4x + 1)"));
            Assert.Equal(AnswerVerdict.WrongForm, Check(AnswerForm.Simplified, expected, expected));
            Assert.Equal(AnswerVerdict.Incorrect, Check(AnswerForm.Simplified, expected, "8x^2 + 14x"));
        }

        [Theory]
        [InlineData("2.333", AnswerVerdict.Correct)]
        [InlineData("2.34", AnswerVerdict.Correct)]
        [InlineData("2,333", AnswerVerdict.Correct)]
        [InlineData("7/3", AnswerVerdict.Correct)]
        [InlineData("2.4", AnswerVerdict.Incorrect)]
        [InlineData("x", AnswerVerdict.Unreadable)]
        [InlineData("abc", AnswerVerdict.Unreadable)]
        public void Number_UsesTheOldToleranceOfFifteenThousandths(string answer, AnswerVerdict verdict)
        {
            Assert.Equal(verdict, Check(AnswerForm.Number, "2.333", answer));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("x^2 + ")]
        [InlineData("x = 3")]
        [InlineData("n^2")]
        public void UnreadableAnswer_IsReportedWithoutJudging(string? answer)
        {
            var result = AnswerChecker.Check(AnswerForm.Expanded, "(x + 3)^2", answer);

            Assert.Equal(AnswerVerdict.Unreadable, result.Verdict);
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
        }

        [Fact]
        public void Equivalence_NeedsEnoughDefinedPoints()
        {
            var expected = MathExpressionParser.Parse("sqrt(-x^2 - 1)", AnswerChecker.Variables);

            Assert.False(AnswerChecker.AreEquivalent(expected, expected));
        }
    }
}
