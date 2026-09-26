using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// The raw-material chemical worksheet ("RAW MATERIAL ANALYTICAL WORKSHEET", brief 09): a
/// No. | TEST &amp; OBSERVATIONS table whose numbered title rows ("1. Description / Appearance",
/// "5. Sulfated Ash Instrument ID:") each open a section, followed by one or more body rows of
/// "Label: ____" blanks.
/// <para>
/// The running header is run data (batch, quantities, dates, sampler, issuer, supplier) and is
/// dropped; only the material name names the template, whose code is RM-NNN. The sheet prints no
/// specifications: those come from the paired Specification document.
/// </para>
/// </summary>
public sealed partial class RawMaterialChemicalRecognizer : IArdFamilyRecognizer
{
    public ArdFamily Family => ArdFamily.RawMaterialChemical;

    [GeneratedRegex(@"^\d+\s*\.?$")]
    private static partial Regex NumberRegex();

    // Sub-tests printed without a number under one ("Identity Test B: Reactions of Bromides",
    // "Specific Optical Rotation Instrument ID:"). Body rows never start like this.
    [GeneratedRegex(@"^(?:Identity|Identification|Specific)\b|(?:Instrument|Balance|Equipment)\s*ID\.?\s*:?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex UnnumberedTitleRegex();

    public void Recognize(DocxDocument document, ImportProposalBuilder builder)
    {
        var template = builder.Template;
        template.Category = WorksheetCategory.Chemical;
        template.Department = "Quality Control";

        var info = RawMaterialHeader.ReadWorksheet(builder.Proposal.FileName, document, builder);
        builder.Proposal.RawMaterial = info;
        template.Code = info.TemplateCode;
        template.Name = info.MaterialName is null ? "Raw Material Analytical Worksheet" : $"Raw Material Analytical Worksheet – {info.MaterialName}";

        foreach (var block in document.Blocks.Where(block => block.Table is not null))
            ReadTable(block, builder);

        if (template.Sections.Count == 0)
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, "No numbered test rows were found.");
        builder.Complete();
    }

    private static void ReadTable(DocxBlock block, ImportProposalBuilder builder)
    {
        var table = block.Table;
        var bodyColumn = table.ColumnCount - 1;
        RawMaterialSectionBody body = null;
        var bodyRows = 0;
        ImportSourceLocation lastLocation = null;

        for (var row = 0; row < table.Rows.Count; row++)
        {
            var cell = table.Cell(row, bodyColumn);
            if (cell is null || cell.IsContinuation)
                continue;

            var text = cell.Text;
            var canonical = ImportText.Canonical(text);
            if (canonical.Contains("testobservations") || canonical.StartsWith("testsobservations"))
                continue;

            // "Analysed by: checked by: Date:" — review and approval cover the sign-off.
            if (canonical.StartsWith("analysedby") || canonical.StartsWith("analyzedby"))
                break;

            var number = table.Cell(row, 0) is { IsContinuation: false } first && bodyColumn > 0 ? first.Text : string.Empty;
            var isTitle = NumberRegex().IsMatch(number)
                          || (body is not null && bodyRows > 0 && cell.Lines.Count == 1 && cell.NestedTables.Count == 0
                              && UnnumberedTitleRegex().IsMatch(cell.Lines[0])
                              && RawMaterialLines.InstrumentRegex().Match(cell.Lines[0]) is not { Success: true, Index: 0 });

            var location = ImportProposalBuilder.At(block, row, bodyColumn);
            if (isTitle)
            {
                body?.Complete(lastLocation);
                body = OpenSection(text, location, builder);
                bodyRows = 0;
            }
            else if (body is not null)
            {
                body.ReadCell(block, row, cell);
                bodyRows++;
            }
            else if (!ImportText.IsBlank(text))
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, $"'{text}' comes before the first numbered test.", location);

            lastLocation = location;
        }

        body?.Complete(lastLocation);
    }

    /// <summary>"Sulfated Ash Instrument ID:" → section "Sulfated Ash" plus its Instrument field.</summary>
    private static RawMaterialSectionBody OpenSection(string title, ImportSourceLocation location, ImportProposalBuilder builder)
    {
        var instruments = RawMaterialLines.InstrumentRegex().Matches(title).ToList();
        var name = instruments.Count == 0 ? title : title[..instruments[0].Index];
        name = ImportText.Normalize(name).TrimEnd(':', ';', '.', '–', '-', ' ');
        if (name.Length == 0)
            name = ImportText.Normalize(title);

        builder.Section(name);
        var body = new RawMaterialSectionBody(builder, name, ImportText.SnakeKey(name, 30));
        foreach (var match in instruments)
            body.AddInstrument(match.Groups["label"].Value, match.Groups["code"].Success ? match.Groups["code"].Value : null, location);
        return body;
    }
}
