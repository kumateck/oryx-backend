using System.Globalization;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Materials.StockMigrations;
using OfficeOpenXml;

namespace APP.Services.Materials;

/// <summary>
/// Reads a stock-count workbook into migration lines. Every tab that has a material code and a quantity
/// column is read; heading rows (no code, no quantity) are dropped. The quirks Excel leaves in hand-typed
/// stock sheets are repaired here and noted on the line, so the verification page can show them.
/// </summary>
public static partial class StockMigrationSheet
{
    private static readonly Dictionary<string, string[]> Columns = new()
    {
        ["warehouse"] = ["warehouse", "warehousename", "store"],
        ["shelf"] = ["shelf", "shelves", "shelve", "shelfname", "location"],
        ["batch"] = ["batchno", "batch", "batchnumber"],
        ["unit"] = ["uom", "unit", "symbol", "baseuom"],
        ["code"] = ["materialcode", "code", "itemcode"],
        ["name"] = ["materialname", "name", "itemname", "rawmaterialname"],
        ["grn"] = ["grnnumber", "grn", "grnno", "waybill"],
        ["ar"] = ["arno", "arnumber", "ardno", "ardnumber"],
        ["manufactured"] = ["manufacturingdate", "manufacturedate", "mfgdate", "mfg"],
        ["expiry"] = ["expirydate", "expiry", "exp", "expdate"],
        ["quantity"] = ["quantity", "qty", "availableqty", "availablequantity"],
    };

    public static string Normal(string text) =>
        NonAlphanumeric().Replace((text ?? string.Empty).ToLowerInvariant(), string.Empty);

    public static List<StockMigrationLine> Read(ExcelPackage package)
    {
        var lines = new List<StockMigrationLine>();
        foreach (var sheet in package.Workbook.Worksheets)
        {
            if (sheet.Dimension == null)
                continue;
            var columns = HeaderColumns(sheet);
            if (!columns.ContainsKey("code") || !columns.ContainsKey("quantity"))
                continue;
            lines.AddRange(ReadSheet(sheet, columns));
        }
        return lines;
    }

    private static Dictionary<string, int> HeaderColumns(ExcelWorksheet sheet)
    {
        var headers = new Dictionary<string, int>();
        for (var col = 1; col <= sheet.Dimension.End.Column; col++)
        {
            var header = Normal(sheet.Cells[1, col].Text);
            if (header.Length > 0)
                headers.TryAdd(header, col);
        }

        var found = new Dictionary<string, int>();
        foreach (var (field, names) in Columns)
        {
            var match = names.Select(n => headers.TryGetValue(n, out var c) ? c : 0).FirstOrDefault(c => c > 0);
            if (match > 0)
                found[field] = match;
        }
        return found;
    }

    private static IEnumerable<StockMigrationLine> ReadSheet(ExcelWorksheet sheet, Dictionary<string, int> columns)
    {
        string previousWarehouse = null, previousShelf = null;
        var previousRow = 0;

        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            ExcelRange Cell(string field) => columns.TryGetValue(field, out var c) ? sheet.Cells[row, c] : null;
            string Text(string field) => Clean(Cell(field)?.Text);

            var code = Text("code");
            var quantityCell = Cell("quantity");
            var quantity = ReadQuantity(quantityCell);
            if (code.Length == 0 && quantity is null)
                continue;

            var corrections = new List<string>();
            var warehouse = Text("warehouse");
            var shelf = Text("shelf");
            if (warehouse.Length == 0 && shelf.Length == 0 && previousWarehouse != null)
            {
                warehouse = previousWarehouse;
                shelf = previousShelf;
                corrections.Add($"Warehouse and shelf were blank, so row {previousRow}'s were used ({warehouse}, {shelf}).");
            }
            else if (warehouse.Length > 0)
            {
                previousWarehouse = warehouse;
                previousShelf = shelf;
                previousRow = row;
            }

            var (batch, batchDamaged, batchNote) = ReadBatch(Cell("batch"));
            if (batchNote != null)
                corrections.Add(batchNote);

            var grn = Text("grn");
            var ar = Text("ar");
            var (grnNumber, arFromGrn) = SplitGrn(grn);
            if (ar.Length == 0 && arFromGrn != null)
                ar = arFromGrn;
            if (batch.Length == 0 && !batchDamaged && grnNumber != null)
            {
                batch = grnNumber;
                corrections.Add($"No batch number, so the GRN {grnNumber} is used as the batch.");
            }

            var (manufactured, manufacturedNote) = ReadMonth(Cell("manufactured"), "Manufacturing date");
            var (expiry, expiryNote) = ReadMonth(Cell("expiry"), "Expiry date");
            if (manufacturedNote != null)
                corrections.Add(manufacturedNote);
            if (expiryNote != null)
                corrections.Add(expiryNote);

            var unit = Text("unit");
            yield return new StockMigrationLine
            {
                Id = Guid.NewGuid(),
                Sheet = Clip(sheet.Name.Trim(), 200),
                RowNumber = row,
                SheetWarehouse = Clip(Text("warehouse"), 500),
                SheetShelf = Clip(Text("shelf"), 200),
                SheetMaterialCode = Clip(code, 200),
                SheetMaterialName = Clip(Text("name"), 1000),
                SheetBatchNumber = Clip(Cell("batch")?.Text?.Trim(), 500),
                SheetUnit = Clip(unit, 50),
                SheetQuantity = Clip(quantityCell?.Text?.Trim(), 100),
                SheetManufacturingDate = Clip(Cell("manufactured")?.Text?.Trim(), 100),
                SheetExpiryDate = Clip(Cell("expiry")?.Text?.Trim(), 100),
                Grn = Clip(grnNumber ?? grn, 500),
                ArNumber = Clip(ar, 500),
                MaterialCode = Clip(WhiteSpace().Replace(code, string.Empty).ToUpperInvariant(), 200),
                WarehouseName = Clip(warehouse, 500),
                ShelfName = Clip(shelf, 200),
                BatchNumber = Clip(batch, 500),
                BatchNumberDamaged = batchDamaged,
                UnitSymbol = Clip(unit, 50),
                QuantityInSheet = quantity,
                ManufacturingDate = manufactured,
                ExpiryDate = expiry,
                Corrections = Clip(string.Join('\n', corrections), 4000),
            };
        }
    }

    private static string Clean(string text) => WhiteSpace().Replace(text ?? string.Empty, " ").Trim();

    private static string Clip(string text, int length) =>
        string.IsNullOrEmpty(text) ? null : text.Length <= length ? text : text[..length];

    public static decimal? ReadQuantity(ExcelRange cell)
    {
        var value = cell?.Value;
        switch (value)
        {
            case null:
                return null;
            case double d:
                return d > 0 ? (decimal)d : null;
            case decimal m:
                return m > 0 ? m : null;
            case int i:
                return i > 0 ? i : null;
        }
        var text = Clean(value.ToString()).Replace(",", string.Empty);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : null;
    }

    /// <summary>The batch number, whether Excel spoiled it beyond repair, and a note when it was repaired.</summary>
    private static (string Batch, bool Damaged, string Note) ReadBatch(ExcelRange cell)
    {
        var value = cell?.Value;
        if (value is null)
            return (string.Empty, false, null);

        if (value is DateTime || (value is double && IsDateFormat(cell)))
            return (Clean(cell.Text), true, null);

        if (value is double d)
        {
            var text = d == Math.Floor(d) ? d.ToString("0", CultureInfo.InvariantCulture) : d.ToString(CultureInfo.InvariantCulture);
            return (text, false, $"Batch number was stored by Excel as a number; read as {text}.");
        }

        var batch = Clean(value.ToString());
        return IsBlankMarker(batch) ? (string.Empty, false, null) : (batch, false, null);
    }

    private static bool IsBlankMarker(string text) =>
        text.Length == 0 || text is "-" or "--" || Normal(text) is "na" or "nil" or "none";

    /// <summary>A GRN cell sometimes holds the GRN and the AR number together; the first is the GRN.</summary>
    public static (string Grn, string Ar) SplitGrn(string cell)
    {
        var parts = (cell ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || IsBlankMarker(parts[0]))
            return (null, null);
        return (parts[0], parts.Length > 1 ? parts[1] : null);
    }

    private static readonly string[] Months = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;

    /// <summary>
    /// A manufacturing or expiry month. Stores type these as month/year ("Mar-30"); Excel reads that as the
    /// 30th of March this year and shows it without a year. A date shown without a year is therefore read
    /// back as month and year, and the line notes the repair.
    /// </summary>
    public static (DateTime? Date, string Note) ReadMonth(ExcelRange cell, string label)
    {
        var value = cell?.Value;
        if (value is null)
            return (null, null);

        DateTime? date = value switch
        {
            DateTime dt => dt,
            double d when IsDateFormat(cell) => DateTime.FromOADate(d),
            _ => null,
        };
        if (date is { } stored)
        {
            if (!FormatShowsYear(cell))
            {
                var repaired = Month(2000 + stored.Day, stored.Month);
                return (repaired, $"{label} \"{cell.Text}\" read as {repaired:MMM yyyy} (Excel had stored {stored:d MMM yyyy}).");
            }
            return (Month(stored.Year, stored.Month), null);
        }

        return ParseMonthText(Clean(value.ToString()), label);
    }

    public static (DateTime? Date, string Note) ParseMonthText(string text, string label)
    {
        if (IsBlankMarker(text))
            return (null, null);

        // Mon-YY, Mon YYYY, Month-YY: typed month/year Excel did not recognise (e.g. "Feb-30").
        var named = NamedMonth().Match(text);
        if (named.Success)
        {
            var month = Array.FindIndex(Months, m => m.Length > 0 && named.Groups[1].Value.StartsWith(m, StringComparison.OrdinalIgnoreCase)) + 1;
            if (month > 0)
            {
                var year = int.Parse(named.Groups[2].Value, CultureInfo.InvariantCulture);
                var date = Month(year < 100 ? 2000 + year : year, month);
                return (date, $"{label} \"{text}\" read as {date:MMM yyyy}.");
            }
        }

        // MM/YY or MM/YYYY.
        var numeric = NumericMonth().Match(text);
        if (numeric.Success)
        {
            var month = int.Parse(numeric.Groups[1].Value, CultureInfo.InvariantCulture);
            var year = int.Parse(numeric.Groups[2].Value, CultureInfo.InvariantCulture);
            if (month is >= 1 and <= 12)
                return (Month(year < 100 ? 2000 + year : year, month), null);
        }

        string[] formats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd.MM.yyyy"];
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var full))
            return (Month(full.Year, full.Month), null);

        return (null, $"{label} \"{text}\" could not be read and was left empty.");
    }

    private static DateTime Month(int year, int month) => new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

    private static bool IsDateFormat(ExcelRange cell)
    {
        var id = cell.Style.Numberformat.NumFmtID;
        if (id is >= 14 and <= 22 or >= 45 and <= 47)
            return true;
        var format = DateFormatText(cell);
        return format.Contains('d') || format.Contains('y') || (format.Contains('m') && !format.Contains('0') && !format.Contains('#'));
    }

    private static bool FormatShowsYear(ExcelRange cell)
    {
        var id = cell.Style.Numberformat.NumFmtID;
        if (id is 16 or 20 or 21 or 45 or 46 or 47)
            return false;
        if (id is >= 14 and <= 22)
            return true;
        return DateFormatText(cell).Contains('y');
    }

    /// <summary>The number format with quoted text, escapes and colour/locale codes removed.</summary>
    private static string DateFormatText(ExcelRange cell) =>
        FormatNoise().Replace(cell.Style.Numberformat.Format ?? string.Empty, string.Empty).ToLowerInvariant().Replace("general", string.Empty);

    /// <summary>How many of a storage unit one sheet unit is (kg → mg is 1,000,000). Null when the units are not of one family.</summary>
    public static decimal? Factor(string sheetUnit, string storageUnit)
    {
        var from = Scale(sheetUnit);
        var to = Scale(storageUnit);
        if (from is null || to is null || from.Value.Family != to.Value.Family)
            return null;
        return from.Value.Size / to.Value.Size;
    }

    private static (string Family, decimal Size)? Scale(string unit) =>
        Normal(unit) switch
        {
            "mg" or "milligram" or "milligrams" => ("mass", 1m),
            "g" or "gram" or "grams" => ("mass", 1_000m),
            "kg" or "kilogram" or "kilograms" => ("mass", 1_000_000m),
            "ml" or "milliliter" or "millilitre" => ("volume", 1m),
            "l" or "liter" or "litre" or "ltr" => ("volume", 1_000m),
            _ => null,
        };

    public static bool IsMassUnit(string unit) => Scale(unit)?.Family == "mass";

    public static bool IsVolumeUnit(string unit) => Scale(unit)?.Family == "volume";

    /// <summary>How alike two names are, from 0 to 1, by shared letter pairs (punctuation and case ignored).</summary>
    public static double Similarity(string a, string b)
    {
        static List<string> Pairs(string s)
        {
            var n = Normal(s);
            var pairs = new List<string>();
            for (var i = 0; i < n.Length - 1; i++)
                pairs.Add(n.Substring(i, 2));
            return pairs;
        }

        var first = Pairs(a);
        var second = Pairs(b);
        if (first.Count == 0 || second.Count == 0)
            return Normal(a) == Normal(b) ? 1 : 0;
        var pool = second.ToList();
        var shared = 0;
        foreach (var pair in first)
        {
            var at = pool.IndexOf(pair);
            if (at < 0)
                continue;
            shared++;
            pool.RemoveAt(at);
        }
        return 2.0 * shared / (first.Count + second.Count);
    }

    /// <summary>A shelf name for matching: case, spaces and leading zeros ignored, so "G/7/A" matches "G/07/A".</summary>
    public static string ShelfKey(string shelf) =>
        string.Join('/', (shelf ?? string.Empty).ToUpperInvariant().Replace(" ", string.Empty).Split('/', '-', '\\')
            .Select(part => part.TrimStart('0') is { Length: > 0 } trimmed ? trimmed : part.Length > 0 ? "0" : part));

    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();

    [GeneratedRegex(@"^([A-Za-z]{3,9})[\s\-/.,']*(\d{2}|\d{4})$")]
    private static partial Regex NamedMonth();

    [GeneratedRegex(@"^(\d{1,2})\s*[/\-.]\s*(\d{2}|\d{4})$")]
    private static partial Regex NumericMonth();

    [GeneratedRegex("\"[^\"]*\"|\\\\.|\\[[^\\]]*\\]")]
    private static partial Regex FormatNoise();
}
