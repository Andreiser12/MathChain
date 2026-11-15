namespace MathChain.Application.Practice
{
    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power
    }

    public abstract class MathExpression
    {
        public abstract bool IsConstant { get; }

        public abstract double Evaluate(IReadOnlyDictionary<char, double> variables);
    }

    public sealed class NumberExpression : MathExpression
    {
        public NumberExpression(double value)
        {
            Value = value;
        }

        public double Value { get; }

        public override bool IsConstant => true;

        public override double Evaluate(IReadOnlyDictionary<char, double> variables)
        {
            return Value;
        }
    }

    public sealed class VariableExpression : MathExpression
    {
        public VariableExpression(char name)
        {
            Name = name;
        }

        public char Name { get; }

        public override bool IsConstant => false;

        public override double Evaluate(IReadOnlyDictionary<char, double> variables)
        {
            return variables.TryGetValue(Name, out var value) ? value : double.NaN;
        }
    }

    public sealed class NegateExpression : MathExpression
    {
        public NegateExpression(MathExpression operand)
        {
            Operand = operand;
        }

        public MathExpression Operand { get; }

        public override bool IsConstant => Operand.IsConstant;

        public override double Evaluate(IReadOnlyDictionary<char, double> variables)
        {
            return -Operand.Evaluate(variables);
        }
    }

    public sealed class BinaryExpression : MathExpression
    {
        public BinaryExpression(BinaryOperator op, MathExpression left, MathExpression right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        public BinaryOperator Operator { get; }
        public MathExpression Left { get; }
        public MathExpression Right { get; }

        public bool IsSum => Operator is BinaryOperator.Add or BinaryOperator.Subtract;

        public override bool IsConstant => Left.IsConstant && Right.IsConstant;

        public override double Evaluate(IReadOnlyDictionary<char, double> variables)
        {
            var left = Left.Evaluate(variables);
            var right = Right.Evaluate(variables);

            return Operator switch
            {
                BinaryOperator.Add => left + right,
                BinaryOperator.Subtract => left - right,
                BinaryOperator.Multiply => left * right,
                BinaryOperator.Divide => left / right,
                BinaryOperator.Power => Math.Pow(left, right),
                _ => double.NaN
            };
        }
    }

    public sealed class FunctionExpression : MathExpression
    {
        public FunctionExpression(string name, MathExpression argument)
        {
            Name = name;
            Argument = argument;
        }

        public string Name { get; }
        public MathExpression Argument { get; }

        public override bool IsConstant => Argument.IsConstant;

        public override double Evaluate(IReadOnlyDictionary<char, double> variables)
        {
            var value = Argument.Evaluate(variables);

            return Name switch
            {
                "sqrt" => Math.Sqrt(value),
                "cbrt" => Math.Cbrt(value),
                "abs" => Math.Abs(value),
                "arcsin" => Math.Asin(value),
                "arccos" => Math.Acos(value),
                "arctan" => Math.Atan(value),
                "sin" => Math.Sin(value),
                "cos" => Math.Cos(value),
                "tan" => Math.Tan(value),
                "ln" => Math.Log(value),
                "exp" => Math.Exp(value),
                _ => double.NaN
            };
        }
    }
}
