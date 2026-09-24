using DOMAIN.Entities.QcWorksheets;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Runtime membership check for choice values (build brief 07, A1), run by both
/// <c>SaveValues</c> and <c>Submit</c> before anything is written.
/// <para>
/// A field or column with no options is not checked. That keeps templates authored before
/// options existed executable: their pinned versions can never gain options, and refusing
/// every value on them would strand in-flight worksheets.
/// </para>
/// </summary>
internal static class WorksheetOptionValues
{
    public static Result Check(WorksheetField field, IEnumerable<WorksheetFieldValue> values)
    {
        // Only analyst entries are checked. Constant and Calculated fields have no analyst
        // write path, and a system-written value is not a choice somebody made.
        if (field.Mode != WorksheetFieldMode.Entry)
            return Result.Success();

        var entered = values.Where(value => !string.IsNullOrWhiteSpace(value.Value)).ToList();
        if (entered.Count == 0)
            return Result.Success();

        return field.Type == WorksheetFieldType.Table
            ? CheckCells(field, entered)
            : CheckScalar(field, entered);
    }

    private static Result CheckScalar(WorksheetField field, IEnumerable<WorksheetFieldValue> values)
    {
        if (!WorksheetFieldOptions.TryParse(field.OptionsJson, out var parsed))
            return Result.Success();

        var options = WorksheetFieldOptions.Distinct(parsed);
        if (options.Count == 0)
            return Result.Success();

        // Composite shapes (e.g. a Reagent's three sub-values) carry a ColumnKey and are not
        // choices; a choice field's value is the plain scalar row.
        foreach (var value in values.Where(value => string.IsNullOrEmpty(value.ColumnKey)))
        {
            var invalid = FirstInvalid(value.Value, options, field.Type == WorksheetFieldType.MultiSelect);
            if (invalid is not null)
                return QcWorksheetErrors.ValueNotAnOption(field.FieldKey, field.Label, invalid, options);
        }

        return Result.Success();
    }

    private static Result CheckCells(WorksheetField field, IEnumerable<WorksheetFieldValue> values)
    {
        var columns = WorksheetTableColumns.Read(field.ColumnDefinitions);
        if (columns is null)
            return Result.Success();

        foreach (var value in values.Where(value => !string.IsNullOrEmpty(value.ColumnKey)))
        {
            var column = WorksheetTableColumns.Find(columns, value.ColumnKey);
            // A calculated column's cells are system output, not a choice anyone made.
            if (column is null || column.IsCalculated || !column.OptionsReadable)
                continue;

            var options = WorksheetFieldOptions.Distinct(column.Options);
            if (options.Count == 0)
                continue;

            var invalid = FirstInvalid(value.Value, options, column.Type == WorksheetFieldType.MultiSelect);
            if (invalid is not null)
                return QcWorksheetErrors.CellValueNotAnOption(
                    field.FieldKey, value.ColumnKey, value.RowIndex, invalid, options);
        }

        return Result.Success();
    }

    /// <summary>The first chosen value that is not an option, or null when all are.</summary>
    private static string FirstInvalid(string value, List<string> options, bool multiSelect)
    {
        var chosen = multiSelect ? WorksheetFieldOptions.Selections(value) : [value.Trim()];
        return chosen.FirstOrDefault(item => !WorksheetFieldOptions.IsOption(options, item));
    }
}
