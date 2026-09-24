using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Reads an ARD worksheet into a <see cref="DocxDocument"/>: ordered heading / paragraph /
/// table blocks plus the running-header text.
/// <para>
/// Corpus facts this is built to (brief 07): no form controls exist, so blanks are inferred
/// from empty cells and leaders; merged cells are heavy, so every table becomes a rectangular
/// grid whose span and merge fillers point at their origin cell; tables split at page breaks
/// are stitched back together; stray bracket paragraphs and "Page x of y" are noise.
/// </para>
/// </summary>
public static class DocxDocumentReader
{
    public static DocxDocument Read(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var mainPart = document.MainDocumentPart
                       ?? throw new InvalidDataException("The document has no readable content.");

        var header = new StringBuilder();
        foreach (var part in mainPart.HeaderParts)
        {
            if (part.Header is null)
                continue;
            foreach (var paragraph in part.Header.Descendants<Paragraph>())
                header.Append(ParagraphText(paragraph)).Append(' ');
        }

        var blocks = new List<DocxBlock>();
        var rawTableOrdinal = 0;
        var body = mainPart.Document?.Body;
        if (body is not null)
            CollectBlocks(body.ChildElements, blocks, ref rawTableOrdinal);

        var stitched = DocxTableStitcher.Stitch(blocks);
        var tableOrdinal = 0;
        for (var index = 0; index < stitched.Count; index++)
        {
            stitched[index].Index = index;
            if (stitched[index].Table is not null)
                stitched[index].Table.Ordinal = tableOrdinal++;
        }

        return new DocxDocument
        {
            HeaderText = ImportText.Clean(ImportText.StripPageNumbers(header.ToString())),
            Blocks = stitched
        };
    }

    private static void CollectBlocks(IEnumerable<OpenXmlElement> elements, List<DocxBlock> blocks, ref int tableOrdinal)
    {
        foreach (var element in elements)
        {
            switch (element)
            {
                case Paragraph paragraph:
                {
                    var text = ImportText.Clean(ParagraphText(paragraph));
                    if (text.Length == 0 || ImportText.IsNoise(text))
                        continue;

                    var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                    blocks.Add(new DocxBlock
                    {
                        Kind = IsHeading(text, style) ? DocxBlockKind.Heading : DocxBlockKind.Paragraph,
                        Text = text,
                        Style = style
                    });
                    break;
                }
                case Table table:
                {
                    var grid = BuildGrid(table, tableOrdinal++);
                    blocks.Add(new DocxBlock { Kind = DocxBlockKind.Table, Table = grid, Text = Describe(grid) });
                    break;
                }
                case SdtBlock sdt when sdt.SdtContentBlock is not null:
                    CollectBlocks(sdt.SdtContentBlock.ChildElements, blocks, ref tableOrdinal);
                    break;
            }
        }
    }

    /// <summary>
    /// Heading = a Heading/Title style, or an all-capitals line such as
    /// "PREVIOUSLY APPROVED BATCH". Codes like "QCD/SOP/044" are all capitals too, so a
    /// heading must also contain a real word.
    /// </summary>
    internal static bool IsHeading(string text, string style)
    {
        if (style is not null && (style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
                                  || style.StartsWith("Title", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (text.Any(char.IsLower) || text.Contains(':'))
            return false;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Trim('.', ',', '-', '(', ')').All(char.IsLetter) && word.Trim('.', ',', '-').Length >= 3)
            .ToList();
        return words.Count >= 2 || (words.Count == 1 && words[0].Length >= 6 && text.Trim().Length == words[0].Length);
    }

    internal static DocxTable BuildGrid(Table table, int sourceOrdinal)
    {
        var rows = new List<List<DocxCell>>();
        var verticalOrigin = new Dictionary<int, (int Row, int Column)>();

        foreach (var tableRow in table.Elements<TableRow>())
        {
            var rowIndex = rows.Count;
            var cells = new List<DocxCell>();
            var column = tableRow.TableRowProperties?.GetFirstChild<GridBefore>()?.Val?.Value ?? 0;
            for (var pad = 0; pad < column; pad++)
                cells.Add(new DocxCell { Row = rowIndex, Column = pad, OriginRow = rowIndex, OriginColumn = pad });

            foreach (var tableCell in tableRow.Elements<TableCell>())
            {
                var properties = tableCell.TableCellProperties;
                var span = Math.Max(1, properties?.GridSpan?.Val?.Value ?? 1);
                var merge = properties?.VerticalMerge;
                var isContinuation = merge is not null
                                     && (merge.Val is null || merge.Val.Value == MergedCellValues.Continue);

                var origin = isContinuation && verticalOrigin.TryGetValue(column, out var above)
                    ? above
                    : (rowIndex, column);

                var text = origin == (rowIndex, column)
                    ? ImportText.Clean(string.Join(" ", tableCell.Elements<Paragraph>().Select(ParagraphText)))
                    : string.Empty;

                for (var offset = 0; offset < span; offset++)
                {
                    var position = column + offset;
                    cells.Add(new DocxCell
                    {
                        Row = rowIndex,
                        Column = position,
                        Text = offset == 0 ? text : string.Empty,
                        OriginRow = origin.Item1,
                        OriginColumn = origin.Item2,
                        IsHorizontalSpan = offset > 0
                    });

                    if (merge is null)
                        verticalOrigin.Remove(position);
                    else if (!isContinuation)
                        verticalOrigin[position] = (rowIndex, column);
                }

                column += span;
            }

            rows.Add(cells);
        }

        var width = rows.Count == 0 ? 0 : rows.Max(row => row.Count);
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            for (var position = rows[rowIndex].Count; position < width; position++)
                rows[rowIndex].Add(new DocxCell { Row = rowIndex, Column = position, OriginRow = rowIndex, OriginColumn = position });
        }

        return new DocxTable(rows.Select(row => (IReadOnlyList<DocxCell>)row).ToList(), [sourceOrdinal]);
    }

    /// <summary>
    /// Paragraph text with tabs and breaks as spaces (InnerText drops them, which fuses
    /// "7.2 ± 0.2" and "Observed:"), no-break hyphens kept, soft hyphens and deletions dropped.
    /// </summary>
    internal static string ParagraphText(OpenXmlElement paragraph)
    {
        var builder = new StringBuilder();
        foreach (var element in paragraph.Descendants())
        {
            switch (element)
            {
                case Text text:
                    builder.Append(text.Text);
                    break;
                case TabChar or Break or CarriageReturn:
                    builder.Append(' ');
                    break;
                case NoBreakHyphen:
                    builder.Append('-');
                    break;
            }
        }

        return builder.ToString();
    }

    internal static string Describe(DocxTable table) =>
        string.Join(" || ", table.Rows.Select((_, row) => string.Join(" | ", table.OriginCells(row).Select(cell => cell.Text))));
}
