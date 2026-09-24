using System.Text.Json;
using DOMAIN.Entities.QcWorksheets;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// One entry of a Table field's <c>ColumnDefinitions</c>, as far as validation needs it. The
/// JSON itself is stored verbatim, so display-only keys such as <c>group</c> (and anything
/// else a client adds) round-trip untouched.
/// </summary>
internal sealed record WorksheetTableColumn(
    string Key,
    WorksheetFieldType? Type,
    bool RowHeader,
    bool HasFixedValues,
    bool FixedValuesIsArray,
    int FixedValueCount,
    bool HasOptions,
    bool OptionsReadable,
    List<string> Options)
{
    /// <summary>Template-owned: rendered from the template, never entered.</summary>
    public bool IsFixed => RowHeader || HasFixedValues;
}

/// <summary>
/// The Table column contract (build brief 07, A1 and A2): per-column <c>options</c>, and fixed
/// columns that all declare the same number of rows.
/// </summary>
internal static class WorksheetTableColumns
{
    /// <summary>The columns, or null when the definitions are absent or not a JSON array.</summary>
    public static List<WorksheetTableColumn> Read(string definitions)
    {
        if (string.IsNullOrWhiteSpace(definitions))
            return null;

        try
        {
            using var document = JsonDocument.Parse(definitions);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            return document.RootElement.EnumerateArray()
                .Where(column => column.ValueKind == JsonValueKind.Object)
                .Select(ReadColumn)
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static WorksheetTableColumn Find(IEnumerable<WorksheetTableColumn> columns, string columnKey) =>
        columns?.FirstOrDefault(column =>
            string.Equals(column.Key, columnKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Template-save rules for a Table field. Unreadable definitions are left alone, matching
    /// the existing tolerance in formula validation.
    /// </summary>
    public static Result Validate(CreateWorksheetFieldRequest field)
    {
        if (field.Type != WorksheetFieldType.Table)
            return Result.Success();

        var columns = Read(field.ColumnDefinitions);
        if (columns is null)
            return Result.Success();

        foreach (var column in columns)
        {
            var options = ValidateOptions(field.FieldKey, column);
            if (!options.IsSuccess)
                return options;
        }

        return ValidateFixedRows(field.FieldKey, columns);
    }

    private static Result ValidateOptions(string fieldKey, WorksheetTableColumn column)
    {
        var name = $"{fieldKey}.{column.Key}";
        var count = column.OptionsReadable ? WorksheetFieldOptions.Distinct(column.Options).Count : 0;
        var choiceType = column.Type is { } type && WorksheetFieldOptions.RequiresOptions(type);

        if (column.HasOptions && column.IsFixed)
            return QcWorksheetErrors.OptionsNotAllowed(name, "a fixed column");

        if (column.HasOptions && column.Type is { } known && !choiceType)
            return QcWorksheetErrors.OptionsNotAllowed(name, known.ToString());

        if ((choiceType && !column.IsFixed) || column.HasOptions)
        {
            if (!column.OptionsReadable || count < 2)
                return QcWorksheetErrors.OptionsRequired(name);
        }

        return Result.Success();
    }

    private static Result ValidateFixedRows(string fieldKey, List<WorksheetTableColumn> columns)
    {
        foreach (var column in columns)
        {
            if (column.HasFixedValues && !column.FixedValuesIsArray)
                return QcWorksheetErrors.FixedRowCountMismatch(
                    fieldKey, $"column '{column.Key}' declares fixedValues that is not a list.");

            if (column.HasFixedValues && column.FixedValueCount == 0)
                return QcWorksheetErrors.FixedRowCountMismatch(
                    fieldKey,
                    $"column '{column.Key}' declares fixedValues but lists none, which would leave "
                    + "its rows open-ended.");

            if (column.RowHeader && !column.HasFixedValues)
                return QcWorksheetErrors.FixedRowCountMismatch(
                    fieldKey,
                    $"column '{column.Key}' is a row header but has no fixedValues, so the table "
                    + "would mix a fixed column with open-ended rows.");
        }

        var fixedColumns = columns.Where(column => column.HasFixedValues).ToList();
        if (fixedColumns.Select(column => column.FixedValueCount).Distinct().Count() <= 1)
            return Result.Success();

        var lengths = string.Join(", ", fixedColumns.Select(column => $"{column.Key}: {column.FixedValueCount}"));
        return QcWorksheetErrors.FixedRowCountMismatch(
            fieldKey,
            $"its fixed columns list different numbers of rows ({lengths}). Every fixedValues list "
            + "in one table must have the same length, which is the table's row count.");
    }

    private static WorksheetTableColumn ReadColumn(JsonElement column)
    {
        var key = column.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.String
            ? keyElement.GetString()
            : null;

        var rowHeader = column.TryGetProperty("rowHeader", out var marker) && marker.ValueKind == JsonValueKind.True;

        var hasFixed = column.TryGetProperty("fixedValues", out var fixedValues)
            && fixedValues.ValueKind != JsonValueKind.Null;
        var fixedIsArray = hasFixed && fixedValues.ValueKind == JsonValueKind.Array;

        var hasOptions = column.TryGetProperty("options", out var optionsElement)
            && optionsElement.ValueKind != JsonValueKind.Null;
        var options = new List<string>();
        var optionsReadable = hasOptions && WorksheetFieldOptions.TryRead(optionsElement, out options);

        return new WorksheetTableColumn(
            key,
            ReadType(column),
            rowHeader,
            hasFixed,
            fixedIsArray,
            fixedIsArray ? fixedValues.GetArrayLength() : 0,
            hasOptions,
            optionsReadable,
            options ?? []);
    }

    /// <summary>A column's <c>type</c> may be the enum name or its number; anything else is unknown.</summary>
    private static WorksheetFieldType? ReadType(JsonElement column)
    {
        if (!column.TryGetProperty("type", out var type))
            return null;

        if (type.ValueKind == JsonValueKind.String
            && Enum.TryParse<WorksheetFieldType>(type.GetString(), ignoreCase: true, out var named)
            && Enum.IsDefined(named))
            return named;

        if (type.ValueKind == JsonValueKind.Number
            && type.TryGetInt32(out var number)
            && Enum.IsDefined(typeof(WorksheetFieldType), number))
            return (WorksheetFieldType)number;

        return null;
    }
}
