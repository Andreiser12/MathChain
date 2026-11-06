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
            if (string.IsNullOrWhiteSpace(solution)) return false;

            const double epsilon = 0.015;
            string clean = solution.Trim().Replace(',', '.');

            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedUserSolution))
            {
                return Math.Abs(parsedUserSolution - CorrectSolution) <= epsilon;
            }

            return false;
        }
    }
}
