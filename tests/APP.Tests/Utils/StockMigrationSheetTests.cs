using APP.Services.Materials;
using OfficeOpenXml;
using Xunit;

namespace APP.Tests.Utils;

public class StockMigrationSheetTests
{
    private static readonly string[] Headers =
    [
        "Warehouse", "Shelves", "Batch No.", "UOM", "Material Code", "Material Name", "GRN NUMBER", "AR No.",
        "Manufacturing Date", "Expiry Date", "Quantity",
    ];

    private static ExcelPackage Workbook(Action<ExcelWorksheet> fill)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Raw Material ");
        for (var i = 0; i < Headers.Length; i++)
            sheet.Cells[1, i + 1].Value = Headers[i];
        fill(sheet);
        return package;
    }

    private static void Row(ExcelWorksheet sheet, int row, params object[] values)
    {
        for (var i = 0; i < values.Length; i++)
            sheet.Cells[row, i + 1].Value = values[i];
    }

    [Fact]
    public void Reads_month_year_typed_dates_that_Excel_stored_as_a_day_this_year()
    {
        using var package = Workbook(sheet =>
        {
            Row(sheet, 2, "SYRUP Raw Warehouse", "G/09/B", "11008/25", "mg", "RMA003", "Alginic Acid", "NWH/RM/25/077", "QCD/RM/25/357",
                new DateTime(2026, 4, 25), new DateTime(2026, 3, 30), 53d);
            sheet.Cells[2, 9].Style.Numberformat.Format = "mmm-d";
            sheet.Cells[2, 10].Style.Numberformat.Format = "mmm-d";
        });

        var line = Assert.Single(StockMigrationSheet.Read(package));

        Assert.Equal(new DateTime(2025, 4, 1), line.ManufacturingDate);
        Assert.Equal(new DateTime(2030, 3, 1), line.ExpiryDate);
        Assert.Contains("read as Mar 2030", line.Corrections);
    }

    [Fact]
    public void Keeps_dates_whose_format_shows_the_year()
    {
        using var package = Workbook(sheet =>
        {
            Row(sheet, 2, "W", "S", "B1", "mg", "RMA014", "Aerosil", null, null, new DateTime(2025, 9, 1), new DateTime(2029, 9, 1), 104d);
            sheet.Cells[2, 9].Style.Numberformat.Format = "mm/yyyy";
            sheet.Cells[2, 10].Style.Numberformat.Format = "mm/yyyy";
        });

        var line = Assert.Single(StockMigrationSheet.Read(package));

        Assert.Equal(new DateTime(2025, 9, 1), line.ManufacturingDate);
        Assert.Equal(new DateTime(2029, 9, 1), line.ExpiryDate);
        Assert.True(string.IsNullOrEmpty(line.Corrections));
    }

    [Theory]
    [InlineData("Feb-30", 2030, 2)]
    [InlineData("Jun-31", 2031, 6)]
    [InlineData("August 2027", 2027, 8)]
    [InlineData("03/2028", 2028, 3)]
    [InlineData("15/04/2027", 2027, 4)]
    public void Reads_month_and_year_written_as_text(string text, int year, int month)
    {
        var (date, _) = StockMigrationSheet.ParseMonthText(text, "Expiry date");

        Assert.Equal(new DateTime(year, month, 1), date);
    }

    [Fact]
    public void Repairs_numeric_batches_and_flags_batches_turned_into_dates()
    {
        using var package = Workbook(sheet =>
        {
            Row(sheet, 2, "W", "S", 285090820d, "mg", "RMA014", "Aerosil", null, null, null, null, 104d);
            Row(sheet, 3, "W", "S", new DateTime(2017, 1, 12), "mg", "RMA016", "Amaranth", null, null, null, null, 23.196d);
            sheet.Cells[3, 3].Style.Numberformat.Format = "dd/mm/yy";
        });

        var lines = StockMigrationSheet.Read(package);

        Assert.Equal("285090820", lines[0].BatchNumber);
        Assert.False(lines[0].BatchNumberDamaged);
        Assert.True(lines[1].BatchNumberDamaged);
    }

    [Fact]
    public void Takes_a_blank_warehouse_and_shelf_from_the_row_above()
    {
        using var package = Workbook(sheet =>
        {
            Row(sheet, 2, "SYRUP Raw Warehouse", "G/09/B", "B1", "mg", "RMC026", "Carbocisteine", null, null, null, null, 170d);
            Row(sheet, 3, null, null, "B2", "mg", "RMC026", "Carbocisteine", null, null, null, null, 500d);
        });

        var lines = StockMigrationSheet.Read(package);

        Assert.Equal("SYRUP Raw Warehouse", lines[1].WarehouseName);
        Assert.Equal("G/09/B", lines[1].ShelfName);
        Assert.Null(lines[1].SheetWarehouse);
    }

    [Fact]
    public void Uses_the_GRN_when_there_is_no_batch_and_drops_heading_rows()
    {
        using var package = Workbook(sheet =>
        {
            Row(sheet, 2, "SYRUP Package Warehouse", "G/7/A", "N/A", "piece", null, "BOTTLES", null, null, "N/A", "N/A", null);
            Row(sheet, 3, "SYRUP Package Warehouse", "G/7/A", "N/A", "piece", "PMC 228", "Alvite Tonic 200ml", " NWH/PM/23/127 \tNQC/PM/23/535\n", null, "N/A", "N/A", 225927d);
            Row(sheet, 4, "SYRUP Package Warehouse", "G/7/A", "N/A", "piece", "PMB012", "Glass Bottles 30ml", "NWH/PM/24/139", null, "N/A", "N/A", "-");
        });

        var lines = StockMigrationSheet.Read(package);

        Assert.Equal(2, lines.Count);
        Assert.Equal("PMC228", lines[0].MaterialCode);
        Assert.Equal("NWH/PM/23/127", lines[0].BatchNumber);
        Assert.Equal("NQC/PM/23/535", lines[0].ArNumber);
        Assert.Null(lines[0].ManufacturingDate);
        Assert.Null(lines[1].QuantityInSheet);
    }

    [Fact]
    public void Further_batches_with_a_blank_code_name_and_unit_take_the_material_above()
    {
        using var package = Workbook(sheet =>
        {
            Row(sheet, 2, "OINTMENT Raw Warehouse", null, "PCS2511319", "Kg", "RMC039", "Cetostearyl Alcohol", null, "QCD/RM/26/075", null, null, 2365.95d);
            Row(sheet, 3, null, null, "CS26063114", null, null, null, null, "QCD/RM/26/493", null, null, 3000d);
            Row(sheet, 4, "OINTMENT Raw Warehouse", null, "L477782309", "Kg", null, "Benzoyl peroxide", null, null, null, null, 100d);
            Row(sheet, 5, null, null, "L434382210", null, null, null, null, null, null, null, 1d);
        });

        var lines = StockMigrationSheet.Read(package);

        Assert.Equal("RMC039", lines[1].MaterialCode);
        Assert.Equal("Kg", lines[1].UnitSymbol);
        Assert.Equal("Cetostearyl Alcohol", lines[1].SheetMaterialName);
        Assert.Null(lines[1].SheetMaterialCode);
        Assert.Contains("row 2's material", lines[1].Corrections);
        Assert.Equal("OINTMENT Raw Warehouse", lines[1].WarehouseName);
        Assert.Null(lines[3].MaterialCode);
        Assert.Equal("Benzoyl peroxide", lines[3].SheetMaterialName);
    }

    [Fact]
    public void Reads_every_tab_with_a_code_and_quantity_column()
    {
        using var package = Workbook(sheet => Row(sheet, 2, "W", "S", "B1", "mg", "RMA014", "Aerosil", null, null, null, null, 1d));
        var other = package.Workbook.Worksheets.Add("Sheet3");
        for (var i = 0; i < Headers.Length; i++)
            other.Cells[1, i + 1].Value = Headers[i];
        Row(other, 2, "W", null, "B2", "MG", "RMG005", "Liquid Glucose", null, null, null, null, 24000d);
        package.Workbook.Worksheets.Add("Notes").Cells[1, 1].Value = "Counted by stores";

        var lines = StockMigrationSheet.Read(package);

        Assert.Equal(["Raw Material", "Sheet3"], lines.Select(l => l.Sheet).ToArray());
    }

    [Theory]
    [InlineData("G/7/A", "G/07/A")]
    [InlineData("g/09/b", "G/9/B")]
    public void Matches_shelves_ignoring_case_and_leading_zeros(string a, string b) =>
        Assert.Equal(StockMigrationSheet.ShelfKey(a), StockMigrationSheet.ShelfKey(b));

    [Theory]
    [InlineData("kg", "mg", 1_000_000)]
    [InlineData("g", "mg", 1_000)]
    [InlineData("L", "ml", 1_000)]
    [InlineData("mg", "mg", 1)]
    public void Converts_sheet_units_to_storage_units(string from, string to, decimal factor) =>
        Assert.Equal(factor, StockMigrationSheet.Factor(from, to));

    [Fact]
    public void Does_not_convert_between_weight_and_volume() =>
        Assert.Null(StockMigrationSheet.Factor("kg", "ml"));
}
