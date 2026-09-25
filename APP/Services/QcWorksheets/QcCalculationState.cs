using System.Globalization;
using APP.Repository.QcWorksheets;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// One thing to calculate: a Calculated-mode field (<see cref="ColumnKey"/> null), or one row
/// of a calculated table column.
/// </summary>
internal sealed class QcCalculationUnit(
    WorksheetField field, string columnKey, int? rowIndex, int columnIndex, string formula)
{
    public WorksheetField Field { get; } = field;
    public string ColumnKey { get; } = columnKey;
    public int? RowIndex { get; } = rowIndex;
    public int ColumnIndex { get; } = columnIndex;
    public string Formula { get; } = formula;
}

/// <summary>
/// The inputs <see cref="QcWorksheetCalculator"/> works from, and the results it feeds back in.
/// <para>
/// A non-blank entry that is not a number is recorded as <i>unusable</i> rather than merely left
/// out: "TNTC" on a plate count is a real entry, and a formula that depends on it must fail
/// loudly saying so, not quietly behave as though the cell were empty. For an aggregate, one
/// unusable cell makes the whole column unusable; for a per-row formula it poisons that row.
/// </para>
/// </summary>
internal sealed partial class QcCalculationState
{
    private const char Separator = '\u001f';

    private readonly Dictionary<string, double> _scalars = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _unusableScalars = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SortedDictionary<int, double>> _columns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _unusableColumns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _unusableCells = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _pendingCells = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<WorksheetTableColumn>> _tables = new(StringComparer.OrdinalIgnoreCase);

    internal List<QcCalculationUnit> Units { get; } = [];

    /// <summary>Why the last lookup failed, when there is something specific to say.</summary>
    internal string Diagnostic { get; set; }

    internal QcCalculationState(
        IReadOnlyCollection<WorksheetField> fields, IReadOnlyCollection<WorksheetFieldValue> values)
    {
        foreach (var field in fields.Where(field => field.Mode == WorksheetFieldMode.Calculated))
            Units.Add(new QcCalculationUnit(field, null, null, 0, field.FormulaExpression));

        // A calculated field's or cell's own previously persisted result is never an input.
        // Re-submitting after a correction recomputes from the entries standing now.
        var calculatedKeys = Units.Select(unit => unit.Field.FieldKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields.Where(field => field.Type == WorksheetFieldType.Table))
            _tables[field.FieldKey] = WorksheetTableColumns.Read(field.ColumnDefinitions) ?? [];

        foreach (var value in values.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
        {
            if (value.ColumnKey is null && value.RowIndex is null)
                AddScalar(value, calculatedKeys);
            else if (value.ColumnKey is not null && value.RowIndex.HasValue
                     && !IsCalculatedColumn(value.FieldKey, value.ColumnKey))
                AddCell(value);
        }

        foreach (var field in fields.Where(field => field.Type == WorksheetFieldType.Table))
            AddRowUnits(field, values);
    }

    internal static bool TryParse(string value, out double number) =>
        double.TryParse(value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    /// <summary>Feeds a result back in, which is what lets a later formula reference it.</summary>
    internal void Complete(QcCalculationUnit unit, double number)
    {
        if (unit.ColumnKey is null)
        {
            _scalars[unit.Field.FieldKey] = number;
            return;
        }

        Column(unit.Field.FieldKey, unit.ColumnKey)[unit.RowIndex!.Value] = number;
        _pendingCells[ColumnId(unit.Field.FieldKey, unit.ColumnKey)]--;
    }

    private void AddScalar(WorksheetFieldValue value, IReadOnlySet<string> calculatedKeys)
    {
        if (calculatedKeys.Contains(value.FieldKey ?? string.Empty))
            return;

        if (TryParse(value.Value, out var number))
            _scalars[value.FieldKey] = number;
        else
            _unusableScalars.TryAdd(value.FieldKey, value.Value.Trim());
    }

    private void AddCell(WorksheetFieldValue value)
    {
        if (TryParse(value.Value, out var number))
        {
            Column(value.FieldKey, value.ColumnKey)[value.RowIndex!.Value] = number;
            return;
        }

        _unusableColumns.TryAdd(ColumnId(value.FieldKey, value.ColumnKey), value.Value.Trim());
        _unusableCells.TryAdd(CellId(value.FieldKey, value.ColumnKey, value.RowIndex!.Value), value.Value.Trim());
    }

    /// <summary>
    /// A fixed table computes every one of its fixed rows; an open-ended table computes each row
    /// that has an entered (non-calculated) cell.
    /// </summary>
    private void AddRowUnits(WorksheetField table, IReadOnlyCollection<WorksheetFieldValue> values)
    {
        var columns = _tables[table.FieldKey];
        var calculated = columns.Where(column => column.IsCalculated && !string.IsNullOrWhiteSpace(column.Key)).ToList();
        if (calculated.Count == 0)
            return;

        var fixedColumns = columns.Where(column => column.FixedValuesIsArray).ToList();
        var rows = fixedColumns.Count > 0
            ? Enumerable.Range(0, fixedColumns.Max(column => column.FixedValueCount)).ToList()
            : values
                .Where(value => string.Equals(value.FieldKey, table.FieldKey, StringComparison.OrdinalIgnoreCase)
                    && value.RowIndex.HasValue && value.ColumnKey is not null
                    && !string.IsNullOrWhiteSpace(value.Value)
                    && !IsCalculatedColumn(table.FieldKey, value.ColumnKey))
                .Select(value => value.RowIndex!.Value)
                .Distinct()
                .Order()
                .ToList();

        foreach (var row in rows)
        {
            foreach (var (column, index) in calculated.Select((column, index) => (column, index)))
            {
                Units.Add(new QcCalculationUnit(table, column.Key, row, index, column.Formula));
                var id = ColumnId(table.FieldKey, column.Key);
                _pendingCells[id] = _pendingCells.GetValueOrDefault(id) + 1;
            }
        }
    }

    private bool IsCalculatedColumn(string tableKey, string columnKey) =>
        FindColumn(tableKey, columnKey)?.IsCalculated == true;

    private WorksheetTableColumn FindColumn(string tableKey, string columnKey) =>
        _tables.TryGetValue(tableKey ?? string.Empty, out var columns)
            ? WorksheetTableColumns.Find(columns, columnKey)
            : null;

    private SortedDictionary<int, double> Column(string tableKey, string columnKey)
    {
        var id = ColumnId(tableKey, columnKey);
        if (!_columns.TryGetValue(id, out var column))
            _columns[id] = column = new SortedDictionary<int, double>();
        return column;
    }

    private static string ColumnId(string tableKey, string columnKey) => $"{tableKey}{Separator}{columnKey}";

    private static string CellId(string tableKey, string columnKey, int row) =>
        $"{tableKey}{Separator}{columnKey}{Separator}{row}";
}
