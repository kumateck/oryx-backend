using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Families whose files all propose one template (EM: every area sheet proposes
/// "Environmental Monitoring – Airborne Viables"). A batch rule, like the media supersession:
/// <list type="number">
/// <item>Already saved (non-superseded, by code through the catalog) → no file proposes it; each
/// carries <see cref="WorksheetImportFlagCodes.SharedTemplateExists"/>, and its specification
/// proposals point at the saved template's field.</item>
/// <item>Several files in one upload → the first carries the template, with the union of every
/// file's instruments (keyed by equipment code); the others carry only their points, groups and
/// specifications, flagged <see cref="WorksheetImportFlagCodes.SharedTemplateInBatch"/>.</item>
/// </list>
/// </summary>
public static class SharedTemplates
{
    /// <summary>Flags about template fields; they go with the template when a file stops carrying one.</summary>
    private static readonly HashSet<string> TemplateFieldFlags =
    [
        WorksheetImportFlagCodes.UnmatchedEquipment, WorksheetImportFlagCodes.UnmatchedReagent,
        WorksheetImportFlagCodes.MediaTemplateMissing, WorksheetImportFlagCodes.IncompleteValue
    ];

    public static void Apply(IReadOnlyList<WorksheetImportProposal> batch, IWorksheetImportCatalog catalog)
    {
        catalog ??= InMemoryWorksheetImportCatalog.Empty;

        foreach (var group in batch.Where(proposal => proposal.SharedTemplate is not null && proposal.Template is not null)
                     .GroupBy(proposal => proposal.SharedTemplate.Key))
        {
            var files = group.ToList();
            var saved = catalog.FindSharedTemplate(group.Key);

            if (saved is not null)
            {
                foreach (var file in files)
                    PointAtSaved(file, saved);
                continue;
            }

            var carrier = files[0];
            foreach (var file in files.Skip(1))
            {
                MergeInstruments(carrier, file);
                ReportDifferences(carrier, file);
                StopCarrying(file, WorksheetImportFlagCodes.SharedTemplateInBatch,
                    $"The shared template '{carrier.Template.Name}' is proposed once, with '{carrier.FileName}'. "
                    + "This file adds its sampling points, groups and specifications.");
                file.SharedTemplate.CarriedBy = carrier.FileName;
            }
        }
    }

    private static void PointAtSaved(WorksheetImportProposal file, CatalogTemplate saved)
    {
        var fieldKeys = (saved.FieldKeys ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var specification in file.SpecificationProposals)
        {
            specification.SourceWorksheetTemplateId = saved.Id;
            if (specification.SourceFieldKey is not null && !fieldKeys.Contains(specification.SourceFieldKey))
            {
                file.Flags.Add(new WorksheetImportFlag
                {
                    Code = WorksheetImportFlagCodes.UnrecognizedContent,
                    Message = $"The saved template {saved.Code} has no field '{specification.SourceFieldKey}'; "
                              + $"bind the '{specification.GroupName ?? specification.TestName}' specification by hand."
                });
                specification.SourceFieldKey = null;
                specification.Confidence = ImportConfidence.Low;
            }
        }

        file.SharedTemplate.CarriedBy = null;
        file.SharedTemplate.ExistingTemplateId = saved.Id;
        file.SharedTemplate.ExistingTemplateCode = saved.Code;
        StopCarrying(file, WorksheetImportFlagCodes.SharedTemplateExists,
            $"The template {saved.Code} ('{saved.Name}') is already saved; only this file's sampling points, groups and "
            + "specifications are proposed, bound to it.");
    }

    /// <summary>Adds the file's instruments the carrier lacks, so no area loses one.</summary>
    private static void MergeInstruments(WorksheetImportProposal carrier, WorksheetImportProposal file)
    {
        var carried = Fields(carrier).Select(field => field.FieldKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var section = carrier.Template.Sections.FirstOrDefault(item => item.Name == "Equipment");

        foreach (var instrument in Fields(file).Where(field => field.Type == WorksheetFieldType.Instrument && !carried.Contains(field.FieldKey)))
        {
            if (section is null)
            {
                section = new ProposedWorksheetSection { Name = "Equipment", Order = carrier.Template.Sections.Count + 1 };
                carrier.Template.Sections.Add(section);
            }

            instrument.Order = section.Fields.Count + 1;
            section.Fields.Add(instrument);
            carried.Add(instrument.FieldKey);
            carrier.EquipmentMatches.AddRange(file.EquipmentMatches.Where(match => match.FieldKey == instrument.FieldKey));
            carrier.FieldProvenance.AddRange(file.FieldProvenance.Where(item => item.FieldKey == instrument.FieldKey)
                .Select(item => new ImportFieldProvenance
                {
                    FieldKey = item.FieldKey, ColumnKey = item.ColumnKey, Confidence = item.Confidence, Location = item.Location,
                    Reason = $"{item.Reason} (from '{file.FileName}')"
                }));
            if (file.EquipmentMatches.FirstOrDefault(match => match.FieldKey == instrument.FieldKey) is { EquipmentId: null } unmatched)
                carrier.Flags.Add(new WorksheetImportFlag
                {
                    Code = WorksheetImportFlagCodes.UnmatchedEquipment,
                    Message = $"'{unmatched.PrintedName}' ({unmatched.PrintedCode}) is not in the QC equipment register (from '{file.FileName}')."
                });
        }
    }

    /// <summary>Anything beyond instruments that differs from the carrier is surfaced, never silently dropped.</summary>
    private static void ReportDifferences(WorksheetImportProposal carrier, WorksheetImportProposal file)
    {
        static HashSet<string> Shape(WorksheetImportProposal proposal) => Fields(proposal)
            .Where(field => field.Type != WorksheetFieldType.Instrument)
            .Select(field => $"{field.FieldKey}|{field.Type}|{field.Mode}|{field.Unit}|{field.ConstantValue}|{string.Join("/", field.Options ?? [])}")
            .ToHashSet();

        var differing = Shape(file).Except(Shape(carrier)).Select(item => item.Split('|')[0]).Distinct().ToList();
        if (differing.Count > 0)
            file.Flags.Add(new WorksheetImportFlag
            {
                Code = WorksheetImportFlagCodes.UnrecognizedContent,
                Message = $"This sheet's {string.Join(", ", differing)} differ from the shared template proposed with "
                          + $"'{carrier.FileName}'; review before relying on the shared template."
            });
    }

    private static void StopCarrying(WorksheetImportProposal file, string code, string message)
    {
        file.Template = null;
        file.FieldProvenance.Clear();
        file.EquipmentMatches.Clear();
        file.ReagentMatches.Clear();
        file.Flags.RemoveAll(flag => TemplateFieldFlags.Contains(flag.Code));
        file.Flags.Insert(0, new WorksheetImportFlag { Code = code, Message = message });
    }

    private static IEnumerable<ProposedWorksheetField> Fields(WorksheetImportProposal proposal) =>
        proposal.Template?.Sections.SelectMany(section => section.Fields) ?? [];
}
