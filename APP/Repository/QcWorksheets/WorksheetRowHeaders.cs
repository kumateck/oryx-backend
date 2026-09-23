using System.Text.Json;

namespace APP.Repository.QcWorksheets;

/// <summary>Template-owned table row headers are presentation, never analyst values.</summary>
internal static class WorksheetRowHeaders
{
    public static bool IsHeaderColumn(string definitions, string columnKey)
    {
        if (string.IsNullOrWhiteSpace(definitions) || string.IsNullOrWhiteSpace(columnKey))
            return false;

        try
        {
            using var document = JsonDocument.Parse(definitions);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var column in document.RootElement.EnumerateArray())
            {
                if (column.ValueKind != JsonValueKind.Object)
                    continue;
                if (!column.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String
                    || !string.Equals(key.GetString(), columnKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                return (column.TryGetProperty("rowHeader", out var marker)
                        && marker.ValueKind == JsonValueKind.True)
                    || (column.TryGetProperty("fixedValues", out var labels)
                        && labels.ValueKind == JsonValueKind.Array);
            }
        }
        catch (JsonException)
        {
            // Malformed definitions are handled by the existing template validation path.
        }

        return false;
    }
}
