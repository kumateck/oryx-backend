using System.Globalization;
using System.Security.Cryptography;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DOMAIN.Entities.StpRevisions;

namespace APP.Services.StpRevisions;

/// <summary>A signature line on the exported STP: who prepared, checked or approved it, and when.</summary>
public sealed record StpSignature(string Name, string Designation, DateTime? At);

/// <summary>
/// Writes a revision as the client's STP Word file: the running-header table (STANDARD TEST PROCEDURE; STP No., title,
/// page; Revision No., Supersedes, Area; Reference; effective, review and issue dates), then the body as the two-column
/// table the client uses (number | text) with 1.0 to 5.0 and the 5.N tests and 5.N.M steps, and a footer with
/// Prepared / Checked / Approved by. The import reads this file back to the same content.
/// </summary>
public static class StpDocxExport
{
    private const string Font = "Arial";
    private const int Width = 9638; // A4 text width in twentieths of a point with 2 cm margins.

    public static (byte[] Content, string Sha256, string FileName) Write(StpRevision revision, StpSignature prepared,
        StpSignature checkedBy, StpSignature approved, bool draft)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var header = main.AddNewPart<HeaderPart>();
            header.Header = new Header(HeaderTable(revision, draft), new Paragraph());
            var footer = main.AddNewPart<FooterPart>();
            footer.Footer = new Footer(FooterTable(prepared, checkedBy, approved));
            var section = new SectionProperties(
                new HeaderReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(header) },
                new FooterReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(footer) },
                new PageSize { Width = 11906, Height = 16838 },
                new PageMargin { Top = 1134, Bottom = 1134, Left = 1134, Right = 1134, Header = 567, Footer = 567 });
            main.Document = new Document(new Body(BodyTable(revision), new Paragraph(), section));
            main.Document.Save();
        }
        var bytes = stream.ToArray();
        var safe = string.Concat((revision.StpNumber ?? "STP").Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || ch == '/' ? '-' : ch));
        return (bytes, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            $"{safe} Rev {revision.RevisionNumber:00}{(draft ? " DRAFT" : "")}.docx");
    }

    // ---- running header -------------------------------------------------------------------------------------

    private static Table HeaderTable(StpRevision revision, bool draft)
    {
        var table = Grid(2600, 4438, 2600);
        table.Append(Row(Cell(3, Merge.None, Bold("STANDARD TEST PROCEDURE" + (draft ? " (DRAFT, NOT APPROVED)" : ""), center: true))));
        table.Append(Row(
            Cell(1, Merge.None, Line($"STP No.: {revision.StpNumber}")),
            Cell(1, Merge.Restart, Bold(revision.Title ?? "", center: true)),
            Cell(1, Merge.None, PageLine())));
        table.Append(Row(
            Cell(1, Merge.None, Line($"Revision No.: {revision.RevisionNumber:00}")),
            Cell(1, Merge.Continue, new Paragraph()),
            Cell(1, Merge.None, Line($"Effective Date: {Date(revision.EffectiveDate)}"))));
        table.Append(Row(
            Cell(1, Merge.None, Line($"Supersedes: {(revision.SupersedesRevision is { } before ? before.ToString("00", CultureInfo.InvariantCulture) : "")}")),
            Cell(1, Merge.Continue, new Paragraph()),
            Cell(1, Merge.None, Line($"Review Date: {Date(revision.ReviewDate)}"))));
        table.Append(Row(
            Cell(1, Merge.None, Line($"Area: {revision.Area}")),
            Cell(1, Merge.None, Line($"Reference: {revision.Reference}")),
            Cell(1, Merge.None, Line($"Issue Date: {Date(revision.IssueDate)}"))));
        return table;
    }

    private static Paragraph PageLine() => new(
        new Run(Props(false), new Text("Page ") { Space = SpaceProcessingModeValues.Preserve }),
        new SimpleField(new Run(Props(false), new Text("1"))) { Instruction = " PAGE " },
        new Run(Props(false), new Text(" of ") { Space = SpaceProcessingModeValues.Preserve }),
        new SimpleField(new Run(Props(false), new Text("1"))) { Instruction = " NUMPAGES " });

    // ---- body -----------------------------------------------------------------------------------------------

    private static readonly (StpSectionKind Kind, string Number, string Heading)[] Numbered =
    [
        (StpSectionKind.Purpose, "1.0", "Purpose :"), (StpSectionKind.Scope, "2.0", "Scope:"),
        (StpSectionKind.Responsibility, "3.0", "Responsibility:"), (StpSectionKind.Accountability, "4.0", "Accountability:"),
    ];

    private static Table BodyTable(StpRevision revision)
    {
        var table = Grid(1100, Width - 1100);
        string SectionText(StpSectionKind kind) => revision.Sections.FirstOrDefault(item => item.Kind == kind)?.Text;
        foreach (var (kind, number, heading) in Numbered)
        {
            table.Append(BodyRow(number, [Bold(heading)]));
            table.Append(BodyRow(null, Lines(SectionText(kind))));
            table.Append(BodyRow(null, [new Paragraph()]));
        }
        table.Append(BodyRow("5.0", [Bold("Procedure:")]));
        if (SectionText(StpSectionKind.Reagents) is { Length: > 0 } reagents)
            table.Append(BodyRow(null, [Bold("Reagents"), .. Lines(reagents)]));
        if (SectionText(StpSectionKind.ProcedureNotes) is { Length: > 0 } notes)
        {
            table.Append(BodyRow(null, [new Paragraph()]));
            table.Append(BodyRow(null, Lines(notes)));
        }
        foreach (var test in revision.Tests.OrderBy(item => item.Order))
        {
            table.Append(BodyRow(null, [new Paragraph()]));
            table.Append(BodyRow(test.Number, [Bold($"{test.Title}:")]));
            foreach (var step in test.Steps.OrderBy(item => item.Order))
            {
                var paragraphs = new List<Paragraph>();
                if (step.Kind != StpStepKind.Step && !string.IsNullOrWhiteSpace(step.Heading)) paragraphs.Add(Bold($"{step.Heading}:"));
                paragraphs.AddRange(Lines(step.Text));
                // A reference the text does not already name is printed as its own line.
                if (!string.IsNullOrWhiteSpace(step.ReferenceCode) && step.Text?.Contains(step.ReferenceCode, StringComparison.Ordinal) != true)
                    paragraphs.Add(Line($"Refer to {step.ReferenceCode}"));
                table.Append(BodyRow(step.Kind == StpStepKind.Step ? step.Number : null, paragraphs.Count > 0 ? paragraphs : [new Paragraph()]));
            }
        }
        return table;
    }

    private static TableRow BodyRow(string number, IEnumerable<Paragraph> text) =>
        Row(new TableCell(new TableCellProperties(new TableCellWidth { Width = "1100", Type = TableWidthUnitValues.Dxa }),
                string.IsNullOrWhiteSpace(number) ? new Paragraph() : Line(number)),
            new TableCell(new OpenXmlElement[] { new TableCellProperties(new TableCellWidth { Width = (Width - 1100).ToString(CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa }) }
                .Concat(text)));

    private static List<Paragraph> Lines(string text) =>
        string.IsNullOrWhiteSpace(text) ? [new Paragraph()]
            : text.Replace("\r\n", "\n").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).Select(Line).ToList();

    // ---- footer ---------------------------------------------------------------------------------------------

    private static Table FooterTable(StpSignature prepared, StpSignature checkedBy, StpSignature approved)
    {
        var table = Grid(Width / 3, Width / 3, Width - 2 * (Width / 3));
        table.Append(Row(Cell(1, Merge.None, Bold("Prepared by")), Cell(1, Merge.None, Bold("Checked by")), Cell(1, Merge.None, Bold("Approved by"))));
        table.Append(Row(Signed(prepared), Signed(checkedBy), Signed(approved)));
        return table;
    }

    private static TableCell Signed(StpSignature person) => Cell(1, Merge.None,
        Line(person?.Name ?? ""), Line(person?.Designation ?? ""), Line(person?.At is { } at ? $"Date: {Date(at)}" : "Date:"));

    // ---- primitives -----------------------------------------------------------------------------------------

    private enum Merge { None, Restart, Continue }

    private static Table Grid(params int[] widths)
    {
        var borders = new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4 }, new BottomBorder { Val = BorderValues.Single, Size = 4 },
            new LeftBorder { Val = BorderValues.Single, Size = 4 }, new RightBorder { Val = BorderValues.Single, Size = 4 },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 }, new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 });
        var table = new Table(new TableProperties(new TableWidth { Width = widths.Sum().ToString(CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa },
            borders, new TableLayout { Type = TableLayoutValues.Fixed }));
        table.Append(new TableGrid(widths.Select(width => new GridColumn { Width = width.ToString(CultureInfo.InvariantCulture) })));
        return table;
    }

    private static TableRow Row(params TableCell[] cells) => new(cells);

    private static TableCell Cell(int span, Merge merge, params Paragraph[] paragraphs)
    {
        var properties = new TableCellProperties();
        if (span > 1) properties.Append(new GridSpan { Val = span });
        if (merge == Merge.Restart) properties.Append(new VerticalMerge { Val = MergedCellValues.Restart });
        if (merge == Merge.Continue) properties.Append(new VerticalMerge());
        properties.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        return new TableCell(new OpenXmlElement[] { properties }.Concat(paragraphs.Length > 0 ? paragraphs : [new Paragraph()]));
    }

    private static RunProperties Props(bool bold)
    {
        var properties = new RunProperties(new RunFonts { Ascii = Font, HighAnsi = Font, ComplexScript = Font });
        if (bold) properties.Append(new Bold());
        properties.Append(new FontSize { Val = "22" });
        return properties;
    }

    private static Paragraph Line(string text) => new(new Run(Props(false), new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));

    private static Paragraph Bold(string text, bool center = false)
    {
        var paragraph = new Paragraph(new Run(Props(true), new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
        if (center) paragraph.PrependChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        return paragraph;
    }

    private static string Date(DateTime? value) => value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
}
