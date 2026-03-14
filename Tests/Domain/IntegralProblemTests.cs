using FluentAssertions;
using MathChain.Domain.Entities;

namespace MathChain.Tests.Domain
{
    public class IntegralProblemTests
    {
        [Fact]
        public void GetProblemStatement_ReturnCorrectFormat()
        {
            var integralProblem = new IntegralProblem
            {
                Expression = "x^2",
                Variable = "x",
                LowerBound = 0,
                UpperBound = 1
            };

            var result = integralProblem.GetProblemStatement();

            result.Should().Contain("∫");
            result.Should().Contain("x^2");
        }

        [Fact]
        public void ValidateSolution_CorrectSolution_ReturnsTrue()
        {
            var integralProblem = new IntegralProblem
            {
                CorrectSolution = 0.333
            };
            var result = integralProblem.ValidateSolution("0.333");
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateSolution_WrongSolution_ReturnsFalse()
        {
            var integralProblem = new IntegralProblem
            {
                CorrectSolution = 0.333
            };
            var result = integralProblem.ValidateSolution("0.999");
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateSolution_ApproximateSolution_ReturnsTrue()
        {
            var integralProblem = new IntegralProblem
            {
                CorrectSolution = 0.333
            };
            var result = integralProblem.ValidateSolution("0.334");
            result.Should().BeTrue();
        }
    }
}
