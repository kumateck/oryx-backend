using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialStockImportTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public async Task New_batch_total_equals_shelf_and_bin_card_totals_without_counting_the_first_row_twice()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
        var material = new Material { Id = Guid.NewGuid(), Code = "RM1", Name = "Test raw material" };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Raw store" };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), WarehouseId = warehouse.Id };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocationId = location.Id };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRackId = rack.Id, Code = "S1" };
        context.AddRange(unit, material, warehouse, location, rack, shelf);
        await context.SaveChangesAsync();
        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var workbook = new ExcelPackage();
        var sheet = workbook.Workbook.Worksheets.Add("Stock");
        var headers = new[] { "Warehouse", "Shelves", "Material Code", "Batch No.", "UOM", "Quantity" };
        for (var i = 0; i < headers.Length; i++) sheet.Cells[1, i + 1].Value = headers[i];
        for (var row = 2; row <= 3; row++)
        {
            sheet.Cells[row, 1].Value = warehouse.Name;
            sheet.Cells[row, 2].Value = shelf.Code;
            sheet.Cells[row, 3].Value = material.Code;
            sheet.Cells[row, 4].Value = "B1";
            sheet.Cells[row, 5].Value = "mg";
            sheet.Cells[row, 6].Value = row == 2 ? 1000000m : 250000m;
        }
        await using var stream = new MemoryStream(await workbook.GetAsByteArrayAsync());
        var file = new FormFile(stream, 0, stream.Length, "file", "stock.xlsx");
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var result = await new MaterialRepository(context, mapper).ImportMaterialStockFromExcel(file);
        Assert.True(result.IsSuccess);
        var batch = Assert.Single(await context.MaterialBatches.ToListAsync());
        Assert.Equal(1250000m, batch.TotalQuantity);
        Assert.Equal(1250000m, batch.QuantityAssigned);
        Assert.Equal(batch.TotalQuantity, await context.ShelfMaterialBatches.SumAsync(s => s.Quantity));
        Assert.Equal(batch.TotalQuantity, await context.BinCardInformation.SumAsync(s => s.QuantityReceived));
    }
}
