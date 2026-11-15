using System.Globalization;
using MathChain.Domain.Enums;

namespace MathChain.Application.Practice
{
    public enum AnswerVerdict
    {
        Correct,
        Incorrect,
        WrongForm,
        Unreadable
    }

    public readonly record struct AnswerCheck(AnswerVerdict Verdict, string Message);

    public static class AnswerChecker
    {
        public const double NumericTolerance = 0.015;

        public static readonly char[] Variables = { 'x', 'y', 'z' };

        private const double RelativeTolerance = 1e-7;
        private const int MinimumValidPoints = 4;

        private static readonly double[][] SamplePoints =
        {
            new[] { 0.731, 1.419, -0.659 },
            new[] { -1.287, 0.563, 1.271 },
            new[] { 1.613, -0.877, 0.443 },
            new[] { -0.429, 1.931, -1.517 },
            new[] { 2.197, -1.143, 0.829 },
            new[] { 0.913, 0.317, 1.703 },
            new[] { -1.861, 1.087, -0.271 }
        };

        public static AnswerCheck Check(AnswerForm form, string expected, string? answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return new AnswerCheck(AnswerVerdict.Unreadable, "Please enter an answer.");
            }

            return form == AnswerForm.Number
                ? CheckNumber(expected, answer)
                : CheckExpression(form, expected, answer);
        }

        public static double EvaluateNumber(string expression)
        {
            var value = MathExpressionParser.Parse(expression, Array.Empty<char>()).Evaluate(new Dictionary<char, double>());

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new MathExpressionParseException("The expression does not evaluate to a number.");
            }

            return value;
        }

        public static string FormatNumber(double value)
        {
            return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static AnswerCheck CheckNumber(string expected, string answer)
        {
            double value;

            try
            {
                value = EvaluateNumber(answer);
            }
            catch (MathExpressionParseException)
            {
                return new AnswerCheck(AnswerVerdict.Unreadable, "Please enter a number, for example 7, -14 or 2.5.");
            }

            var expectedValue = double.Parse(expected, CultureInfo.InvariantCulture);

            return Math.Abs(value - expectedValue) <= NumericTolerance
                ? new AnswerCheck(AnswerVerdict.Correct, "Correct!")
                : new AnswerCheck(AnswerVerdict.Incorrect, "Incorrect answer. Please try again.");
        }

        private static AnswerCheck CheckExpression(AnswerForm form, string expected, string answer)
        {
            MathExpression student;

            try
            {
                student = MathExpressionParser.Parse(answer, Variables);
            }
            catch (MathExpressionParseException exception)
            {
                return new AnswerCheck(AnswerVerdict.Unreadable,
                    $"Your answer could not be read: {exception.Message} Use x, y, z, numbers, + - * / ^, sqrt() and parentheses.");
            }

            var reference = MathExpressionParser.Parse(expected, Variables);

            if (!AreEquivalent(reference, student))
            {
                return new AnswerCheck(AnswerVerdict.Incorrect, "Incorrect answer. Please try again.");
            }

            return form switch
            {
                AnswerForm.Expanded when !IsExpanded(student) =>
                    new AnswerCheck(AnswerVerdict.WrongForm, "Your expression is equivalent, but it is not fully expanded. Remove every product and power of parentheses."),
                AnswerForm.Factored when CountFactors(student) < 2 =>
                    new AnswerCheck(AnswerVerdict.WrongForm, "Your expression is equivalent, but it is not written as a product of factors."),
                AnswerForm.Simplified when ContainsPowerOfSum(student) =>
                    new AnswerCheck(AnswerVerdict.WrongForm, "Your expression is equivalent, but it can still be simplified."),
                _ => new AnswerCheck(AnswerVerdict.Correct, "Correct!")
            };
        }

        public static bool AreEquivalent(MathExpression first, MathExpression second)
        {
            var validPoints = 0;

            foreach (var point in SamplePoints)
            {
                var variables = new Dictionary<char, double> { ['x'] = point[0], ['y'] = point[1], ['z'] = point[2] };
                var a = first.Evaluate(variables);
                var b = second.Evaluate(variables);

                if (!double.IsFinite(a) || !double.IsFinite(b))
                {
                    continue;
                }

                validPoints++;

                if (Math.Abs(a - b) > RelativeTolerance * Math.Max(1, Math.Max(Math.Abs(a), Math.Abs(b))))
                {
                    return false;
                }
            }

            return validPoints >= MinimumValidPoints;
        }

        public static bool IsExpanded(MathExpression expression)
        {
            return expression switch
            {
                BinaryExpression { Operator: BinaryOperator.Power } power =>
                    !IsVariableSum(power.Left) && IsExpanded(power.Left) && IsExpanded(power.Right),
                BinaryExpression { Operator: BinaryOperator.Multiply } product =>
                    !IsVariableSum(product.Left) && !IsVariableSum(product.Right) && IsExpanded(product.Left) && IsExpanded(product.Right),
                BinaryExpression { Operator: BinaryOperator.Divide } quotient =>
                    !IsVariableSum(quotient.Right) && IsExpanded(quotient.Left) && IsExpanded(quotient.Right),
                BinaryExpression sum => IsExpanded(sum.Left) && IsExpanded(sum.Right),
                NegateExpression negate => !IsVariableSum(negate.Operand) && IsExpanded(negate.Operand),
                FunctionExpression function => IsExpanded(function.Argument),
                _ => true
            };
        }

        public static int CountFactors(MathExpression expression)
        {
            return expression switch
            {
                NegateExpression negate => CountFactors(negate.Operand),
                BinaryExpression { Operator: BinaryOperator.Multiply } product => CountFactor(product.Left) + CountFactor(product.Right),
                BinaryExpression { Operator: BinaryOperator.Divide } quotient when quotient.Right.IsConstant => CountFactors(quotient.Left),
                BinaryExpression { Operator: BinaryOperator.Power } => CountFactor(expression),
                _ => 0
            };
        }

        private static int CountFactor(MathExpression expression)
        {
            if (expression.IsConstant)
            {
                return 0;
            }

            return expression switch
            {
                NegateExpression negate => CountFactor(negate.Operand),
                BinaryExpression { Operator: BinaryOperator.Multiply } product => CountFactor(product.Left) + CountFactor(product.Right),
                BinaryExpression { Operator: BinaryOperator.Divide } quotient when quotient.Right.IsConstant => CountFactor(quotient.Left),
                BinaryExpression { Operator: BinaryOperator.Power, Right: NumberExpression exponent } power
                    when exponent.Value >= 1 && exponent.Value == Math.Floor(exponent.Value) && exponent.Value <= 1000 =>
                    CountFactor(power.Left) * (int)exponent.Value,
                _ => 1
            };
        }

        private static bool ContainsPowerOfSum(MathExpression expression)
        {
            return expression switch
            {
                BinaryExpression { Operator: BinaryOperator.Power } power =>
                    IsVariableSum(power.Left) || ContainsPowerOfSum(power.Left) || ContainsPowerOfSum(power.Right),
                BinaryExpression binary => ContainsPowerOfSum(binary.Left) || ContainsPowerOfSum(binary.Right),
                NegateExpression negate => ContainsPowerOfSum(negate.Operand),
                FunctionExpression function => ContainsPowerOfSum(function.Argument),
                _ => false
            };
        }

        private static bool IsVariableSum(MathExpression expression)
        {
            return expression switch
            {
                BinaryExpression { IsSum: true } sum => !sum.IsConstant,
                NegateExpression negate => IsVariableSum(negate.Operand),
                _ => false
            };
        }
    }
}
