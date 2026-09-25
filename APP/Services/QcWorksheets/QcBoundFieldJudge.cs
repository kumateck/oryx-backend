using APP.Repository.QcWorksheets;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Which worksheet fields OOS detection judges, and how a value of each kind is judged.
/// <para>
/// The rule is <b>"judged = bound by a characteristic"</b>: whatever field a Characteristic on
/// the round's pinned Specification names in its <c>SourceFieldKey</c> is judged against it,
/// whatever its type — a specified-organism result is as often a Select ("Absence of E. coli")
/// as a Result. This class only decides which fields are <i>candidates</i>; the caller still
/// skips a candidate no Characteristic binds.
/// </para>
/// </summary>
internal static class QcBoundFieldJudge
{
    /// <summary>
    /// A field is a candidate when it holds one scalar value an analyst entered or the system
    /// computed.
    /// <list type="bullet">
    ///   <item><description>Result fields are candidates in every mode — exactly the M4 rule, kept unchanged.</description></item>
    ///   <item><description>Any other type is a candidate in Entry or Calculated mode. Constant fields are template text, never a result.</description></item>
    ///   <item><description>Heading and Instructions hold no value; Table, Reagent and ReferenceStandard hold several rows, and a Characteristic has no column/row key to say which one it means.</description></item>
    /// </list>
    /// </summary>
    public static bool IsCandidate(WorksheetFieldType type, WorksheetFieldMode mode)
    {
        if (type == WorksheetFieldType.Result)
            return true;

        if (mode is not (WorksheetFieldMode.Entry or WorksheetFieldMode.Calculated))
            return false;

        return type is not (WorksheetFieldType.Heading
            or WorksheetFieldType.Instructions
            or WorksheetFieldType.Table
            or WorksheetFieldType.Reagent
            or WorksheetFieldType.ReferenceStandard);
    }

    /// <summary>
    /// Judges one field's submitted value. Every type goes straight to
    /// <see cref="LimitEvaluator.Evaluate"/> except MultiSelect, whose value is a JSON array of
    /// choices: each choice is judged on its own and the worst outcome stands (ActionOos &gt;
    /// ManualReview &gt; Alert &gt; Compliant), so the field complies only when every choice
    /// does. An empty selection is judged as a blank value.
    /// </summary>
    public static LimitEvaluation Evaluate(
        SpecificationCharacteristic characteristic, WorksheetFieldType type, string submitted)
    {
        if (type != WorksheetFieldType.MultiSelect || string.IsNullOrWhiteSpace(submitted))
            return LimitEvaluator.Evaluate(characteristic, submitted);

        var choices = WorksheetFieldOptions.Selections(submitted)
            .Where(choice => !string.IsNullOrWhiteSpace(choice))
            .ToList();

        if (choices.Count == 0)
            return LimitEvaluator.Evaluate(characteristic, null);

        return choices
            .Select(choice => LimitEvaluator.Evaluate(characteristic, choice))
            .OrderByDescending(evaluation => Severity(evaluation.Outcome))
            .First();
    }

    private static int Severity(LimitOutcome outcome) => outcome switch
    {
        LimitOutcome.ActionOos => 3,
        LimitOutcome.ManualReview => 2,
        LimitOutcome.Alert => 1,
        _ => 0
    };
}
