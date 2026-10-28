using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Forms;

#nullable enable

namespace APP.Services.Forms;

/// <summary>Input completeness only; governed formula execution remains authoritative.</summary>
internal static class FormAnswerCompleteness
{
    public static bool IsComplete(string? value, QuestionType type, string? definition)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (type is not (QuestionType.Formula or QuestionType.Table
            or QuestionType.Checkbox or QuestionType.Instrument)) return true;
        try
        {
            using var document = JsonDocument.Parse(value);
            var answer = document.RootElement;
            if (type is QuestionType.Checkbox or QuestionType.Instrument)
                return answer.ValueKind == JsonValueKind.Array && answer.GetArrayLength() > 0
                    && answer.EnumerateArray().All(item => Text(Get(item, "value")) || Text(item));
            using var configDocument = JsonDocument.Parse(definition ?? "null");
            var config = Unwrap(configDocument.RootElement);
            if (type == QuestionType.Table)
                return config.ValueKind == JsonValueKind.Object && answer.ValueKind == JsonValueKind.Object && Properties(answer).Any()
                    && Properties(Get(config, "cellConfigs")).All(cell =>
                        Get(cell.Value, "isMerged").ValueKind == JsonValueKind.True
                        || Get(cell.Value, "type").ToString() is not ("input" or "select")
                        || Text(Get(answer, cell.Name)));
            if (answer.ValueKind != JsonValueKind.Object || !Properties(answer).Any()) return false;
            var mode = Get(config, "mode").ToString();
            if (mode is "capsule" or "tablet" or "tube")
            {
                var inputs = Get(answer, "inputs");
                var count = Count(Get(config, "sampleCount"));
                return count > 0 && (mode == "capsule"
                    ? Numeric(Get(inputs, "bulkCapsules")) && Numeric(Get(inputs, "bulkShells"))
                        && Numbers(Get(inputs, "capsWts"), count) && Numbers(Get(inputs, "shellWts"), count)
                    : Numeric(Get(inputs, "bulkTotal")) && Numbers(Get(inputs, "indWts"), count));
            }
            if (!Text(Get(config, "expression"))) return false;
            var formulaType = Get(config, "type").ToString();
            if (formulaType != "3")
            {
                var constants = Get(config, "constants");
                return Regex.Matches(Get(config, "expression").GetString()!, @":([A-Za-z0-9_]+)")
                    .All(match => Numeric(Get(constants, match.Groups[1].Value))
                        || Numeric(Get(answer, match.Groups[1].Value)))
                    && Properties(answer).All(item => Numeric(item.Value));
            }
            var standard = Get(answer, "standardInputs");
            if (!Inputs(Get(config, "standardInputs"), standard)) return false;
            var samples = Items(Get(answer, "sampleInputs")).ToList();
            var configuredSamples = Items(Get(config, "sampleInputs")).ToList();
            if (samples.Count < configuredSamples.Count || samples.Where((sample, index) =>
                !Inputs(index < configuredSamples.Count ? configuredSamples[index] : default, sample)).Any()) return false;
            var manual = Get(answer, "manualVars");
            var expressions = Get(config, "expression").ToString() + " "
                + string.Join(" ", Items(Get(config, "namedExpressions")).Select(item => Get(item, "expression").ToString()));
            var referenced = Regex.Matches(expressions, @":([A-Za-z0-9_.]+)")
                .Select(match => match.Groups[1].Value).ToHashSet();
            var manualKeys = Properties(Get(config, "manualVars")).Select(item => item.Name)
                .Concat(Properties(Get(config, "manualVarLabels")).Select(item => item.Name))
                .Concat(Properties(manual).Select(item => item.Name)).Where(referenced.Contains).Distinct().ToList();
            if (manualKeys.Any(key =>
            {
                var entry = Get(manual, key);
                return !Numeric(entry) && !Numeric(Get(entry, "value"))
                    && !(Text(Get(entry, "raw")) && Numeric(Get(entry, "evaluated")));
            })) return false;
            var rows = Count(First(config, "numberOfReadings", "noOfReadings"), Items(Get(config, "rowHeaders")).Count());
            var columns = Items(Get(config, "headers")).Count();
            if (columns == 0) columns = Count(First(config, "numberOfSamples", "noOfSamples"))
                + (Get(config, "standardInTable").ValueKind == JsonValueKind.True ? 1 : 0);
            var table = Items(Get(answer, "tableData")).ToList();
            if (table.Count < rows || table.Any(row => !Numbers(row, columns))) return false;
            return Properties(standard).Any() || samples.Any(sample => Properties(sample).Any())
                || manualKeys.Count > 0 || table.Any(row => Items(row).Any());
        }
        catch (JsonException)
        {
            // Older clients persisted these choices as comma-separated option text.
            return type is QuestionType.Checkbox or QuestionType.Instrument
                && value.TrimStart()[0] is not ('[' or '{' or '"')
                && value.Split(',').All(item => !string.IsNullOrWhiteSpace(item));
        }
    }

    private static JsonElement Unwrap(JsonElement value)
    {
        for (var depth = 0; depth < 4 && value.ValueKind == JsonValueKind.String; depth++)
        {
            using var nested = JsonDocument.Parse(value.GetString()!);
            value = nested.RootElement.Clone();
        }
        foreach (var key in new[] { "schema", "data", "value", "settings", "formula" })
        {
            if (Text(Get(value, "expression")) || Text(Get(value, "mode"))) break;
            var nested = Get(value, key);
            if (nested.ValueKind == JsonValueKind.Object)
            {
                var rootType = Get(value, "type");
                // Keep the discriminator on the wrapper when the payload omits it.
                if (Get(nested, "type").ValueKind == JsonValueKind.Undefined && rootType.ValueKind != JsonValueKind.Undefined)
                {
                    var properties = Properties(nested).ToDictionary(item => item.Name, item => item.Value);
                    properties["type"] = rootType;
                    return JsonSerializer.SerializeToElement(properties);
                }
                return nested;
            }
        }
        return value;
    }

    private static JsonElement Get(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item) ? item : default;
    private static JsonElement First(JsonElement value, string first, string second) =>
        Get(value, first).ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? Get(value, second) : Get(value, first);
    private static IEnumerable<JsonProperty> Properties(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object ? value.EnumerateObject() : [];
    private static IEnumerable<JsonElement> Items(JsonElement value) =>
        value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];
    private static bool Text(JsonElement value) => value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());
    private static bool Numeric(JsonElement value) => value.ValueKind is JsonValueKind.Number or JsonValueKind.String
        && double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number);
    private static int Count(JsonElement value, int fallback = 0) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var count) ? count : fallback;
    private static bool Numbers(JsonElement value, int count) => value.ValueKind == JsonValueKind.Array
        && value.GetArrayLength() >= count && value.EnumerateArray().All(Numeric);
    private static bool Inputs(JsonElement configured, JsonElement entered) => Items(configured).All(input =>
        Text(Get(input, "key")) && Numeric(Get(entered, Get(input, "key").GetString()!)))
        && Properties(entered).All(item => Numeric(item.Value));
}
