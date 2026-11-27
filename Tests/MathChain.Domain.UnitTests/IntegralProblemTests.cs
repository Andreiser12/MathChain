using FluentAssertions;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;

namespace MathChain.Domain.UnitTests
{
    public class IntegralProblemTests
    {
        [Fact]
        public void GetProblemStatement_ShouldReturnTheCorrectFormat_WhenTheProblemHasBounds()
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
        public void ValidateSolution_ShouldReturnTrue_WhenTheSolutionIsCorrect()
        {
            var integralProblem = new IntegralProblem
            {
                CorrectSolution = 0.333
            };
            var result = integralProblem.ValidateSolution("0.333");
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateSolution_ShouldReturnFalse_WhenTheSolutionIsWrong()
        {
            var integralProblem = new IntegralProblem
            {
                CorrectSolution = 0.333
            };
            var result = integralProblem.ValidateSolution("0.999");
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateSolution_ShouldReturnTrue_WhenTheSolutionIsApproximate()
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
