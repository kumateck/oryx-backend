using System.Globalization;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Evaluates a worksheet's Calculated-mode fields, and its per-row calculated table columns,
/// against the values actually entered on one instance, so each computed result can be written
/// down as a real <see cref="WorksheetFieldValue"/> rather than recomputed for display on every
/// read.
/// <para>
/// This is the runtime counterpart to <see cref="QcFormulaEvaluator.Analyze"/>, which
/// <c>WorksheetTemplateRepository</c> calls at template-save time to prove a formula is
/// well-formed. Analysis proves the formula parses; this proves it produces a number from the
/// data in front of it, which is the part a COA later has to cite.
/// </para>
/// </summary>
public static class QcWorksheetCalculator
{
    /// <summary>
    /// One computed value: a Calculated field (<see cref="ColumnKey"/> and
    /// <see cref="RowIndex"/> null), or one row of a calculated table column.
    /// </summary>
    public sealed record QcCalculatedValue(
        WorksheetField Field, double Number, string Value, int? RowIndex = null, string ColumnKey = null);

    /// <summary>
    /// What could not be calculated, and why. <see cref="ColumnKey"/> and
    /// <see cref="RowIndex"/> are set when the failure is one cell of a calculated column.
    /// </summary>
    public sealed record QcCalculationFailure(WorksheetField Field, string ColumnKey, int? RowIndex, string Reason);

    /// <summary>
    /// The canonical stored form of a calculated number. Fixed-point with up to ten decimal
    /// places: never scientific notation (a CFU/g count of 1.2e10 must read as a count on a
    /// printed COA), invariant culture so the stored digits do not depend on server locale,
    /// and trailing zeros trimmed so a whole number stores as one.
    /// </summary>
    public static string Format(double value) =>
        value.ToString("0.##########", CultureInfo.InvariantCulture);

    /// <summary>
    /// Evaluates every Calculated-mode field and every row of every calculated table column in
    /// <paramref name="allFields"/> against <paramref name="values"/>.
    /// <para>
    /// Any of them may reference another's result, so this runs to a fixed point rather than in
    /// a single pass: each round evaluates whatever has become resolvable, and a round that
    /// resolves nothing new ends it. Whatever is still unresolved at that point is either
    /// missing an input or sitting in a reference cycle — both of which surface as a failure
    /// naming the field (or the table cell), never as a silently skipped result.
    /// </para>
    /// </summary>
    /// <returns>True when every calculation produced a finite number.</returns>
    public static bool TryEvaluateAll(
        IReadOnlyCollection<WorksheetField> allFields,
        IReadOnlyCollection<WorksheetFieldValue> values,
        out IReadOnlyList<QcCalculatedValue> computed,
        out QcCalculationFailure failure)
    {
        computed = [];
        failure = null;

        var state = new QcCalculationState(allFields, values);
        var pending = state.Units.ToList();

        if (pending.Count == 0)
            return true;

        var results = new List<QcCalculatedValue>();
        var lastError = new Dictionary<QcCalculationUnit, string>();

        // Units whose failure was explained by the data itself rather than by a dependency that
        // had not been computed yet — the root causes, as opposed to those downstream of one.
        var rootCauses = new HashSet<QcCalculationUnit>();

        while (pending.Count > 0)
        {
            var progressed = false;

            foreach (var unit in pending.ToList())
            {
                state.Diagnostic = null;

                if (!QcFormulaEvaluator.TryEvaluate(
                        unit.Formula, state.ResolverFor(unit), out var number, out var reason))
                {
                    // The resolver's own account of why a reference was unusable is more use to
                    // an analyst than the evaluator's generic "no value is available".
                    if (state.Diagnostic is null)
                    {
                        lastError[unit] = reason;
                        rootCauses.Remove(unit);
                    }
                    else
                    {
                        lastError[unit] = state.Diagnostic;
                        rootCauses.Add(unit);
                    }

                    continue;
                }

                state.Complete(unit, number);
                results.Add(new QcCalculatedValue(unit.Field, number, Format(number), unit.RowIndex, unit.ColumnKey));
                pending.Remove(unit);
                progressed = true;
            }

            if (!progressed)
                break;
        }

        if (pending.Count > 0)
        {
            // A calculation that failed on its own data is reported ahead of one that merely
            // failed because it depends on it — an analyst told "mean_count holds 'TNTC'" can
            // act, one told "cfu_per_g has no value for mean_count" has to work backwards.
            // Template order, then row, then column breaks the tie.
            var failed = pending
                .OrderBy(unit => rootCauses.Contains(unit) ? 0 : 1)
                .ThenBy(unit => unit.Field.Order)
                .ThenBy(unit => unit.RowIndex ?? -1)
                .ThenBy(unit => unit.ColumnIndex)
                .First();

            failure = new QcCalculationFailure(
                failed.Field,
                failed.ColumnKey,
                failed.RowIndex,
                lastError.GetValueOrDefault(failed) ?? "The formula could not be evaluated.");
            return false;
        }

        computed = results;
        return true;
    }
}
