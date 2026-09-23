using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace APP.Services.FullProcedures;

internal static class ProcedureCanonicalJson
{
    private static readonly JsonSerializerOptions StringOptions = new()
        { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    internal static string Object(string json, int maxBytes = 262_144)
    {
        if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > maxBytes)
            throw new ArgumentException("A bounded JSON object is required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 64,
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("The Procedure parameter schema must be a JSON object.");
        var output = new StringBuilder(json.Length);
        Write(document.RootElement, output, "$", 0);
        return output.ToString();
    }

    private static void Write(JsonElement value, StringBuilder output, string path, int depth)
    {
        if (depth > 64) throw new ArgumentException($"JSON depth exceeded at {path}.");
        switch (value.ValueKind)
        {
            case JsonValueKind.Object: WriteObject(value, output, path, depth); break;
            case JsonValueKind.Array: WriteArray(value, output, path, depth); break;
            case JsonValueKind.String:
                output.Append(JsonSerializer.Serialize(value.GetString(), StringOptions)); break;
            case JsonValueKind.Number: output.Append(NormalizeNumber(value.GetRawText())); break;
            case JsonValueKind.True: output.Append("true"); break;
            case JsonValueKind.False: output.Append("false"); break;
            case JsonValueKind.Null: output.Append("null"); break;
            default: throw new ArgumentException($"Unsupported JSON value at {path}.");
        }
    }

    private static void WriteObject(JsonElement value, StringBuilder output, string path, int depth)
    {
        var properties = value.EnumerateObject().ToArray();
        var duplicate = properties.GroupBy(x => x.Name, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Duplicate JSON property at {path}.{duplicate.Key}.");
        output.Append('{');
        var first = true;
        foreach (var property in properties.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (!first) output.Append(',');
            first = false;
            output.Append(JsonSerializer.Serialize(property.Name, StringOptions)).Append(':');
            Write(property.Value, output, $"{path}.{property.Name}", depth + 1);
        }
        output.Append('}');
    }

    private static void WriteArray(JsonElement value, StringBuilder output, string path, int depth)
    {
        output.Append('[');
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            if (index > 0) output.Append(',');
            Write(item, output, $"{path}[{index}]", depth + 1);
            index++;
        }
        output.Append(']');
    }

    private static string NormalizeNumber(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var number))
            throw new ArgumentException("Procedure JSON numbers must fit decimal precision.");
        return number.ToString("G29", CultureInfo.InvariantCulture);
    }
}
