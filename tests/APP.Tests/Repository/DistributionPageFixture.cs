using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.Repository;

internal sealed class DistributionPageFixture : IAsyncDisposable
{
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "Production";
    }
    public ApplicationDbContext Context { get; } = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    public ShipmentDocument Shipment { get; } = new() { Id = Guid.NewGuid(), Code = "SD", Approved = true, Status = ShipmentStatus.Arrived };
    public RequisitionItem[] Requests { get; private set; } = [];
    public ShipmentInvoiceItem[] Lines { get; private set; } = [];
    public ProcurementRepository Repository => new(Context, null!, null!, null!, null!, null!, null!);

    public async Task Seed()
    {
        var material = new Material { Id = Guid.NewGuid(), Code = "RM", Name = "Material", Description = "Description" };
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg", Name = "Milligram" };
        var currency = new Currency { Id = Guid.NewGuid(), Name = "Dollar", Symbol = "$" };
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" };
        var manufacturer = new Manufacturer { Id = Guid.NewGuid(), Name = "Maker" };
        var source = new SourceRequisition { Id = Guid.NewGuid(), SupplierId = supplier.Id };
        var order = new PurchaseOrder { Id = Guid.NewGuid(), Code = "PO", SupplierId = supplier.Id, SourceRequisitionId = source.Id };
        var invoice = new ShipmentInvoice { Id = Guid.NewGuid(), SupplierId = supplier.Id };
        Shipment.ShipmentInvoiceId = invoice.Id;
        Context.AddRange(material, unit, currency, supplier, manufacturer, source, order, invoice, Shipment);
        var requests = new List<RequisitionItem>();
        foreach (var quantity in new[] { 60m, 40m })
        {
            var department = new Department { Id = Guid.NewGuid(), Code = $"D{quantity}", Name = $"Dept {quantity}", Description = "Department description" };
            var request = new Requisition { Id = Guid.NewGuid(), DepartmentId = department.Id };
            var item = new RequisitionItem { Id = Guid.NewGuid(), RequisitionId = request.Id,
                MaterialId = material.Id, UoMId = unit.Id, Quantity = quantity, Status = RequestStatus.Sourced };
            // Duplicate linkage must not duplicate the department's request.
            Context.AddRange(department, request, item,
                new SourceRequisitionItem { Id = Guid.NewGuid(), SourceRequisitionId = source.Id,
                    RequisitionId = request.Id, MaterialId = material.Id, UoMId = unit.Id },
                new SourceRequisitionItem { Id = Guid.NewGuid(), SourceRequisitionId = source.Id,
                    RequisitionId = request.Id, MaterialId = material.Id, UoMId = unit.Id });
            requests.Add(item);
        }
        Requests = requests.ToArray();
        Lines = new[] { 40m, 20m }.Select(quantity => new ShipmentInvoiceItem {
            Id = Guid.NewGuid(), ShipmentInvoiceId = invoice.Id, MaterialId = material.Id,
            UoMId = unit.Id, ManufacturerId = manufacturer.Id, PurchaseOrderId = order.Id,
            ReceivedQuantity = quantity, ExpectedQuantity = quantity, Price = 1.5m,
            PriceUoM = "kg", CurrencyId = currency.Id
        }).ToArray();
        // Insert the newer source first; calculation must still use creation time.
        Context.AddRange(Lines.Reverse());
        await Context.SaveChangesAsync();
        Lines[0].CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Lines[1].CreatedAt = Lines[0].CreatedAt.AddDays(1);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
