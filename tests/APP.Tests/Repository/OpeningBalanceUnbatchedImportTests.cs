using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class OpeningBalanceUnbatchedImportTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public async Task Unbatched_rows_keep_their_own_dates_references_and_comparison_counts_without_posting_stock()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
        var department = new Department { Id = Guid.NewGuid(), Name = "Packaging department" };
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "piece" };
        var material = new Material { Id = Guid.NewGuid(), Code = "PM1", Name = "Packaging", Kind = MaterialKind.Package };
        var link = new MaterialDepartment { Id = Guid.NewGuid(), MaterialId = material.Id, DepartmentId = department.Id, UoMId = unit.Id };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Package store", DepartmentId = department.Id, Type = WarehouseType.PackagedStorage };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), Name = "Ground", WarehouseId = warehouse.Id };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), Name = "G/01", WarehouseLocationId = location.Id };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "G/01/A", Code = "PM/G/01/A", WarehouseLocationRackId = rack.Id };
        context.AddRange(department, unit, material, link, warehouse, location, rack, shelf);
        await context.SaveChangesAsync();
        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var workbook = new ExcelPackage();
        var sheet = workbook.Workbook.Worksheets.Add("Upload");
        string[] headers = ["Warehouse", "Material Code", "Material Name", "UOM", "Batch No.", "AR No.", "Manufacturing Date", "Expiry Date", "Quantity"];
        for (var i = 0; i < headers.Length; i++) sheet.Cells[1, i + 1].Value = headers[i];
        for (var row = 2; row <= 3; row++)
        {
            sheet.Cells[row, 1].Value = warehouse.Name;
            sheet.Cells[row, 2].Value = material.Code;
            sheet.Cells[row, 3].Value = material.Name;
            sheet.Cells[row, 4].Value = unit.Symbol;
            sheet.Cells[row, 6].Value = $"AR-{row}";
            sheet.Cells[row, 7].Value = row == 2 ? "01/01/2023" : "01/02/2024";
            sheet.Cells[row, 8].Value = row == 2 ? "31/01/2027" : "29/02/2028";
            sheet.Cells[row, 9].Value = row == 2 ? 10 : 20;
        }
        await using var stream = new MemoryStream(await workbook.GetAsByteArrayAsync());
        var repository = new StockMigrationRepository(context);
        var result = await repository.Upload(new FormFile(stream, 0, stream.Length, "file", "stock.xlsx"), department.Id, "mg", "ml", Guid.NewGuid());
        Assert.True(result.IsSuccess, result.Error?.Description);
        var rows = await context.StockMigrationLines.OrderBy(l => l.RowNumber).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, line =>
        {
            Assert.True(string.IsNullOrEmpty(line.BatchNumber));
            Assert.True(string.IsNullOrEmpty(line.Grn));
            Assert.Equal(StockMigrationLineStatus.Warning, line.Status);
            Assert.Contains("No supplier batch or GRN", line.Warnings);
            Assert.True(string.IsNullOrEmpty(line.Blockers));
        });
        Assert.Equal("AR-2", rows[0].ArNumber);
        Assert.Equal("AR-3", rows[1].ArNumber);
        Assert.Equal(2023, rows[0].ManufacturingDate?.Year);
        Assert.Equal(2024, rows[1].ManufacturingDate?.Year);
        var comparison = await repository.Comparison(result.Value);
        Assert.True(comparison.IsSuccess);
        Assert.Equal(2, Assert.Single(comparison.Value).NewBatches);
        Assert.Empty(await context.MaterialBatches.ToListAsync());
        Assert.Empty(await context.BinCardInformation.ToListAsync());

        var topUp = await repository.CreateTopUp(new CreateStockTopUpRequest
        {
            DepartmentId = department.Id, Reason = "Additional unnumbered stock", Lines = [
                new() { MaterialCode = material.Code, ShelfId = shelf.Id, BatchNumber = "", UnitSymbol = unit.Symbol, QuantityInSheet = 2 },
                new() { MaterialCode = material.Code, ShelfId = shelf.Id, UnitSymbol = unit.Symbol, QuantityInSheet = 3 },
            ],
        }, Guid.NewGuid());
        Assert.True(topUp.IsSuccess, topUp.Error?.Description);
        var topUpRows = await context.StockMigrationLines.Where(l => l.StockMigrationId == topUp.Value).ToListAsync();
        Assert.Equal(2, topUpRows.Count);
        Assert.All(topUpRows, line =>
        {
            Assert.True(string.IsNullOrWhiteSpace(line.BatchNumber));
            Assert.True(string.IsNullOrWhiteSpace(line.ArNumber));
            Assert.True(string.IsNullOrEmpty(line.Blockers));
            Assert.Contains("separate top-up stock", line.Warnings);
        });
        var topUpComparison = await repository.Comparison(topUp.Value);
        Assert.Equal(2, Assert.Single(topUpComparison.Value).NewBatches);
        Assert.Empty(await context.MaterialBatches.ToListAsync());
    }
}
