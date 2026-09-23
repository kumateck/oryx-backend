using System.Globalization;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Evaluates a worksheet's Calculated-mode fields against the values actually entered on one
/// instance, so the computed result can be written down as a real
/// <see cref="WorksheetFieldValue"/> rather than recomputed for display on every read.
/// <para>
/// This is the runtime counterpart to <see cref="QcFormulaEvaluator.Analyze"/>, which
/// <c>WorksheetTemplateRepository</c> calls at template-save time to prove a formula is
/// well-formed. Analysis proves the formula parses; this proves it produces a number from the
/// data in front of it, which is the part a COA later has to cite.
/// </para>
/// </summary>
public static class QcWorksheetCalculator
{
    /// <summary>One Calculated field and the value computed for it.</summary>
    public sealed record QcCalculatedValue(WorksheetField Field, double Number, string Value);

    /// <summary>
    /// The canonical stored form of a calculated number. Fixed-point with up to ten decimal
    /// places: never scientific notation (a CFU/g count of 1.2e10 must read as a count on a
    /// printed COA), invariant culture so the stored digits do not depend on server locale,
    /// and trailing zeros trimmed so a whole number stores as one.
    /// </summary>
    public static string Format(double value) =>
        value.ToString("0.##########", CultureInfo.InvariantCulture);

    /// <summary>
    /// Evaluates every Calculated-mode field in <paramref name="allFields"/> against
    /// <paramref name="values"/>.
    /// <para>
    /// A Calculated field may reference another Calculated field's key, so this runs to a fixed
    /// point rather than in a single pass: each round evaluates whatever has become resolvable,
    /// and a round that resolves nothing new ends it. Whatever is still unresolved at that point
    /// is either missing an input or sitting in a reference cycle — both of which surface as a
    /// failure naming the field, never as a silently skipped result.
    /// </para>
    /// </summary>
    /// <returns>True when every Calculated field produced a finite number.</returns>
    public static bool TryEvaluateAll(
        IReadOnlyCollection<WorksheetField> allFields,
        IReadOnlyCollection<WorksheetFieldValue> values,
        out IReadOnlyList<QcCalculatedValue> computed,
        out WorksheetField failedField,
        out string error)
    {
        computed = [];
        failedField = null;
        error = null;

        var pending = allFields
            .Where(field => field.Mode == WorksheetFieldMode.Calculated)
            .ToList();

        if (pending.Count == 0)
            return true;

        // A Calculated field's own previously persisted result is never an input. Re-submitting
        // after a correction must recompute from the entries standing now, not resolve against
        // the number the last submission happened to store.
        var calculatedKeys = pending
            .Select(field => field.FieldKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scalars = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var unusableScalars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var columns = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        var unusableColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        BuildScalars(values, calculatedKeys, scalars, unusableScalars);
        BuildColumns(values, columns, unusableColumns);

        var resolver = new Resolver(scalars, unusableScalars, columns, unusableColumns);

        var results = new List<QcCalculatedValue>();
        var lastError = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Fields whose failure was explained by the data itself rather than by a dependency that
        // had not been computed yet — the root causes, as opposed to the fields downstream of one.
        var rootCauses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (pending.Count > 0)
        {
            var progressed = false;

            foreach (var field in pending.ToList())
            {
                resolver.ResetDiagnostic();

                if (!QcFormulaEvaluator.TryEvaluate(
                        field.FormulaExpression, resolver, out var number, out var reason))
                {
                    // The resolver's own account of why a reference was unusable is more use to
                    // an analyst than the evaluator's generic "no value is available".
                    if (resolver.Diagnostic is null)
                    {
                        lastError[field.FieldKey] = reason;
                        rootCauses.Remove(field.FieldKey);
                    }
                    else
                    {
                        lastError[field.FieldKey] = resolver.Diagnostic;
                        rootCauses.Add(field.FieldKey);
                    }

                    continue;
                }

                // Feeding the result back in is what lets a later formula reference this one.
                scalars[field.FieldKey] = number;
                results.Add(new QcCalculatedValue(field, number, Format(number)));
                pending.Remove(field);
                progressed = true;
            }

            if (!progressed)
                break;
        }

        if (pending.Count > 0)
        {
            // A field that failed on its own data is reported ahead of one that merely failed
            // because it depends on that field — an analyst told "mean_count holds 'TNTC'" can
            // act, one told "cfu_per_g has no value for mean_count" has to work backwards.
            // Template order breaks the tie, so the earliest such field is named.
            failedField = pending
                .OrderBy(field => rootCauses.Contains(field.FieldKey) ? 0 : 1)
                .ThenBy(field => field.Order)
                .First();

            error = lastError.GetValueOrDefault(failedField.FieldKey)
                ?? "The formula could not be evaluated.";
            return false;
        }

        computed = results;
        return true;
    }

    /// <summary>
    /// Plain scalar values, parsed as numbers.
    /// <para>
    /// A non-blank entry that is not a number is recorded as <i>unusable</i> rather than merely
    /// left out: "TNTC" on a plate count is a real entry, and a formula that depends on it must
    /// fail loudly saying so, not quietly behave as though the field were empty.
    /// </para>
    /// </summary>
    private static void BuildScalars(
        IReadOnlyCollection<WorksheetFieldValue> values,
        IReadOnlySet<string> calculatedKeys,
        IDictionary<string, double> scalars,
        IDictionary<string, string> unusable)
    {
        foreach (var value in values.Where(item => item.ColumnKey is null && item.RowIndex is null))
        {
            if (calculatedKeys.Contains(value.FieldKey ?? string.Empty))
                continue;

            if (string.IsNullOrWhiteSpace(value.Value))
                continue;

            if (TryParse(value.Value, out var number))
                scalars[value.FieldKey] = number;
            else
                unusable.TryAdd(value.FieldKey, value.Value.Trim());
        }
    }

    /// <summary>
    /// Table cells, grouped into the columns AVG/SUM/MIN/MAX/RSD aggregate over.
    /// <para>
    /// One non-numeric cell makes the whole column unusable. Averaging the rows that happen to
    /// parse and discarding the rest would produce a confident number from an incomplete
    /// column — the worst available outcome for a result that goes on a certificate.
    /// </para>
    /// </summary>
    private static void BuildColumns(
        IReadOnlyCollection<WorksheetFieldValue> values,
        IDictionary<string, List<double>> columns,
        IDictionary<string, string> unusable)
    {
        foreach (var value in values
                     .Where(item => item.RowIndex.HasValue && item.ColumnKey is not null)
                     .OrderBy(item => item.RowIndex))
        {
            if (string.IsNullOrWhiteSpace(value.Value))
                continue;

            var key = ColumnKey(value.FieldKey, value.ColumnKey);

            if (!TryParse(value.Value, out var number))
            {
                unusable.TryAdd(key, value.Value.Trim());
                continue;
            }

            if (!columns.TryGetValue(key, out var list))
                columns[key] = list = [];

            list.Add(number);
        }
    }

    private static bool TryParse(string value, out double number) =>
        double.TryParse(
            value?.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out number);

    private static string ColumnKey(string tableFieldKey, string columnKey) =>
        $"{tableFieldKey}{columnKey}";

    private sealed class Resolver(
        IReadOnlyDictionary<string, double> scalars,
        IReadOnlyDictionary<string, string> unusableScalars,
        IReadOnlyDictionary<string, List<double>> columns,
        IReadOnlyDictionary<string, string> unusableColumns) : IQcFormulaValueResolver
    {
        /// <summary>Why the last lookup failed, when there is something specific to say.</summary>
        internal string Diagnostic { get; private set; }

        internal void ResetDiagnostic() => Diagnostic = null;

        public bool TryGetScalar(string fieldKey, out double value)
        {
            if (scalars.TryGetValue(fieldKey, out value))
                return true;

            if (unusableScalars.TryGetValue(fieldKey, out var raw))
                Diagnostic = $"field '{fieldKey}' holds '{raw}', which is not a number.";

            return false;
        }

        public bool TryGetColumn(string tableFieldKey, string columnKey, out IReadOnlyList<double> values)
        {
            values = null;
            var key = ColumnKey(tableFieldKey, columnKey);

            // Checked before the parsed rows: a column with one bad cell is unusable even
            // though some of its rows did parse.
            if (unusableColumns.TryGetValue(key, out var raw))
            {
                Diagnostic =
                    $"table column '{tableFieldKey}.{columnKey}' holds '{raw}', which is not a number.";
                return false;
            }

            if (!columns.TryGetValue(key, out var list))
                return false;

            values = list;
            return true;
        }
    }
}
