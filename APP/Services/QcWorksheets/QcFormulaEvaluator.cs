using System.Globalization;
using System.Text;

namespace APP.Services.QcWorksheets;

/// <summary>
/// One aggregate reference inside a formula, e.g. <c>AVG({dissolution_table.abs_spl})</c>.
/// </summary>
public sealed record QcFormulaTableReference(string Function, string TableFieldKey, string ColumnKey);

/// <summary>The result of parsing a formula without evaluating it.</summary>
public sealed record QcFormulaAnalysis(
    bool IsValid,
    string Error,
    IReadOnlyList<string> ScalarFieldKeys,
    IReadOnlyList<QcFormulaTableReference> TableReferences);

/// <summary>Supplies values at evaluation time.</summary>
public interface IQcFormulaValueResolver
{
    bool TryGetScalar(string fieldKey, out double value);

    bool TryGetColumn(string tableFieldKey, string columnKey, out IReadOnlyList<double> values);
}

/// <summary>
/// A small, local recursive-descent evaluator for QC worksheet formulas.
/// <para>
/// Deliberately <b>not</b> the existing <c>FormulaWorksheetPreprocessor</c>: that is a thin
/// adapter onto an external formula microservice with its own versioned DSL and
/// hash-locked definitions. Taking a network dependency for every QC field calculation is
/// the wrong tradeoff for something that must feel instant in the Test Room.
/// </para>
/// <para>
/// Grammar:
/// <code>
/// expression := term (('+' | '-') term)*
/// term       := unary (('*' | '/') unary)*
/// unary      := '-'? primary
/// primary    := number | '{' field_key '}' | FUNC '(' '{' table '.' column '}' ')' | '(' expression ')'
/// FUNC       := AVG | SUM | MIN | MAX | RSD
/// </code>
/// References are worksheet-scoped, not section-scoped, and a CalculatedValue field may
/// reference another CalculatedValue field's key.
/// </para>
/// </summary>
public static class QcFormulaEvaluator
{
    private static readonly string[] Functions = ["AVG", "SUM", "MIN", "MAX", "RSD"];

    /// <summary>
    /// Parses a formula and reports its references, without needing any values. Used at
    /// template-save time to reject a formula that is malformed or that references a field
    /// key which does not exist in the template.
    /// </summary>
    public static QcFormulaAnalysis Analyze(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return new QcFormulaAnalysis(false, "The formula is empty.", [], []);

        try
        {
            var parser = new Parser(formula);
            var node = parser.ParseExpression();
            parser.ExpectEnd();
            return new QcFormulaAnalysis(true, null, parser.ScalarKeys, parser.TableReferences);
        }
        catch (QcFormulaException ex)
        {
            return new QcFormulaAnalysis(false, ex.Message, [], []);
        }
    }

    /// <summary>
    /// Evaluates a formula against supplied values. Returns false with a reason when the
    /// formula is malformed, a reference cannot be resolved, or the arithmetic is invalid
    /// (division by zero, RSD of an empty column).
    /// </summary>
    public static bool TryEvaluate(
        string formula,
        IQcFormulaValueResolver resolver,
        out double result,
        out string error)
    {
        result = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(formula))
        {
            error = "The formula is empty.";
            return false;
        }

        try
        {
            var parser = new Parser(formula);
            var node = parser.ParseExpression();
            parser.ExpectEnd();
            result = node.Evaluate(resolver);

            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                error = "The formula produced a value that is not a finite number.";
                return false;
            }

            return true;
        }
        catch (QcFormulaException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // AST
    // -----------------------------------------------------------------------

    private abstract class Node
    {
        internal abstract double Evaluate(IQcFormulaValueResolver resolver);
    }

    private sealed class ConstantNode(double value) : Node
    {
        internal override double Evaluate(IQcFormulaValueResolver resolver) => value;
    }

    private sealed class ScalarNode(string fieldKey) : Node
    {
        internal override double Evaluate(IQcFormulaValueResolver resolver)
        {
            if (resolver is null || !resolver.TryGetScalar(fieldKey, out var value))
                throw new QcFormulaException($"No value is available for field '{fieldKey}'.");
            return value;
        }
    }

    private sealed class AggregateNode(string function, string tableKey, string columnKey) : Node
    {
        internal override double Evaluate(IQcFormulaValueResolver resolver)
        {
            if (resolver is null
                || !resolver.TryGetColumn(tableKey, columnKey, out var values)
                || values is null)
                throw new QcFormulaException(
                    $"No values are available for table column '{tableKey}.{columnKey}'.");

            var list = values.ToList();
            if (list.Count == 0)
                throw new QcFormulaException(
                    $"Table column '{tableKey}.{columnKey}' has no values to aggregate.");

            return function switch
            {
                "AVG" => list.Average(),
                "SUM" => list.Sum(),
                "MIN" => list.Min(),
                "MAX" => list.Max(),
                "RSD" => RelativeStandardDeviation(list, tableKey, columnKey),
                _ => throw new QcFormulaException($"Unknown function '{function}'.")
            };
        }

        /// <summary>Percent relative standard deviation, using the sample standard deviation.</summary>
        private static double RelativeStandardDeviation(
            List<double> values, string tableKey, string columnKey)
        {
            if (values.Count < 2)
                throw new QcFormulaException(
                    $"RSD needs at least two values in '{tableKey}.{columnKey}'.");

            var mean = values.Average();
            if (mean == 0)
                throw new QcFormulaException(
                    $"RSD is undefined for '{tableKey}.{columnKey}' because the mean is zero.");

            var variance = values.Sum(value => Math.Pow(value - mean, 2)) / (values.Count - 1);
            return Math.Sqrt(variance) / Math.Abs(mean) * 100d;
        }
    }

    private sealed class UnaryNode(Node operand) : Node
    {
        internal override double Evaluate(IQcFormulaValueResolver resolver) => -operand.Evaluate(resolver);
    }

    private sealed class BinaryNode(char op, Node left, Node right) : Node
    {
        internal override double Evaluate(IQcFormulaValueResolver resolver)
        {
            var l = left.Evaluate(resolver);
            var r = right.Evaluate(resolver);

            switch (op)
            {
                case '+': return l + r;
                case '-': return l - r;
                case '*': return l * r;
                case '/':
                    if (r == 0)
                        throw new QcFormulaException("The formula divides by zero.");
                    return l / r;
                default:
                    throw new QcFormulaException($"Unknown operator '{op}'.");
            }
        }
    }

    private sealed class QcFormulaException(string message) : Exception(message);

    // -----------------------------------------------------------------------
    // Parser
    // -----------------------------------------------------------------------

    private sealed class Parser
    {
        private readonly string _text;
        private int _position;

        private readonly List<string> _scalarKeys = [];
        private readonly List<QcFormulaTableReference> _tableReferences = [];

        internal IReadOnlyList<string> ScalarKeys => _scalarKeys;
        internal IReadOnlyList<QcFormulaTableReference> TableReferences => _tableReferences;

        internal Parser(string text)
        {
            _text = text;
            _position = 0;
        }

        internal Node ParseExpression()
        {
            var node = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (AtEnd())
                    return node;

                if (Peek() is '+' or '-')
                {
                    var op = Next();
                    node = new BinaryNode(op, node, ParseTerm());
                    continue;
                }

                return node;
            }
        }

        private Node ParseTerm()
        {
            var node = ParseUnary();
            while (true)
            {
                SkipWhitespace();
                if (AtEnd())
                    return node;

                if (Peek() is '*' or '/')
                {
                    var op = Next();
                    node = new BinaryNode(op, node, ParseUnary());
                    continue;
                }

                return node;
            }
        }

        private Node ParseUnary()
        {
            SkipWhitespace();
            if (AtEnd())
                throw new QcFormulaException("The formula ended unexpectedly.");

            if (Peek() == '-')
            {
                Next();
                return new UnaryNode(ParseUnary());
            }

            return ParsePrimary();
        }

        private Node ParsePrimary()
        {
            SkipWhitespace();

            if (AtEnd())
                throw new QcFormulaException("The formula ended unexpectedly.");

            var c = Peek();

            if (c == '(')
            {
                Next();
                var inner = ParseExpression();
                SkipWhitespace();
                Expect(')');
                return inner;
            }

            if (c == '{')
                return ParseScalarReference();

            if (char.IsLetter(c))
                return ParseFunction();

            if (char.IsDigit(c) || c == '.')
                return ParseNumber();

            throw new QcFormulaException($"Unexpected character '{c}' at position {_position}.");
        }

        private Node ParseScalarReference()
        {
            Expect('{');
            var key = ReadUntil('}');
            Expect('}');

            key = key.Trim();
            if (key.Length == 0)
                throw new QcFormulaException("A field reference '{}' is empty.");

            if (key.Contains('.'))
                throw new QcFormulaException(
                    $"'{{{key}}}' refers to a table column, which is only valid inside "
                    + "AVG(), SUM(), MIN(), MAX() or RSD().");

            _scalarKeys.Add(key);
            return new ScalarNode(key);
        }

        private Node ParseFunction()
        {
            var start = _position;
            while (!AtEnd() && char.IsLetter(Peek())) Next();
            var name = _text[start.._position].ToUpperInvariant();

            if (!Functions.Contains(name))
                throw new QcFormulaException(
                    $"Unknown function '{name}'. Supported functions are "
                    + $"{string.Join(", ", Functions)}.");

            SkipWhitespace();
            Expect('(');
            SkipWhitespace();
            Expect('{');
            var reference = ReadUntil('}');
            Expect('}');
            SkipWhitespace();
            Expect(')');

            var parts = reference.Trim().Split('.', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                throw new QcFormulaException(
                    $"{name}() must reference a table column as "
                    + "{{table_key.column_key}}.");

            _tableReferences.Add(new QcFormulaTableReference(name, parts[0], parts[1]));
            return new AggregateNode(name, parts[0], parts[1]);
        }

        private Node ParseNumber()
        {
            var start = _position;
            var seenDot = false;

            while (!AtEnd())
            {
                var c = Peek();
                if (char.IsDigit(c)) { Next(); continue; }
                if (c == '.' && !seenDot) { seenDot = true; Next(); continue; }
                break;
            }

            var text = _text[start.._position];
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new QcFormulaException($"'{text}' is not a valid number.");

            return new ConstantNode(value);
        }

        internal void ExpectEnd()
        {
            SkipWhitespace();
            if (!AtEnd())
                throw new QcFormulaException(
                    $"Unexpected trailing text '{_text[_position..]}'.");
        }

        private string ReadUntil(char terminator)
        {
            var builder = new StringBuilder();
            while (!AtEnd() && Peek() != terminator)
                builder.Append(Next());

            if (AtEnd())
                throw new QcFormulaException($"Expected '{terminator}' but the formula ended.");

            return builder.ToString();
        }

        private void Expect(char expected)
        {
            if (AtEnd() || Peek() != expected)
                throw new QcFormulaException(
                    $"Expected '{expected}' at position {_position}.");
            Next();
        }

        private void SkipWhitespace()
        {
            while (!AtEnd() && char.IsWhiteSpace(Peek())) Next();
        }

        private bool AtEnd() => _position >= _text.Length;

        private char Peek() => _text[_position];

        private char Next() => _text[_position++];
    }
}
