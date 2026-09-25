using APP.Services.QcWorksheets;
using DOMAIN.Entities.QcWorksheets;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Template-save validation of per-row calculated table columns (build brief 07, Phase A).
/// <para>
/// A column is calculated when its definition says <c>"mode": "Calculated"</c>. Its
/// <c>formula</c> uses the ordinary formula syntax, where <c>{key}</c> resolves first to a
/// column of the <i>same row</i> and otherwise to a worksheet field, and
/// <c>AVG({table.column})</c>-style aggregates work exactly as they do in field formulas.
/// </para>
/// </summary>
internal static class WorksheetColumnFormulas
{
    /// <summary>
    /// <paramref name="findColumn"/> answers whether a table has a column: true, false, or null
    /// when the table's definitions cannot be read.
    /// </summary>
    public static Result Validate(
        CreateWorksheetFieldRequest field,
        IReadOnlySet<string> knownKeys,
        IReadOnlySet<string> tableKeys,
        Func<string, string, bool?> findColumn)
    {
        if (field.Type != WorksheetFieldType.Table)
            return Result.Success();

        var columns = WorksheetTableColumns.Read(field.ColumnDefinitions);
        if (columns is null)
            return Result.Success();

        var columnKeys = columns
            .Select(column => column.Key?.Trim())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
        {
            var name = $"{field.FieldKey}.{column.Key}";
            var hasFormula = !string.IsNullOrWhiteSpace(column.Formula);

            if (!column.IsCalculated)
            {
                if (hasFormula)
                    return QcWorksheetErrors.InvalidFormula(
                        name, "a column with a formula must be \"mode\": \"Calculated\", or it would never be computed");
                continue;
            }

            if (column.IsFixed)
                return QcWorksheetErrors.InvalidFormula(
                    name, "a calculated column cannot also be a fixed or row-header column");

            if (!hasFormula)
                return QcWorksheetErrors.InvalidFormula(name, "a formula is required for a calculated column");

            var analysis = QcFormulaEvaluator.Analyze(column.Formula);
            if (!analysis.IsValid)
                return QcWorksheetErrors.InvalidFormula(name, analysis.Error);

            foreach (var reference in analysis.ScalarFieldKeys)
            {
                if (string.Equals(reference, column.Key?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return QcWorksheetErrors.InvalidFormula(name, "a formula cannot reference its own column");

                if (!columnKeys.Contains(reference) && !knownKeys.Contains(reference))
                    return QcWorksheetErrors.InvalidFormula(
                        name,
                        $"it references '{reference}', which is neither a column of this table nor a field "
                        + "in this template");
            }

            foreach (var reference in analysis.TableReferences)
            {
                var check = ValidateTableReference(name, reference, knownKeys, tableKeys, findColumn);
                if (!check.IsSuccess)
                    return check;
            }
        }

        return Result.Success();
    }

    /// <summary>The aggregate-reference rule shared by field formulas and column formulas.</summary>
    public static Result ValidateTableReference(
        string owner,
        QcFormulaTableReference reference,
        IReadOnlySet<string> knownKeys,
        IReadOnlySet<string> tableKeys,
        Func<string, string, bool?> findColumn)
    {
        if (!knownKeys.Contains(reference.TableFieldKey))
            return QcWorksheetErrors.InvalidFormula(
                owner, $"it references '{reference.TableFieldKey}', which is not a field in this template");

        if (!tableKeys.Contains(reference.TableFieldKey))
            return QcWorksheetErrors.InvalidFormula(
                owner,
                $"'{reference.TableFieldKey}' is not a Table field, so {reference.Function}() cannot aggregate it");

        if (findColumn(reference.TableFieldKey, reference.ColumnKey) == false)
            return QcWorksheetErrors.InvalidFormula(
                owner, $"'{reference.TableFieldKey}' has no column '{reference.ColumnKey}'");

        return Result.Success();
    }
}
