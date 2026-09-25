using System.Text.Json;
using System.Text.Json.Serialization;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>One proposed column of a Table field, before it is written as ColumnDefinitions JSON.</summary>
public sealed class GridColumn
{
    public string Key { get; set; }
    public string Label { get; set; }
    public string Group { get; set; }
    public string Unit { get; set; }
    public WorksheetFieldType Type { get; set; }
    public WorksheetFieldMode Mode { get; set; } = WorksheetFieldMode.Entry;

    /// <summary>Template-owned per-row values; any column carrying them is read-only.</summary>
    public List<string> FixedValues { get; set; }

    public bool RowHeader { get; set; }
    public List<string> Options { get; set; }

    /// <summary>A per-row formula over sibling column keys (see <see cref="PlateAverage"/>).</summary>
    public string Formula { get; set; }

    public int SourceColumn { get; set; }
    public ImportConfidence Confidence { get; set; } = ImportConfidence.High;
    public string Reason { get; set; }
    public string FlagCode { get; set; }
}

/// <summary>
/// Writes the ColumnDefinitions JSON: the existing <c>{ key, label, type, unit }</c> entries
/// plus <c>fixedValues</c> / <c>rowHeader</c> (already honoured by <c>WorksheetRowHeaders</c>)
/// and the Phase A keys <c>options</c> / <c>group</c>, and <c>mode</c> / <c>formula</c> for a
/// calculated column.
/// </summary>
public static class ColumnDefinitionsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record Entry(
        string Key,
        string Label,
        string Type,
        string Unit,
        string Mode,
        string Formula,
        List<string> FixedValues,
        bool? RowHeader,
        string Group,
        List<string> Options);

    public static string Write(IEnumerable<GridColumn> columns) =>
        JsonSerializer.Serialize(columns.Select(column => new Entry(
            column.Key,
            column.Label,
            column.Type.ToString(),
            string.IsNullOrWhiteSpace(column.Unit) ? null : column.Unit,
            column.Mode == WorksheetFieldMode.Calculated ? nameof(WorksheetFieldMode.Calculated) : null,
            column.Formula,
            column.FixedValues,
            column.RowHeader ? true : null,
            column.Group,
            column.Options is { Count: > 0 } ? column.Options : null)).ToList(), Options);
}
