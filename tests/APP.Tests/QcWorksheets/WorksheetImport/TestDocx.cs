using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Builds small synthetic .docx files in memory — a few cells, no real names — so the reader
/// and every primitive are tested against genuine OpenXml (gridSpan, vMerge, header parts).
/// </summary>
internal static class TestDocx
{
    public static MemoryStream Create(IEnumerable<OpenXmlElement> body, params string[] headers)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(body));

            foreach (var text in headers)
            {
                var part = main.AddNewPart<HeaderPart>();
                part.Header = new Header(new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
            }

            main.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    public static DocxDocument Read(IEnumerable<OpenXmlElement> body, params string[] headers)
    {
        using var stream = Create(body, headers);
        return DocxDocumentReader.Read(stream);
    }

    /// <summary>A paragraph; "\t" in the text becomes a real tab element.</summary>
    public static Paragraph P(string text, string? style = null)
    {
        var paragraph = new Paragraph();
        if (style is not null)
            paragraph.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = style });

        var parts = text.Split('\t');
        for (var index = 0; index < parts.Length; index++)
        {
            if (index > 0)
                paragraph.AppendChild(new Run(new TabChar()));
            paragraph.AppendChild(new Run(new Text(parts[index]) { Space = SpaceProcessingModeValues.Preserve }));
        }

        return paragraph;
    }

    public static Table T(params TableRow[] rows) => new(rows);

    public static TableRow R(params TableCell[] cells) => new(cells);

    /// <summary>A cell; " | " in the text splits it into separate paragraphs.</summary>
    public static TableCell C(string text = "", int span = 1, VMerge merge = VMerge.None)
    {
        var properties = new TableCellProperties();
        if (span > 1)
            properties.AppendChild(new GridSpan { Val = span });
        if (merge == VMerge.Restart)
            properties.AppendChild(new VerticalMerge { Val = MergedCellValues.Restart });
        else if (merge == VMerge.Continue)
            properties.AppendChild(new VerticalMerge());

        var cell = new TableCell(properties);
        foreach (var part in text.Split(" | "))
            cell.AppendChild(P(part));
        return cell;
    }

    /// <summary>Plain one-row-per-array table.</summary>
    public static Table Grid(params string[][] rows) =>
        T(rows.Select(row => R(row.Select(text => C(text)).ToArray())).ToArray());

    public static DocxTable Table(params string[][] rows) => Read([Grid(rows)]).Tables.Single();
}

internal enum VMerge
{
    None,
    Restart,
    Continue
}
