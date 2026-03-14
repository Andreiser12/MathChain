using System;
using System.Text;
using System.Globalization;

namespace MathChain.Domain.Entities
{
    public class IntegralProblem : MathProblem
    {
        public string Expression { get; set; } = string.Empty;
        public double CorrectSolution { get; set; }
        public string Variable { get; set; } = "x";
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }

        public override string GetProblemStatement()
        {
            return $"Calculeaza:" +
                $" ∫ ({Expression}) d{Variable} pe intervalul [{LowerBound}, {UpperBound}]";
        }

        public override bool ValidateSolution(string solution)
        {
            const int epsilon = 10;

            if(double.TryParse(solution, System.Globalization.NumberStyles.Float,
                CultureInfo.InvariantCulture, out double parsedUserSolution))
            {
                double userSolution = parsedUserSolution * 1000;
                double correctSolution = CorrectSolution * 1000;

                return Math.Abs(userSolution - correctSolution) < epsilon;
            }

            return false;
        }
    }
}
