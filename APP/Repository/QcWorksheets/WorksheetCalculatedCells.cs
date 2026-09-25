using APP.Services.QcWorksheets;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Per-row calculated table columns on the instance side: which stored cells are system-computed
/// rather than entered, and writing the computed cells down at submission.
/// </summary>
internal static class WorksheetCalculatedCells
{
    public static bool IsCalculatedColumn(string definitions, string columnKey) =>
        !string.IsNullOrWhiteSpace(columnKey)
        && WorksheetTableColumns.Find(WorksheetTableColumns.Read(definitions), columnKey)?.IsCalculated == true;

    /// <summary>The field's values minus any cell of a calculated column.</summary>
    public static List<WorksheetFieldValue> EnteredValues(WorksheetField field, IEnumerable<WorksheetFieldValue> values)
    {
        if (field.Type != WorksheetFieldType.Table)
            return values.ToList();

        var columns = WorksheetTableColumns.Read(field.ColumnDefinitions);
        return values
            .Where(value => WorksheetTableColumns.Find(columns, value.ColumnKey)?.IsCalculated != true)
            .ToList();
    }

    /// <summary>
    /// Writes one value per computed (row, column) exactly like an entered cell, and removes any
    /// previously computed cell that this submission no longer produces — a re-submission is
    /// recomputed from scratch, so a row the analyst has since cleared leaves no stale result.
    /// </summary>
    public static void Persist(
        ApplicationDbContext context,
        WorksheetField table,
        List<WorksheetFieldValue> stored,
        IReadOnlyCollection<QcWorksheetCalculator.QcCalculatedValue> computed,
        Guid instanceId,
        Guid userId)
    {
        var columns = WorksheetTableColumns.Read(table.ColumnDefinitions);
        var previous = stored
            .Where(value => value.RowIndex.HasValue
                && WorksheetTableColumns.Find(columns, value.ColumnKey)?.IsCalculated == true)
            .ToList();

        foreach (var cell in computed)
        {
            var existing = previous.FirstOrDefault(value =>
                value.RowIndex == cell.RowIndex
                && string.Equals(value.ColumnKey, cell.ColumnKey, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                context.QcWorksheetFieldValues.Add(new WorksheetFieldValue
                {
                    Id = Guid.NewGuid(),
                    WorksheetInstanceId = instanceId,
                    FieldKey = table.FieldKey,
                    RowIndex = cell.RowIndex,
                    ColumnKey = cell.ColumnKey,
                    Value = cell.Value,
                    EnteredById = userId,
                    EnteredAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId
                });
                continue;
            }

            previous.Remove(existing);
            existing.Value = cell.Value;
            existing.EnteredById = userId;
            existing.EnteredAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.LastUpdatedById = userId;
        }

        if (previous.Count > 0)
            context.QcWorksheetFieldValues.RemoveRange(previous);
    }
}
