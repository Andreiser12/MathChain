using System.Globalization;
using System.Text;

namespace MathChain.Application.Practice
{
    public class MathExpressionParseException : Exception
    {
        public MathExpressionParseException(string message) : base(message)
        {
        }
    }

    public class MathExpressionParser
    {
        public const int MaximumLength = 300;
        private const int MaximumDepth = 64;

        private static readonly string[] FunctionNames = { "sqrt", "cbrt", "abs", "arcsin", "arccos", "arctan", "sin", "cos", "tan", "exp", "ln" };

        private readonly ISet<char> _allowedVariables;
        private List<Token> _tokens = new();
        private int _position;
        private int _depth;

        private MathExpressionParser(ISet<char> allowedVariables)
        {
            _allowedVariables = allowedVariables;
        }

        public static MathExpression Parse(string input, IEnumerable<char> allowedVariables)
        {
            return new MathExpressionParser(new HashSet<char>(allowedVariables)).ParseInput(input);
        }

        public static bool TryParse(string input, IEnumerable<char> allowedVariables, out MathExpression? expression)
        {
            try
            {
                expression = Parse(input, allowedVariables);
                return true;
            }
            catch (MathExpressionParseException)
            {
                expression = null;
                return false;
            }
        }

        public static string Normalize(string input)
        {
            var builder = new StringBuilder(input.Length);

            foreach (var character in input)
            {
                switch (character)
                {
                    case '−':
                    case '–':
                        builder.Append('-');
                        break;
                    case '×':
                    case '·':
                    case '⋅':
                        builder.Append('*');
                        break;
                    case '÷':
                        builder.Append('/');
                        break;
                    case '²':
                        builder.Append("^2");
                        break;
                    case '³':
                        builder.Append("^3");
                        break;
                    default:
                        builder.Append(char.ToLowerInvariant(character));
                        break;
                }
            }

            return builder.ToString().Replace("**", "^");
        }

        private MathExpression ParseInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new MathExpressionParseException("The expression is empty.");
            }

            if (input.Length > MaximumLength)
            {
                throw new MathExpressionParseException("The expression is too long.");
            }

            _tokens = Tokenize(Normalize(input));
            _position = 0;
            _depth = 0;

            var expression = ParseSum();

            if (Current.Kind != TokenKind.End)
            {
                throw new MathExpressionParseException($"Unexpected '{Current.Text}'.");
            }

            return expression;
        }

        private MathExpression ParseSum()
        {
            var expression = ParseProduct();

            while (Current.Kind == TokenKind.Operator && (Current.Text == "+" || Current.Text == "-"))
            {
                var op = Current.Text == "+" ? BinaryOperator.Add : BinaryOperator.Subtract;
                _position++;
                expression = new BinaryExpression(op, expression, ParseProduct());
            }

            return expression;
        }

        private MathExpression ParseProduct()
        {
            var expression = ParseUnary();

            while (true)
            {
                if (Current.Kind == TokenKind.Operator && (Current.Text == "*" || Current.Text == "/"))
                {
                    var op = Current.Text == "*" ? BinaryOperator.Multiply : BinaryOperator.Divide;
                    _position++;
                    expression = new BinaryExpression(op, expression, ParseUnary());
                }
                else if (Current.Kind is TokenKind.Variable or TokenKind.Function or TokenKind.OpenParenthesis || IsNamedConstant(Current))
                {
                    expression = new BinaryExpression(BinaryOperator.Multiply, expression, ParsePower());
                }
                else
                {
                    return expression;
                }
            }
        }

        private static bool IsNamedConstant(Token token)
        {
            return token.Kind == TokenKind.Number && (token.Text == "pi" || token.Text == "e");
        }

        private MathExpression ParseUnary()
        {
            if (Current.Kind == TokenKind.Operator && (Current.Text == "-" || Current.Text == "+"))
            {
                var negate = Current.Text == "-";
                _position++;
                EnterNesting();
                var operand = ParseUnary();
                _depth--;

                return negate ? new NegateExpression(operand) : operand;
            }

            return ParsePower();
        }

        private MathExpression ParsePower()
        {
            var expression = ParsePrimary();

            if (Current.Kind == TokenKind.Operator && Current.Text == "^")
            {
                _position++;
                EnterNesting();
                var exponent = ParseUnary();
                _depth--;
                expression = new BinaryExpression(BinaryOperator.Power, expression, exponent);
            }

            return expression;
        }

        private MathExpression ParsePrimary()
        {
            var token = Current;

            switch (token.Kind)
            {
                case TokenKind.Number:
                    _position++;
                    return new NumberExpression(token.Value);
                case TokenKind.Variable:
                    _position++;
                    return new VariableExpression(token.Text[0]);
                case TokenKind.Function:
                    _position++;
                    Expect(TokenKind.OpenParenthesis, $"Expected '(' after {token.Text}.");
                    var argument = ParseGroupContent();
                    return new FunctionExpression(token.Text, argument);
                case TokenKind.OpenParenthesis:
                    _position++;
                    return ParseGroupContent();
                case TokenKind.End:
                    throw new MathExpressionParseException("The expression ends too early.");
                default:
                    throw new MathExpressionParseException($"Unexpected '{token.Text}'.");
            }
        }

        private MathExpression ParseGroupContent()
        {
            EnterNesting();
            var expression = ParseSum();
            _depth--;
            Expect(TokenKind.CloseParenthesis, "A closing parenthesis is missing.");

            return expression;
        }

        private void EnterNesting()
        {
            if (++_depth > MaximumDepth)
            {
                throw new MathExpressionParseException("The expression is nested too deeply.");
            }
        }

        private void Expect(TokenKind kind, string message)
        {
            if (Current.Kind != kind)
            {
                throw new MathExpressionParseException(message);
            }

            _position++;
        }

        private Token Current => _tokens[_position];

        private List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var index = 0;

            while (index < input.Length)
            {
                var character = input[index];

                if (char.IsWhiteSpace(character))
                {
                    index++;
                }
                else if (char.IsDigit(character) || ((character == '.' || character == ',') && index + 1 < input.Length && char.IsDigit(input[index + 1])))
                {
                    var start = index;
                    var seenSeparator = false;

                    while (index < input.Length && (char.IsDigit(input[index]) || (!seenSeparator && (input[index] == '.' || input[index] == ','))))
                    {
                        seenSeparator |= input[index] == '.' || input[index] == ',';
                        index++;
                    }

                    var text = input[start..index].Replace(',', '.');

                    if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
                    {
                        throw new MathExpressionParseException($"'{text}' is not a valid number.");
                    }

                    tokens.Add(new Token(TokenKind.Number, text, value));
                }
                else if (char.IsLetter(character))
                {
                    var function = FunctionNames.FirstOrDefault(name => string.CompareOrdinal(input, index, name, 0, name.Length) == 0);

                    if (function != null)
                    {
                        tokens.Add(new Token(TokenKind.Function, function));
                        index += function.Length;
                    }
                    else if (string.CompareOrdinal(input, index, "pi", 0, 2) == 0)
                    {
                        tokens.Add(new Token(TokenKind.Number, "pi", Math.PI));
                        index += 2;
                    }
                    else if (character == 'e' && !_allowedVariables.Contains('e'))
                    {
                        tokens.Add(new Token(TokenKind.Number, "e", Math.E));
                        index++;
                    }
                    else if (_allowedVariables.Contains(character))
                    {
                        tokens.Add(new Token(TokenKind.Variable, character.ToString()));
                        index++;
                    }
                    else
                    {
                        throw new MathExpressionParseException($"Unknown symbol '{character}'.");
                    }
                }
                else if ("+-*/^".IndexOf(character) >= 0)
                {
                    tokens.Add(new Token(TokenKind.Operator, character.ToString()));
                    index++;
                }
                else if (character is '(' or '[')
                {
                    tokens.Add(new Token(TokenKind.OpenParenthesis, "("));
                    index++;
                }
                else if (character is ')' or ']')
                {
                    tokens.Add(new Token(TokenKind.CloseParenthesis, ")"));
                    index++;
                }
                else
                {
                    throw new MathExpressionParseException($"Unknown symbol '{character}'.");
                }
            }

            tokens.Add(new Token(TokenKind.End, "end of expression"));

            return tokens;
        }

        private enum TokenKind
        {
            Number,
            Variable,
            Function,
            Operator,
            OpenParenthesis,
            CloseParenthesis,
            End
        }

        private readonly record struct Token(TokenKind Kind, string Text, double Value = 0);
    }
}
