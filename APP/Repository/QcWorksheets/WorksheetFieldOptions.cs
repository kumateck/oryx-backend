using System.Text.Encodings.Web;
using System.Text.Json;
using DOMAIN.Entities.QcWorksheets;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Choice lists for Select, MultiSelect and GrowthObservation fields (build brief 07, A1).
/// <para>
/// <c>OptionsJson</c> is a JSON array of strings, e.g. <c>["Complies","Does not comply"]</c>.
/// A MultiSelect <i>value</i> uses the same encoding: a JSON array of the chosen options,
/// stored in the single <c>WorksheetFieldValue.Value</c> column. A comma-separated list was
/// rejected because real options contain commas.
/// </para>
/// </summary>
internal static class WorksheetFieldOptions
{
    private static readonly JsonSerializerOptions Compact = new()
    {
        // Options are data read back by the API, never embedded in HTML, so non-ASCII
        // text ("λ", "µ") is stored as written rather than as \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static bool RequiresOptions(WorksheetFieldType type) =>
        type is WorksheetFieldType.Select
            or WorksheetFieldType.MultiSelect
            or WorksheetFieldType.GrowthObservation;

    /// <summary>
    /// Reads a JSON string array. Null or blank input is an empty list; anything that is not an
    /// array of strings is unreadable.
    /// </summary>
    public static bool TryParse(string json, out List<string> options)
    {
        options = [];
        if (string.IsNullOrWhiteSpace(json))
            return true;

        try
        {
            using var document = JsonDocument.Parse(json);
            return TryRead(document.RootElement, out options);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryRead(JsonElement element, out List<string> options)
    {
        options = [];
        if (element.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                return false;
            options.Add(item.GetString());
        }

        return true;
    }

    /// <summary>Trimmed, non-blank, de-duplicated case-insensitively, in authored order.</summary>
    public static List<string> Distinct(IEnumerable<string> options) =>
        options
            .Select(option => option?.Trim())
            .Where(option => !string.IsNullOrEmpty(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// The stored form: the distinct options re-serialized, or null when there are none (so an
    /// empty <c>[]</c> sent for a non-choice field reads as "no options"). Unreadable input is
    /// returned unchanged; template validation refuses it before anything is stored.
    /// </summary>
    public static string Normalize(string json)
    {
        if (!TryParse(json, out var options))
            return json;

        var distinct = Distinct(options);
        return distinct.Count == 0 ? null : JsonSerializer.Serialize(distinct, Compact);
    }

    /// <summary>Template-save rule: choice types need two or more options, other types none.</summary>
    public static Result Validate(CreateWorksheetFieldRequest field)
    {
        var readable = TryParse(field.OptionsJson, out var options);
        var count = readable ? Distinct(options).Count : 0;

        if (RequiresOptions(field.Type))
        {
            if (!readable || count < 2)
                return QcWorksheetErrors.OptionsRequired(field.FieldKey);

            return Result.Success();
        }

        if (!readable || count > 0)
            return QcWorksheetErrors.OptionsNotAllowed(field.FieldKey, field.Type.ToString());

        return Result.Success();
    }

    /// <summary>Exact (case-sensitive) match after trimming, so stored results stay canonical.</summary>
    public static bool IsOption(IEnumerable<string> options, string value) =>
        options.Any(option => string.Equals(option?.Trim(), value?.Trim(), StringComparison.Ordinal));

    /// <summary>
    /// The options chosen in a MultiSelect value: a JSON string array. A bare string that is not
    /// a JSON array is read as a single selection. An array holding anything other than strings
    /// yields its raw elements, which then fail the membership check.
    /// </summary>
    public static List<string> Selections(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (!trimmed.StartsWith('['))
            return [trimmed];

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return [trimmed];

            return document.RootElement.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
                .ToList();
        }
        catch (JsonException)
        {
            return [trimmed];
        }
    }
}
