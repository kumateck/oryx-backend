using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public enum CultureMediaFormat
{
    Unknown,
    New,
    Superseded
}

/// <summary>Tells the two media form revisions apart by the labels of their metadata table.</summary>
public static class CultureMediaFormatDetector
{
    private static readonly HashSet<string> OldMarkers = ["lotno", "dateofmfg", "dateofexpiry", "datereceived", "dateopened"];
    private static readonly HashSet<string> NewMarkers = ["issueno", "issuedate", "issuedby", "formatno", "mediumcode", "serialno"];

    public static CultureMediaFormat Detect(IEnumerable<string> labels)
    {
        var canonical = labels.Select(ImportText.Canonical).ToList();

        // Two old markers, not one: a lone "Lot No." could be a stray label on a new form.
        if (canonical.Count(OldMarkers.Contains) >= 2)
            return CultureMediaFormat.Superseded;

        return canonical.Any(NewMarkers.Contains) ? CultureMediaFormat.New : CultureMediaFormat.Unknown;
    }
}

internal sealed partial class CultureMediaWalker
{
    private void ReadMetadata(DocxBlock block)
    {
        var segments = ParameterTable.ReadLabelledCells(block.Table);
        var format = CultureMediaFormatDetector.Detect(segments.Select(segment => segment.Decision.Label));
        builder.Proposal.FormatVersion = format.ToString();

        if (format == CultureMediaFormat.Superseded)
            builder.Flag(WorksheetImportFlagCodes.SupersededFormat,
                "Older media form (Lot No. / Date of Mfg. / Date of Expiry / Date Received). The newer Batch No. / "
                + "Issue No. form wins: do not save this proposal when the newer sheet for this medium is imported.",
                ImportProposalBuilder.At(block));
        else if (format == CultureMediaFormat.Unknown)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata,
                "Neither the newer (Issue No., Medium Code) nor the older (Lot No., Date of Mfg.) media form markers were found.",
                ImportProposalBuilder.At(block));

        string code = null;
        (DocxCell Cell, ParameterDecision Decision)? batch = null;
        var others = new List<(DocxCell Cell, ParameterDecision Decision)>();

        foreach (var segment in segments)
        {
            switch (segment.Decision.Key)
            {
                case "culture_medium_name":
                    _mediumName = segment.Decision.Kind == ParameterKind.Constant ? segment.Decision.ConstantValue : null;
                    break;
                case "medium_code":
                    code = segment.Decision.Kind == ParameterKind.Constant ? segment.Decision.ConstantValue : null;
                    break;
                case "batch_no":
                    batch ??= segment;
                    break;
                default:
                    others.Add(segment);
                    break;
            }
        }

        builder.Proposal.Medium = CultureMediaSupersession.Identify(_mediumName, code);
        builder.Section("Medium details");
        var location = ImportProposalBuilder.At(block);

        if (_mediumName is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "The culture medium name is blank.", location);
        else
            builder.AddField(Constant("medium_name", "Culture medium", _mediumName), location, ImportConfidence.High,
                "Culture Medium Name: identifies the template");

        if (code is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "The medium code is blank; the template code is derived from the name.", location);
        else
            builder.AddField(Constant("medium_code", "Medium code", code), location, ImportConfidence.High, "Medium Code");

        // The dehydrated medium itself is a Reagent entry (catalog id + batch + expiry). Its batch
        // number is what product and water sheets resolve their ReferencedResult against.
        ReagentTable.AddReagent(builder, _mediumName ?? "culture medium", CultureMediaKeys.Medium,
            $"Dehydrated medium{(_mediumName is null ? string.Empty : $" ({_mediumName})")} — batch no. and expiry",
            batch is null ? location : ImportProposalBuilder.At(block, batch.Value.Cell.Row, batch.Value.Cell.Column));
        if (batch is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No Batch No. label was found; the medium reagent field is still proposed.", location);

        foreach (var (cell, decision) in others)
        {
            // Nothing in the metadata table is a method parameter: an unknown label with a
            // printed value is more likely run data than a constant, so it never becomes one.
            if (decision.Kind == ParameterKind.Constant)
            {
                builder.AddDecision(decision with
                {
                    Kind = ParameterKind.Entry, ConstantValue = null, Confidence = ImportConfidence.Low,
                    Reason = "Unrecognized metadata label; printed value discarded as possible run data",
                    FlagCode = WorksheetImportFlagCodes.SuspectedRunData
                }, ImportProposalBuilder.At(block, cell.Row, cell.Column));
                continue;
            }

            builder.AddDecision(decision, ImportProposalBuilder.At(block, cell.Row, cell.Column));
        }

        builder.Template.Code = code
                                ?? (_mediumName is null ? null : "MQ-" + ImportText.SnakeKey(_mediumName, 40).ToUpperInvariant().Replace('_', '-'));
    }

    private static ProposedWorksheetField Constant(string key, string label, string value) => new()
    {
        FieldKey = key, Label = label, Type = WorksheetFieldType.ShortText, Mode = WorksheetFieldMode.Constant, ConstantValue = value
    };
}
