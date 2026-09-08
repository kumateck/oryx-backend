using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace APP.Services.Formulas;

public static class FormulaCanonicalJson
{
    private static readonly JsonSerializerOptions StringOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Canonicalize(string json, int maxUtf8Bytes)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Canonical JSON input is required.");
        if (Encoding.UTF8.GetByteCount(json) > maxUtf8Bytes)
            throw new ArgumentException("Canonical JSON input exceeds its byte limit.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 64
        });
        var output = new StringBuilder(json.Length);
        Write(document.RootElement, output, "$", 0);
        return output.ToString();
    }

    public static string HashJson(string domain, string json, int maxUtf8Bytes) =>
        HashCanonical(domain, Canonicalize(json, maxUtf8Bytes));

    public static string HashCanonical(string domain, string canonicalJson)
    {
        if (string.IsNullOrWhiteSpace(domain) || domain.Contains('\0'))
            throw new ArgumentException("A non-empty hash domain without NUL is required.");
        var bytes = Encoding.UTF8.GetBytes($"{domain}\0{canonicalJson}");
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static void Write(JsonElement element, StringBuilder output,
        string path, int depth)
    {
        if (depth > 64)
            throw new ArgumentException($"Canonical JSON depth exceeded at {path}.");
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                output.Append("null");
                break;
            case JsonValueKind.True:
                output.Append("true");
                break;
            case JsonValueKind.False:
                output.Append("false");
                break;
            case JsonValueKind.String:
                output.Append(JsonSerializer.Serialize(element.GetString(), StringOptions));
                break;
            case JsonValueKind.Number:
                if (!element.TryGetInt64(out var integer))
                    throw new ArgumentException(
                        $"Only signed 64-bit integer metadata is canonical at {path}.");
                output.Append(integer.ToString(CultureInfo.InvariantCulture));
                break;
            case JsonValueKind.Array:
                WriteArray(element, output, path, depth);
                break;
            case JsonValueKind.Object:
                WriteObject(element, output, path, depth);
                break;
            default:
                throw new ArgumentException($"Unsupported canonical JSON value at {path}.");
        }
    }

    private static void WriteArray(JsonElement element, StringBuilder output,
        string path, int depth)
    {
        output.Append('[');
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            if (index > 0)
                output.Append(',');
            Write(item, output, $"{path}[{index}]", depth + 1);
            index++;
        }
        output.Append(']');
    }

    private static void WriteObject(JsonElement element, StringBuilder output,
        string path, int depth)
    {
        var properties = element.EnumerateObject().ToList();
        var duplicate = properties.GroupBy(item => item.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException(
                $"Duplicate canonical JSON property at {path}.{duplicate.Key}.");
        output.Append('{');
        var index = 0;
        foreach (var property in properties.OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            if (index > 0)
                output.Append(',');
            output.Append(JsonSerializer.Serialize(property.Name, StringOptions));
            output.Append(':');
            Write(property.Value, output, $"{path}.{property.Name}", depth + 1);
            index++;
        }
        output.Append('}');
    }
}
