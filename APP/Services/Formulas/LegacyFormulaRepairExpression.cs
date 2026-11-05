using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace APP.Services.Formulas;

/// <summary>Matches the v1 arithmetic AST for hashing; the calculation service verifies it independently.</summary>
internal sealed class LegacyFormulaRepairExpression(string expression)
{
    private readonly string text = Regex.Replace(expression, @"\s", "");
    private int position;
    private int nodes;
    public JsonObject Parse()
    {
        var value = Sum(0);
        if (position != text.Length) throw new ArgumentException("Unsupported expression token.");
        return value;
    }
    private void Limit(int depth)
    {
        if (depth > 64 || ++nodes > 1000) throw new ArgumentException("Expression exceeds the arithmetic repair limits.");
    }
    private JsonObject Sum(int depth)
    {
        var left = Product(depth + 1);
        while (position < text.Length && text[position] is '+' or '-')
        { var op = text[position++] == '+' ? 0 : 1; left = Binary(left, Product(depth + 1), op); }
        return left;
    }
    private JsonObject Product(int depth)
    {
        var left = Atom(depth + 1);
        while (position < text.Length && text[position] is '*' or '/')
        { var op = text[position++] == '*' ? 2 : 3; left = Binary(left, Atom(depth + 1), op); }
        return left;
    }
    private JsonObject Atom(int depth)
    {
        Limit(depth);
        if (position >= text.Length) throw new ArgumentException("Expression is incomplete.");
        var token = text[position];
        if (token is '+' or '-')
        { position++; return new() { ["kind"] = 2, ["operator"] = token == '+' ? 0 : 1, ["operand"] = Atom(depth + 1) }; }
        if (token == '(')
        {
            position++; var nested = Sum(depth + 1);
            if (position >= text.Length || text[position++] != ')') throw new ArgumentException("Unbalanced parentheses.");
            return nested;
        }
        if (token == ':')
        {
            position++; var start = position;
            while (position < text.Length && (char.IsAsciiLetterOrDigit(text[position]) || text[position] is '_' or '.')) position++;
            var key = text[start..position];
            if (!Regex.IsMatch(key, @"^[A-Za-z][A-Za-z0-9_.]*$")) throw new ArgumentException("Invalid variable key.");
            return new() { ["kind"] = 1, ["key"] = key };
        }
        var literal = Regex.Match(text[position..], @"^(?:\d+(?:\.\d*)?|\.\d+)");
        if (!literal.Success) throw new ArgumentException("Expected a number or colon-prefixed variable.");
        position += literal.Length;
        var parts = literal.Value.Split('.');
        var whole = parts[0].TrimStart('0'); if (whole.Length == 0) whole = "0";
        var fraction = parts.Length > 1 ? parts[1].TrimEnd('0') : "";
        return new() { ["kind"] = 0, ["value"] = fraction.Length > 0 ? whole + "." + fraction : whole };
    }
    private JsonObject Binary(JsonObject left, JsonObject right, int op)
    {
        Limit(0);
        return new() { ["kind"] = 3, ["operator"] = op, ["left"] = left, ["right"] = right };
    }
}
