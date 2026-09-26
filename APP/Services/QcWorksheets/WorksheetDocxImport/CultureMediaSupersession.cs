using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Locked decision 3 as refined: the newer media form wins, but an older-form sheet stays
/// importable when no newer version exists (EEBM exists only in the older form).
/// <para>
/// An older-form proposal is <see cref="WorksheetImportFlagCodes.SupersededFormatBlocked"/>
/// when its newer twin is in the same upload, or is already saved as a non-superseded
/// MediaQualification template; otherwise it keeps the <see cref="WorksheetImportFlagCodes.SupersededFormat"/>
/// warning. The saved-template check goes through <see cref="IWorksheetImportCatalog"/>, so this
/// stays a pure function of (proposals, catalog).
/// </para>
/// </summary>
public static class CultureMediaSupersession
{
    public static MediumIdentity Identify(string name, string code) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : new MediumIdentity
            {
                Name = name,
                NameKey = ImportText.Canonical(name),
                Code = string.IsNullOrWhiteSpace(code) ? null : code,
                CodeKey = string.IsNullOrWhiteSpace(code) ? null : ImportText.Canonical(code)
            };

    /// <summary>Same medium: the same name, and codes that agree wherever both forms print one.</summary>
    public static bool SameMedium(MediumIdentity left, MediumIdentity right) =>
        left is not null && right is not null
        && left.NameKey == right.NameKey
        && (left.CodeKey is null || right.CodeKey is null || left.CodeKey == right.CodeKey);

    /// <summary>Upgrades each older-form proposal's warning to a block when a newer twin exists.</summary>
    public static void Apply(IReadOnlyList<WorksheetImportProposal> batch, IWorksheetImportCatalog catalog)
    {
        catalog ??= InMemoryWorksheetImportCatalog.Empty;

        foreach (var proposal in batch.Where(IsOlderForm))
        {
            var twin = batch.FirstOrDefault(other => other != proposal
                && other.Family == ArdFamily.CultureMedia
                && other.FormatVersion == nameof(CultureMediaFormat.New)
                && SameMedium(other.Medium, proposal.Medium));

            var saved = proposal.Medium is null
                ? null
                : catalog.FindMediaTemplate(proposal.Medium.Name, proposal.Medium.Code);

            if (twin is null && saved is null)
                continue;

            var warning = proposal.Flags.FirstOrDefault(flag => flag.Code == WorksheetImportFlagCodes.SupersededFormat);
            proposal.Flags.Remove(warning);
            proposal.Flags.Insert(0, new WorksheetImportFlag
            {
                Code = WorksheetImportFlagCodes.SupersededFormatBlocked,
                Location = warning?.Location,
                Message = twin is not null
                    ? $"Older media form for '{proposal.Medium.Name}'; the newer form is in this upload ('{twin.FileName}'). Import that one instead."
                    : $"Older media form for '{proposal.Medium.Name}'; a newer template is already saved ({saved.Code}). This proposal cannot be saved."
            });
        }
    }

    private static bool IsOlderForm(WorksheetImportProposal proposal) =>
        proposal.Family == ArdFamily.CultureMedia
        && proposal.FormatVersion == nameof(CultureMediaFormat.Superseded)
        && proposal.Flags.Any(flag => flag.Code == WorksheetImportFlagCodes.SupersededFormat);
}
