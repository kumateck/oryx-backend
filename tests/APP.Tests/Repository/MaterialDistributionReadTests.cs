using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialDistributionReadTests
{
    private sealed class CurrentUser(Guid departmentId) : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => departmentId;
        public string DepartmentType => "Production";
    }

    [Fact]
    public async Task Arrived_shipment_details_preserve_allocations_and_sources_without_loading_storage_or_distributing()
    {
        var department = new Department { Id = Guid.NewGuid(), Code = "PROD", Name = "Production" };
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser(department.Id));
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Raw warehouse", DepartmentId = department.Id };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), Name = "Ground floor", WarehouseId = warehouse.Id };
        var material = new Material { Id = Guid.NewGuid(), Name = "Material", Code = "RM-1" };
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg" };
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" };
        var manufacturer = new Manufacturer { Id = Guid.NewGuid(), Name = "Manufacturer" };
        var request = new Requisition { Id = Guid.NewGuid(), Code = "REQ-1", DepartmentId = department.Id };
        var requestItem = new RequisitionItem {
            Id = Guid.NewGuid(), RequisitionId = request.Id, MaterialId = material.Id,
            UoMId = unit.Id, Quantity = 100_000_000m
        };
        var source = new SourceRequisition { Id = Guid.NewGuid(), Code = "SRC-1", SupplierId = supplier.Id };
        var sourceItem = new SourceRequisitionItem {
            Id = Guid.NewGuid(), SourceRequisitionId = source.Id, RequisitionId = request.Id,
            MaterialId = material.Id, UoMId = unit.Id, Quantity = 100_000_000m
        };
        var order = new PurchaseOrder { Id = Guid.NewGuid(), Code = "PO-1", SupplierId = supplier.Id, SourceRequisitionId = source.Id };
        var invoice = new ShipmentInvoice { Id = Guid.NewGuid(), Code = "INV-1", SupplierId = supplier.Id };
        var invoiceItem = new ShipmentInvoiceItem {
            Id = Guid.NewGuid(), ShipmentInvoiceId = invoice.Id, MaterialId = material.Id,
            UoMId = unit.Id, PurchaseOrderId = order.Id, ManufacturerId = manufacturer.Id,
            ReceivedQuantity = 60_000_000m, ExpectedQuantity = 60_000_000m,
            Price = 1.5m, PriceUoM = "kg"
        };
        var shipment = new ShipmentDocument {
            Id = Guid.NewGuid(), Code = "SD-1", ShipmentInvoiceId = invoice.Id,
            Status = ShipmentStatus.Arrived, Approved = true
        };
        context.AddRange(department, warehouse, location, material, unit, supplier, manufacturer,
            request, requestItem, source, sourceItem, order, invoice, invoiceItem, shipment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        var repository = new ProcurementRepository(context, mapper, null!, null!, null!, null!, null!);

        var result = await repository.GetMaterialDistribution(shipment.Id);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var section = Assert.Single(result.Value.Sections);
        Assert.Equal(material.Id, section.Material.Id);
        Assert.Equal(60_000_000m, section.TotalQuantity);
        var share = Assert.Single(section.Items);
        Assert.Equal(department.Id, share.Department.Id);
        Assert.Equal("Production", share.Department.Name);
        Assert.Equal(100_000_000m, share.QuantityRequested);
        Assert.Equal(60_000_000m, share.QuantityAllocated);
        Assert.Equal(40_000_000m, share.QuantityRemaining);
        var supplied = Assert.Single(share.Distributions);
        Assert.Equal(invoiceItem.Id, supplied.ShipmentInvoiceItem.Id);
        Assert.Equal(60_000_000m, supplied.Quantity);
        Assert.Equal(1.5m, supplied.ShipmentInvoiceItem.Price);
        Assert.Equal("kg", supplied.ShipmentInvoiceItem.PriceUoM);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.False((await context.ShipmentInvoiceItems.SingleAsync()).Distributed);
        Assert.Null((await context.ShipmentDocuments.SingleAsync()).CompletedDistributionAt);
        Assert.Equal(0m, (await context.RequisitionItems.SingleAsync()).QuantityReceived);
    }
}
