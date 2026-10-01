using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>One field as the result chooser sees it: a proposal field or a saved template field.</summary>
public sealed record ResultCandidate(string Key, string Label, WorksheetFieldType Type, WorksheetFieldMode Mode);

/// <summary>
/// The field of a raw-material section a Specification characteristic binds to (brief 09, "Bind
/// to the section's Result or Calculated field"). One rule for the proposal in the upload and
/// for a saved RM-NNN template, so both bind the same way:
/// <list type="number">
/// <item>the last Result-type field (a calculated assay, LOD or average, or an added Result);</item>
/// <item>otherwise the last Calculated field;</item>
/// <item>otherwise the last "Observation" / "Inference" entry, which is what those tests record;</item>
/// <item>otherwise the entry a test definition keys as its result ("…_result": a disintegration time).</item>
/// </list>
/// </summary>
public static class RawMaterialResultField
{
    private static readonly string[] ObservationLabels = ["observation", "observations", "inference", "result"];

    /// <summary>True for the labels an observation test records its outcome under.</summary>
    public static bool IsObservationLabel(string label) => ObservationLabels.Contains(ImportText.Canonical(label));

    public static string Choose(IEnumerable<ResultCandidate> fields)
    {
        var list = fields.ToList();
        return list.LastOrDefault(field => field.Type == WorksheetFieldType.Result)?.Key
               ?? list.LastOrDefault(field => field.Mode == WorksheetFieldMode.Calculated && field.Type != WorksheetFieldType.Table)?.Key
               ?? list.LastOrDefault(field => field.Mode == WorksheetFieldMode.Entry
                                              && ObservationLabels.Contains(ImportText.Canonical(field.Label)))?.Key
               ?? list.LastOrDefault(field => field.Mode == WorksheetFieldMode.Entry && field.Type != WorksheetFieldType.Table
                                              && field.Key.EndsWith("_result", StringComparison.OrdinalIgnoreCase))?.Key;
    }

    public static string Choose(ProposedWorksheetSection section) =>
        Choose(section.Fields.Select(field => new ResultCandidate(field.FieldKey, field.Label, field.Type, field.Mode)));
}
