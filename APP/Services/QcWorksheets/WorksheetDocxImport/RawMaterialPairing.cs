using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Binds a raw-material Specification document's characteristics to its worksheet (brief 09), a
/// batch rule like <see cref="SharedTemplates"/>. The pair shares the material number NNN:
/// <list type="number">
/// <item>Worksheet NNN in the same upload → each characteristic binds to the Result or Calculated
/// field of the section whose test name matches (<see cref="RawMaterialTestNames"/>). A test with
/// no section gets an added section (Result + "Attach print out") in that worksheet proposal,
/// flagged <see cref="WorksheetImportFlagCodes.AddedForSpecification"/> (locked decision 2).</item>
/// <item>Otherwise a saved RM-NNN template → bind to its sections; it is never modified, so a test
/// with no field is flagged <see cref="WorksheetImportFlagCodes.FieldNotOnTemplate"/>.</item>
/// <item>Otherwise → <see cref="WorksheetImportFlagCodes.WorksheetNotFound"/>: import the worksheet first.</item>
/// </list>
/// </summary>
public static class RawMaterialPairing
{
    public static void Apply(IReadOnlyList<WorksheetImportProposal> batch, IWorksheetImportCatalog catalog)
    {
        catalog ??= InMemoryWorksheetImportCatalog.Empty;

        foreach (var specification in batch.Where(proposal => proposal.Family == ArdFamily.RawMaterialSpecification
                                                               && proposal.RawMaterial is not null
                                                               && proposal.SpecificationProposals.Count > 0))
        {
            var info = specification.RawMaterial;
            var worksheet = info.PairingKey is null
                ? null
                : batch.FirstOrDefault(proposal => proposal.Family == ArdFamily.RawMaterialChemical
                                                   && proposal.Template is not null
                                                   && proposal.RawMaterial?.PairingKey == info.PairingKey);

            if (worksheet is not null)
            {
                BindInUpload(specification, worksheet);
                continue;
            }

            if (catalog.FindRawMaterialTemplate(info.TemplateCode) is { } saved)
            {
                BindToSaved(specification, saved);
                continue;
            }

            info.Pairing = RawMaterialPairingStatus.Missing;
            foreach (var characteristic in specification.SpecificationProposals)
                characteristic.SourceFieldKey = null;
            Flag(specification, WorksheetImportFlagCodes.WorksheetNotFound, info.PairingKey is null
                ? "The material number is unknown, so no worksheet can be paired; set it and import the worksheet first."
                : $"No worksheet {info.PairingKey} in this upload and no saved {info.TemplateCode} template: import {info.PairingKey}'s worksheet first.");
        }
    }

    private static void BindInUpload(WorksheetImportProposal specification, WorksheetImportProposal worksheet)
    {
        specification.RawMaterial.Pairing = RawMaterialPairingStatus.InUpload;
        specification.RawMaterial.PairedFileName = worksheet.FileName;

        var sections = worksheet.Template.Sections.ToList();
        var names = sections.Select(section => section.Name).ToList();
        var bound = new HashSet<int>();
        var keys = worksheet.Template.Sections.SelectMany(section => section.Fields).Select(field => field.FieldKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = new Dictionary<string, ProposedWorksheetSection>(StringComparer.OrdinalIgnoreCase);

        foreach (var characteristic in specification.SpecificationProposals)
        {
            characteristic.SourceWorksheetTemplateId = null;
            var index = RawMaterialTestNames.Match(characteristic.TestName, characteristic.Analyte, names, bound);
            var key = index < 0 ? null : RawMaterialResultField.Choose(sections[index]);
            if (key is not null)
            {
                bound.Add(index);
                characteristic.SourceFieldKey = key;
                continue;
            }

            characteristic.SourceFieldKey = AddForSpecification(specification, worksheet, characteristic, keys, added);
        }
    }

    /// <summary>Locked decision 2: a Result (Entry, LongText) the characteristic binds to, plus "Attach print out".</summary>
    private static string AddForSpecification(
        WorksheetImportProposal specification, WorksheetImportProposal worksheet, SpecificationCharacteristicProposal characteristic,
        HashSet<string> keys, Dictionary<string, ProposedWorksheetSection> added)
    {
        var prefix = ImportText.SnakeKey(characteristic.TestName, 30);
        var reason = $"Added for the Specification test '{Describe(characteristic)}' from '{specification.FileName}', "
                     + "which has no section on the worksheet; the reviewer may remove it";

        if (!added.TryGetValue(characteristic.TestName, out var section))
        {
            section = new ProposedWorksheetSection { Name = characteristic.TestName, Order = worksheet.Template.Sections.Count + 1 };
            worksheet.Template.Sections.Add(section);
            added[characteristic.TestName] = section;
            AddField(worksheet, section, keys, new ProposedWorksheetField
            {
                FieldKey = $"{prefix}_attach_print_out", Label = "Attach print out",
                Type = WorksheetFieldType.FileUpload, Mode = WorksheetFieldMode.Entry
            }, reason);

            var message = $"Section '{section.Name}' was added for the Specification test '{Describe(characteristic)}' "
                          + $"({specification.FileName}); remove it if the test is not recorded on this worksheet.";
            Flag(worksheet, WorksheetImportFlagCodes.AddedForSpecification, message);
            Flag(specification, WorksheetImportFlagCodes.AddedForSpecification, message);
        }

        // One Result per characteristic, ahead of the section's single attachment.
        var result = new ProposedWorksheetField
        {
            FieldKey = characteristic.Analyte is null ? $"{prefix}_result" : $"{prefix}_{ImportText.SnakeKey(characteristic.Analyte, 40)}",
            Label = characteristic.Analyte ?? "Result",
            Type = WorksheetFieldType.LongText,
            Mode = WorksheetFieldMode.Entry
        };
        AddField(worksheet, section, keys, result, reason, section.Fields.Count - 1);
        characteristic.Confidence = ImportConfidence.Medium;
        return result.FieldKey;
    }

    private static void AddField(
        WorksheetImportProposal worksheet, ProposedWorksheetSection section, HashSet<string> keys, ProposedWorksheetField field,
        string reason, int? position = null)
    {
        var key = field.FieldKey.Length > 90 ? field.FieldKey[..90].TrimEnd('_') : field.FieldKey;
        var unique = key;
        for (var suffix = 2; !keys.Add(unique); suffix++)
            unique = $"{key}_{suffix}";
        field.FieldKey = unique;

        section.Fields.Insert(Math.Clamp(position ?? section.Fields.Count, 0, section.Fields.Count), field);
        for (var index = 0; index < section.Fields.Count; index++)
            section.Fields[index].Order = index + 1;

        worksheet.FieldProvenance.Add(new ImportFieldProvenance
        {
            FieldKey = unique, Confidence = ImportConfidence.Medium, Reason = reason
        });
    }

    /// <summary>A saved RM-NNN template is never modified: an unmatched test is flagged for a template revision.</summary>
    private static void BindToSaved(WorksheetImportProposal specification, CatalogTemplate saved)
    {
        var info = specification.RawMaterial;
        info.Pairing = RawMaterialPairingStatus.ExistingTemplate;
        info.PairedTemplateId = saved.Id;
        info.PairedTemplateCode = saved.Code;

        var sections = saved.Sections ?? [];
        var names = sections.Select(section => section.Name).ToList();
        var bound = new HashSet<int>();

        foreach (var characteristic in specification.SpecificationProposals)
        {
            characteristic.SourceWorksheetTemplateId = saved.Id;
            var index = RawMaterialTestNames.Match(characteristic.TestName, characteristic.Analyte, names, bound);
            characteristic.SourceFieldKey = index < 0 ? null : sections[index].ResultFieldKey;
            if (characteristic.SourceFieldKey is not null)
            {
                bound.Add(index);
                continue;
            }

            characteristic.Confidence = ImportConfidence.Low;
            Flag(specification, WorksheetImportFlagCodes.FieldNotOnTemplate,
                $"'{Describe(characteristic)}' matches no section of the saved template {saved.Code}, which is not modified; "
                + "revise the template (add a section for it), then bind the characteristic.");
        }
    }

    private static string Describe(SpecificationCharacteristicProposal characteristic) =>
        characteristic.Analyte is null ? characteristic.TestName : $"{characteristic.TestName} – {characteristic.Analyte}";

    private static void Flag(WorksheetImportProposal proposal, string code, string message) =>
        proposal.Flags.Add(new WorksheetImportFlag { Code = code, Message = message });
}
