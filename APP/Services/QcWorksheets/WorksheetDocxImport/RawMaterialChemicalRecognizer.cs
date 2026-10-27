using System.Text.RegularExpressions;
using APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;
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

    // "pH [4.5 – 6.0]": a limit printed in the test name.
    [GeneratedRegex(@"\s*\[(?<limit>[^\]]*\d[^\]]*)\]\s*$")]
    private static partial Regex LimitRegex();

    public void Recognize(DocxDocument document, ImportProposalBuilder builder)
    {
        var template = builder.Template;
        template.Category = WorksheetCategory.Chemical;
        template.Department = "Quality Control";

        var info = RawMaterialHeader.ReadWorksheet(builder.Proposal.FileName, document, builder);
        builder.Proposal.RawMaterial = info;
        template.Code = info.TemplateCode;
        template.Name = info.MaterialName is null ? "Raw Material Analytical Worksheet" : $"Raw Material Analytical Worksheet – {info.MaterialName}";

        var bodies = new List<RawMaterialSectionBody>();
        foreach (var block in document.Blocks.Where(block => block.Table is not null))
            ReadTable(block, builder, bodies);
        ResolveReferences(bodies);

        if (template.Sections.Count == 0)
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, "No numbered test rows were found.");
        builder.Complete();
    }

    private static void ReadTable(DocxBlock block, ImportProposalBuilder builder, List<RawMaterialSectionBody> bodies)
    {
        var table = block.Table;
        var bodyColumn = table.ColumnCount - 1;
        RawMaterialSectionBody body = null;
        ImportSourceLocation lastLocation = null;

        // A sheet whose number column is empty throughout ("244 - Potassium Iodide"): title and
        // content rows alternate, so a one-line cell after a content row is the next test's name.
        var numbered = bodyColumn > 0 && Enumerable.Range(0, table.Rows.Count)
            .Any(row => table.Cell(row, 0) is { IsContinuation: false } cell && NumberRegex().IsMatch(cell.Text));

        for (var row = 0; row < table.Rows.Count; row++)
        {
            var cell = table.Cell(row, bodyColumn);
            if (cell is null || cell.IsContinuation)
                continue;

            var text = cell.Text;
            var canonical = ImportText.Canonical(text);
            if (canonical is "testobservation" or "testobservations" or "testsobservations" or "testsobservation" || canonical.Contains("testobservations"))
                continue;

            // "Analysed by: checked by: Date:" — review and approval cover the sign-off.
            if (canonical.StartsWith("analysedby") || canonical.StartsWith("analyzedby"))
                break;

            var number = table.Cell(row, 0) is { IsContinuation: false } first && bodyColumn > 0 ? first.Text : string.Empty;
            var isTitle = NumberRegex().IsMatch(number) || IsUnnumberedTitle(cell, body, numbered);

            var location = ImportProposalBuilder.At(block, row, bodyColumn);
            if (isTitle)
            {
                body?.Complete(lastLocation);
                body = OpenSection(text, location, builder);
                bodies.Add(body);
            }
            else if (body is not null)
                body.ReadCell(block, row, cell);
            else if (!ImportText.IsBlank(text))
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, $"'{text}' comes before the first numbered test.", location);

            lastLocation = location;
        }

        body?.Complete(lastLocation);
    }

    /// <summary>
    /// A test printed without a number: a one-line cell that follows a content row and either reads
    /// as a sub-test ("Identity Test B: …", "… Instrument ID:"), names a defined test ("Assay –
    /// Titration", "Heavy Metals"), or sits in a sheet that numbers nothing. The first one-line cell
    /// of an un-numbered sheet opens its first test.
    /// </summary>
    private static bool IsUnnumberedTitle(DocxCell cell, RawMaterialSectionBody body, bool numbered)
    {
        if (cell.Lines.Count != 1 || cell.NestedTables.Count > 0)
            return false;

        var line = cell.Lines[0];
        if (ImportText.HasLeader(line) || line.Contains('=') || RawMaterialLines.InstrumentRegex().Match(line) is { Success: true, Index: 0 })
            return false;

        if (body is null)
            return !numbered;
        if (!body.HasBodyRows)
            return false;
        if (UnnumberedTitleRegex().IsMatch(line) || !numbered)
            return true;

        // "Water:" under Solubility is a solvent, not the water determination.
        var name = SectionName(line);
        return !body.IsSolubility && !name.Contains(':') && RawMaterialTestDefinitions.ByName(name) is not null;
    }

    /// <summary>"Sulfated Ash Instrument ID:" → "Sulfated Ash".</summary>
    private static string SectionName(string title)
    {
        var instrument = RawMaterialLines.InstrumentRegex().Match(title);
        var name = instrument.Success ? title[..instrument.Index] : title;
        name = ImportText.Normalize(name).TrimEnd(':', ';', '.', '–', '-', ' ');
        return name.Length == 0 ? ImportText.Normalize(title) : name;
    }

    /// <summary>
    /// "Sulfated Ash Instrument ID:" → section "Sulfated Ash" plus its Instrument field;
    /// "pH [4.5 – 6.0]" → section "pH" plus the printed limit as a Constant.
    /// </summary>
    private static RawMaterialSectionBody OpenSection(string title, ImportSourceLocation location, ImportProposalBuilder builder)
    {
        var instruments = RawMaterialLines.InstrumentRegex().Matches(title).ToList();
        var name = SectionName(title);
        var limit = LimitRegex().Match(name);
        if (limit.Success && ImportText.Canonical(name[..limit.Index]).Length > 0)
            name = name[..limit.Index].TrimEnd(':', ' ', '–', '-');

        builder.Section(name);
        var prefix = ImportText.SnakeKey(name, 30);
        if (limit.Success && name.Length < SectionName(title).Length)
            builder.AddField(new ProposedWorksheetField
            {
                FieldKey = $"{prefix}_limit", Label = "Limit", Type = WorksheetFieldType.ShortText, Mode = WorksheetFieldMode.Constant,
                ConstantValue = ImportText.Normalize(limit.Groups["limit"].Value)
            }, location, ImportConfidence.High, "Limit printed in the test name, read from the sheet");

        var body = new RawMaterialSectionBody(builder, name, prefix, location);
        foreach (var match in instruments)
            body.AddInstrument(match.Groups["label"].Value, match.Groups["code"].Success ? match.Groups["code"].Value : null, location);
        return body;
    }

    /// <summary>
    /// "(100 – LOD)" / "(100 – Water)" in an assay is this worksheet's own Loss on Drying or Water
    /// result (brief 12). When the worksheet has no such test, the assay gets an entry for it.
    /// </summary>
    private static void ResolveReferences(List<RawMaterialSectionBody> bodies)
    {
        foreach (var body in bodies.Where(body => body.References.Count > 0))
        {
            foreach (var name in body.References.ToList())
            {
                var (definition, label) = name switch
                {
                    "lod" => ("loss_on_drying", "Loss on drying (%)"),
                    "water" => ("water", "Water (%)"),
                    "loi" => ("loss_on_ignition", "Loss on ignition (%)"),
                    _ => (null, name)
                };
                var target = bodies.FirstOrDefault(other => other != body && other.Definition?.Key == definition);
                body.ResolveReference(name, label, target is null ? null : RawMaterialResultField.Choose(target.Section));
            }
        }
    }
}
