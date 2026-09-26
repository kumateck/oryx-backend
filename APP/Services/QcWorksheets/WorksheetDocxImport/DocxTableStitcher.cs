namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Word splits a long table at each page break and repeats its header on the next page, so
/// the EM results list arrives as six tables. Tables that are directly adjacent (only noise
/// between them, which the reader has already dropped), equally wide, and share their first
/// row are one table; the repeated header rows of the later piece are dropped.
/// </summary>
public static class DocxTableStitcher
{
    private const int MaxHeaderRows = 3;

    public static List<DocxBlock> Stitch(IReadOnlyList<DocxBlock> blocks)
    {
        var result = new List<DocxBlock>();

        foreach (var block in blocks)
        {
            var previous = result.Count > 0 ? result[^1] : null;
            if (block.Table is not null && previous?.Table is not null
                && SharedHeaderRows(previous.Table, block.Table) is var shared and > 0)
            {
                var merged = Append(previous.Table, block.Table, shared);
                result[^1] = new DocxBlock
                {
                    Kind = DocxBlockKind.Table,
                    Table = merged,
                    Text = DocxDocumentReader.Describe(merged)
                };
                continue;
            }

            result.Add(block);
        }

        return result;
    }

    /// <summary>
    /// How many leading rows the two tables have in common — 0 when they should not be
    /// stitched. The later piece must keep at least one row of its own.
    /// </summary>
    public static int SharedHeaderRows(DocxTable first, DocxTable second)
    {
        if (first.ColumnCount != second.ColumnCount || first.Rows.Count == 0 || second.Rows.Count < 2)
            return 0;

        var shared = 0;
        while (shared < MaxHeaderRows && shared < first.Rows.Count && shared < second.Rows.Count - 1
               && RowKey(first, shared) == RowKey(second, shared))
            shared++;

        // A shared first row that is entirely blank says nothing about the tables being one.
        return shared > 0 && RowKey(first, 0).Trim('|').Length > 0 ? shared : 0;
    }

    private static string RowKey(DocxTable table, int row) =>
        string.Join("|", table.RowTexts(row).Select(ImportText.Canonical));

    private static DocxTable Append(DocxTable first, DocxTable second, int skipRows)
    {
        var rows = first.Rows.ToList();
        var offset = rows.Count - skipRows;

        for (var row = skipRows; row < second.Rows.Count; row++)
        {
            rows.Add(second.Rows[row].Select(cell => new DocxCell
            {
                Row = cell.Row + offset,
                Column = cell.Column,
                Text = cell.Text,
                // A merge that started in a dropped header row now starts in the kept one.
                OriginRow = cell.OriginRow < skipRows ? cell.OriginRow : cell.OriginRow + offset,
                OriginColumn = cell.OriginColumn,
                IsHorizontalSpan = cell.IsHorizontalSpan
            }).ToList());
        }

        return new DocxTable(rows, first.SourceTables.Concat(second.SourceTables).ToList());
    }
}
