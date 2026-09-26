namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public enum DocxBlockKind
{
    Heading,
    Paragraph,
    Table
}

/// <summary>
/// One position of a table's rectangular grid. Every grid position exists: a cell widened
/// by <c>gridSpan</c> fills each column it covers, and a <c>vMerge</c> continuation fills its
/// row. Both kinds of filler point back at the cell that actually carries the text.
/// </summary>
public sealed class DocxCell
{
    public int Row { get; init; }
    public int Column { get; init; }

    /// <summary>Normalized text of this position's own cell; empty for any filler.</summary>
    public string Text { get; init; } = string.Empty;

    public int OriginRow { get; init; }
    public int OriginColumn { get; init; }

    /// <summary>True when this position is covered by another cell (horizontal span or vertical merge).</summary>
    public bool IsContinuation => OriginRow != Row || OriginColumn != Column;

    /// <summary>True for a horizontal-span filler specifically.</summary>
    public bool IsHorizontalSpan { get; init; }
}

public sealed class DocxTable
{
    public DocxTable(IReadOnlyList<IReadOnlyList<DocxCell>> rows, IReadOnlyList<int> sourceTables)
    {
        Rows = rows;
        SourceTables = sourceTables;
        ColumnCount = rows.Count == 0 ? 0 : rows.Max(row => row.Count);
    }

    /// <summary>Ordinal among the document's tables after stitching.</summary>
    public int Ordinal { get; set; }

    public IReadOnlyList<IReadOnlyList<DocxCell>> Rows { get; }

    /// <summary>The raw (pre-stitch) table ordinals this table was built from.</summary>
    public IReadOnlyList<int> SourceTables { get; }

    public int ColumnCount { get; }

    public DocxCell Cell(int row, int column) =>
        row < Rows.Count && column < Rows[row].Count ? Rows[row][column] : null;

    /// <summary>The text of whatever cell covers this grid position.</summary>
    public string Resolved(int row, int column)
    {
        var cell = Cell(row, column);
        return cell is null ? string.Empty : Cell(cell.OriginRow, cell.OriginColumn)?.Text ?? string.Empty;
    }

    /// <summary>The distinct cells of a row, in order, skipping span/merge fillers.</summary>
    public IEnumerable<DocxCell> OriginCells(int row) =>
        row < Rows.Count ? Rows[row].Where(cell => !cell.IsContinuation) : [];

    /// <summary>Row text as the resolved value of every grid column.</summary>
    public IReadOnlyList<string> RowTexts(int row) =>
        Enumerable.Range(0, ColumnCount).Select(column => Resolved(row, column)).ToList();
}

public sealed class DocxBlock
{
    public int Index { get; set; }
    public DocxBlockKind Kind { get; init; }

    /// <summary>Normalized paragraph text; for a table, its rows joined for display.</summary>
    public string Text { get; init; } = string.Empty;

    public string Style { get; init; }
    public DocxTable Table { get; init; }
}

public sealed class DocxDocument
{
    /// <summary>Text of every non-empty <c>word/header*.xml</c> part, "Page x of y" removed.</summary>
    public string HeaderText { get; init; } = string.Empty;

    public IReadOnlyList<DocxBlock> Blocks { get; init; } = [];

    public IEnumerable<DocxTable> Tables => Blocks.Where(block => block.Table is not null).Select(block => block.Table);
}
