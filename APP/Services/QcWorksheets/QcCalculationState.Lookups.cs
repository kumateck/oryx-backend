using APP.Repository.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Value lookups for <see cref="QcCalculationState"/>: how a formula reference resolves for a
/// Calculated field, and for one row of a calculated table column.
/// </summary>
internal sealed partial class QcCalculationState
{
    internal IQcFormulaValueResolver ResolverFor(QcCalculationUnit unit) =>
        unit.ColumnKey is null
            ? new ScalarResolver(this)
            : new RowResolver(this, unit.Field.FieldKey, unit.RowIndex!.Value);

    private bool TryGetScalar(string fieldKey, out double value)
    {
        if (_scalars.TryGetValue(fieldKey, out value))
            return true;

        if (_unusableScalars.TryGetValue(fieldKey, out var raw))
            Diagnostic = $"field '{fieldKey}' holds '{raw}', which is not a number.";

        return false;
    }

    /// <summary>
    /// A whole column for AVG/SUM/MIN/MAX/RSD. Calculated cells count once every row of their
    /// column has been computed — the same way a calculated field feeds later formulas.
    /// </summary>
    private bool TryGetColumn(string tableKey, string columnKey, out IReadOnlyList<double> values)
    {
        values = null;
        var id = ColumnId(tableKey, columnKey);

        if (_pendingCells.GetValueOrDefault(id) > 0)
            return false;

        // Checked before the parsed rows: a column with one bad cell is unusable even though
        // some of its rows did parse.
        if (_unusableColumns.TryGetValue(id, out var raw))
        {
            Diagnostic = $"table column '{tableKey}.{columnKey}' holds '{raw}', which is not a number.";
            return false;
        }

        if (!_columns.TryGetValue(id, out var column) || column.Count == 0)
            return false;

        values = column.Values.ToList();
        return true;
    }

    /// <summary>
    /// A same-row column value: a calculated cell once computed, a fixed column's per-row value,
    /// or the analyst's entry.
    /// </summary>
    private bool TryGetCell(string tableKey, WorksheetTableColumn column, int row, out double value)
    {
        value = 0;

        if (column.IsCalculated)
            return _columns.TryGetValue(ColumnId(tableKey, column.Key), out var computed)
                && computed.TryGetValue(row, out value);

        if (column.FixedValuesIsArray)
        {
            var label = row < column.FixedValueCount ? column.FixedValues[row] : null;
            if (label is not null && TryParse(label, out value))
                return true;

            Diagnostic = label is null
                ? $"column '{column.Key}' has no value in this row."
                : $"column '{column.Key}' holds '{label}' in this row, which is not a number.";
            return false;
        }

        if (_columns.TryGetValue(ColumnId(tableKey, column.Key), out var entered) && entered.TryGetValue(row, out value))
            return true;

        Diagnostic = _unusableCells.TryGetValue(CellId(tableKey, column.Key, row), out var raw)
            ? $"column '{column.Key}' holds '{raw}', which is not a number."
            : $"column '{column.Key}' has no value in this row.";
        return false;
    }

    private sealed class ScalarResolver(QcCalculationState state) : IQcFormulaValueResolver
    {
        public bool TryGetScalar(string fieldKey, out double value) => state.TryGetScalar(fieldKey, out value);

        public bool TryGetColumn(string tableFieldKey, string columnKey, out IReadOnlyList<double> values) =>
            state.TryGetColumn(tableFieldKey, columnKey, out values);
    }

    /// <summary><c>{key}</c> resolves to a column of the same row first, then to a worksheet field.</summary>
    private sealed class RowResolver(QcCalculationState state, string tableKey, int row) : IQcFormulaValueResolver
    {
        public bool TryGetScalar(string key, out double value)
        {
            var column = state.FindColumn(tableKey, key);
            return column is null
                ? state.TryGetScalar(key, out value)
                : state.TryGetCell(tableKey, column, row, out value);
        }

        public bool TryGetColumn(string tableFieldKey, string columnKey, out IReadOnlyList<double> values) =>
            state.TryGetColumn(tableFieldKey, columnKey, out values);
    }
}
