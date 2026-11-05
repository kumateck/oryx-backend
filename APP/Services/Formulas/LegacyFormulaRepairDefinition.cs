using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

#nullable enable
namespace APP.Services.Formulas;

/// <summary>Bounded legacy arithmetic adapter. It validates expressions, not scientific release evidence.</summary>
public static class LegacyFormulaRepairDefinition
{
    public const string Policy = "oryx-decimal-v1-half-up";
    private static readonly Regex Token = new(@":([A-Za-z][A-Za-z0-9_.]*)|(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?|[()+*/^\-]", RegexOptions.CultureInvariant);
    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    public static JsonObject Payload(string raw)
    {
        if (Encoding.UTF8.GetByteCount(raw) > 262144) throw new ArgumentException("Formula configuration is too large.");
        JsonNode? value = JsonNode.Parse(raw, documentOptions: new JsonDocumentOptions { MaxDepth = 32 });
        for (var depth = 0; depth < 6; depth++)
        {
            if (value is JsonValue scalar && scalar.TryGetValue<string>(out var text)) { value = JsonNode.Parse(text); continue; }
            if (value is not JsonObject obj) break;
            if (obj["expression"] is JsonValue expression && expression.TryGetValue<string>(out var nested) && nested.TrimStart().StartsWith('{'))
            { value = JsonNode.Parse(nested); continue; }
            if (obj.ContainsKey("expression") || obj.ContainsKey("mode")) return obj;
            value = new[] { "settings", "formula", "schema", "data", "value" }.Select(key => obj[key]).FirstOrDefault(node => node is not null);
        }
        throw new ArgumentException("A supported formula configuration is required.");
    }

    public static string? Validate(string before, string after)
    {
        try
        {
            var old = Payload(before); var next = Payload(after);
            if (Number(next, "type", 0) is not (0 or 3)) return "This repair supports Simple and Universal arithmetic formulas. Other formula types need their own repair adapter.";
            var stableOld = (JsonObject)old.DeepClone(); var stableNext = (JsonObject)next.DeepClone();
            foreach (var key in new[] { "expression", "manualVarLabels" }) { stableOld.Remove(key); stableNext.Remove(key); }
            if (!JsonNode.DeepEquals(stableOld, stableNext)) return "Only the expression and manual-input labels can change in this repair. Table shape, units, inputs, constants and precision must stay unchanged.";
            if (next["namedExpressions"] is JsonArray named && named.Count > 0 || next["formulaInTable"]?.GetValue<bool>() == true || next["averageAllResults"]?.GetValue<bool>() == true)
                return "Named expressions, derived table columns and averaged result sets require their own repair adapter.";
            var expression = Text(next, "expression");
            if (expression.Length is < 1 or > 4000) return "Enter an expression of 1–4000 characters.";
            var compact = Regex.Replace(expression, @"\s", "");
            if (string.Concat(Token.Matches(compact).Select(m => m.Value)) != compact) return "Use numeric arithmetic and colon-prefixed variables; bare names and functions are unsupported.";
            _ = new LegacyFormulaRepairExpression(expression).Parse();
            if (next["manualVarLabels"] is JsonObject labels && (labels.Count > 100 || labels.Any(x => !Regex.IsMatch(x.Key, @"^[A-Za-z][A-Za-z0-9_]{0,99}$") || x.Value is not JsonValue v || !v.TryGetValue<string>(out var label) || label.Length is < 1 or > 200)))
                return "Manual inputs need plain variable keys and labels of 1–200 characters.";
            if (Number(next, "noOfSamples", Number(next, "numberOfSamples", 2)) is < 1 or > 100) return "Unsupported sample count.";
            foreach (var key in Variables(expression))
                if (!Known(next, key)) return $"Variable :{key} has no input, constant or table binding. Add its manual-input label or correct the reference.";
            return null;
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or FormatException)
        { return "Invalid formula configuration: " + error.Message; }
    }

    public static IEnumerable<string> Variables(string expression) => Token.Matches(expression)
        .Where(m => m.Groups[1].Success).Select(m => m.Groups[1].Value).Distinct();
    public static int Number(JsonObject value, string key, int fallback) => value[key] is JsonValue node && node.TryGetValue<int>(out var number) ? number : fallback;
    public static string Text(JsonObject value, string key) => value[key] is JsonValue node && node.TryGetValue<string>(out var text) ? text : "";
    private static bool Known(JsonObject config, string key)
    {
        if (Number(config, "type", 0) == 0) return true;
        if (config["constants"] is JsonObject constants && constants.ContainsKey(key) || config["manualVarLabels"] is JsonObject labels && labels.ContainsKey(key)) return true;
        if (Regex.IsMatch(key, @"^(standard|sample\d*)(Avg|SD|RSD)$") || Regex.IsMatch(key, @"^R\d+(standard|sample\d*)$")) return true;
        var match = Regex.Match(key, @"^(standardInput|sampleInput\d*)\.([A-Za-z][A-Za-z0-9_]*)$");
        if (!match.Success) return false;
        var inputs = match.Groups[1].Value == "standardInput" ? config["standardInputs"] as JsonArray
            : (config["sampleInputs"] as JsonArray)?.FirstOrDefault() as JsonArray;
        return inputs?.OfType<JsonObject>().Any(input => Text(input, "key") == match.Groups[2].Value) == true;
    }

    public static (JsonElement Definition, string Hash) Compile(string raw)
    {
        var config = Payload(raw); var universal = Number(config, "type", 0) == 3;
        var count = universal ? Number(config, "numberOfSamples", Number(config, "noOfSamples", 2)) : 1;
        var expressions = Enumerable.Range(1, count).Select(sample => universal
            ? Regex.Replace(Text(config, "expression"), @":sample(Avg|SD|RSD)\b|:sampleInput\.|:R(\d+)sample\b", m => m.Value.StartsWith(":sampleInput", StringComparison.Ordinal)
                ? $":sampleInput{sample}." : m.Groups[2].Success ? $":R{m.Groups[2].Value}sample{sample}" : $":sample{sample}{m.Groups[1].Value}")
            : Text(config, "expression")).ToArray();
        var definition = JsonSerializer.SerializeToElement(new
        {
            formulaLanguageVersion = "oryx-formula-v1", numericPolicyVersion = Policy,
            variables = expressions.SelectMany(Variables).Distinct().Order(StringComparer.Ordinal).Select(key => new
            { key, label = key, dataType = 0, valueShape = 0, scope = 0, unit = (string?)null, required = true }).ToArray(),
            results = expressions.Select((expression, i) => new
            {
                key = $"sample{i + 1}", label = $"Sample {i + 1}", expression, unit = (string?)null,
                numericScale = config["percentageResults"]?.ToString() == "true" || config["percent"]?.ToString() == "true" ? 2 : 0,
                rounding = new { kind = 0, digits = Number(config, "finalResultDecimalPlaces", Number(config, "decimalPlaces", 2)) }
            }).ToArray()
        });
        var executable = JsonNode.Parse(definition.GetRawText())!.AsObject();
        foreach (var variable in executable["variables"]!.AsArray().OfType<JsonObject>()) variable.Remove("label");
        var results = executable["results"]!.AsArray().OfType<JsonObject>().OrderBy(r => Text(r, "key"), StringComparer.Ordinal).ToList();
        foreach (var result in results) { result.Remove("label"); result["expression"] = new LegacyFormulaRepairExpression(Text(result, "expression")).Parse(); }
        executable["results"] = new JsonArray(results.Select(r => r.DeepClone()).ToArray());
        return (definition, FormulaCanonicalJson.HashJson("oryx:formula-definition:v1", executable.ToJsonString(), 262144));
    }

    public static JsonElement ValidationRequest(string raw)
    {
        var compiled = Compile(raw);
        return JsonSerializer.SerializeToElement(new { schemaVersion = "oryx-formula-service-validate-request-v1",
            requestId = Guid.NewGuid().ToString("N"), formulaLanguageVersion = "oryx-formula-v1", numericPolicyVersion = Policy,
            definitionHash = compiled.Hash, definition = compiled.Definition, testCases = Array.Empty<object>() });
    }

    public static string InputHash(string raw)
    {
        var value = JsonNode.Parse(raw) as JsonObject ?? throw new ArgumentException("Saved readings must be an object.");
        foreach (var key in new[] { "settings", "results", "result", "averageResult", "stats" }) value.Remove(key);
        return Hash(value.ToJsonString());
    }

    public static Dictionary<string, string?> ResolveInputs(string definition, string value)
    {
        var config = Payload(definition); var answer = JsonNode.Parse(value) as JsonObject ?? new();
        var compiled = Compile(definition); var result = new Dictionary<string, string?>();
        foreach (var variable in compiled.Definition.GetProperty("variables").EnumerateArray())
        {
            var key = variable.GetProperty("key").GetString() ?? "";
            JsonNode? input = null;
            if (Number(config, "type", 0) == 0) input = answer[key];
            if (config["constants"] is JsonObject constants && constants[key] is { } constant) input = constant;
            if (answer["manualVars"] is JsonObject manual && manual[key] is { } entry)
                input = entry is JsonObject obj ? obj["raw"] ?? obj["value"] : entry;
            var match = Regex.Match(key, @"^(standardInput|sampleInput(\d+))\.(.+)$");
            if (match.Success)
            {
                var group = match.Groups[1].Value == "standardInput" ? answer["standardInputs"] as JsonObject
                    : (answer["sampleInputs"] as JsonArray)?.ElementAtOrDefault(int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) - 1) as JsonObject;
                input = group?[match.Groups[3].Value];
            }
            var table = Regex.Match(key, @"^(standard|sample(\d+))(Avg|SD|RSD)$");
            if (table.Success && answer["tableData"] is JsonArray rows)
            {
                var column = table.Groups[1].Value == "standard" ? 0 : int.Parse(table.Groups[2].Value, CultureInfo.InvariantCulture) - (config["standardInTable"]?.ToString() == "false" ? 1 : 0);
                var values = rows.OfType<JsonArray>().Select(row => row.ElementAtOrDefault(column)?.ToString() ?? "");
                result[key] = FormulaDecimalStatistics.Calculate(values, table.Groups[3].Value == "Avg" ? 0 : table.Groups[3].Value == "SD" ? 1 : 3,
                    Number(config, "inputDecimalPlaces", 12), Number(config, "tableCalculationDecimalPlaces", Number(config, "decimalPlaces", 2)));
                continue;
            }
            var cell = Regex.Match(key, @"^R(\d+)(standard|sample(\d+))$");
            if (cell.Success && answer["tableData"] is JsonArray tableRows)
            {
                var row = int.Parse(cell.Groups[1].Value, CultureInfo.InvariantCulture) - 1;
                var col = cell.Groups[2].Value == "standard" ? 0 : int.Parse(cell.Groups[3].Value, CultureInfo.InvariantCulture) - (config["standardInTable"]?.ToString() == "false" ? 1 : 0);
                input = (tableRows.ElementAtOrDefault(row) as JsonArray)?.ElementAtOrDefault(col);
            }
            result[key] = input?.ToString();
        }
        return result;
    }
}
