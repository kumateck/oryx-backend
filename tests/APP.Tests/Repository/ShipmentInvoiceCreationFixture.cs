using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;

namespace APP.Tests.Repository;

internal sealed class ShipmentInvoiceCreationFixture : IAsyncDisposable
{
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => ActorId;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    public static readonly Guid ActorId = Guid.NewGuid();
    public ApplicationDbContext Context { get; } = new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    public CreateShipmentInvoice Request { get; } = new() { Code = "IVN/2310/001" };
    public List<PurchaseOrder> Orders { get; } = [];
    public ProcurementRepository Repository { get; }
    private readonly ServiceProvider provider;

    public ShipmentInvoiceCreationFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(Context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        provider = services.BuildServiceProvider();
        Repository = new ProcurementRepository(Context, provider.GetRequiredService<IMapper>(),
            null!, null!, null!, null!, null!);
    }

    public async Task Seed(bool legacy = false)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" };
        var uom = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg" };
        var manufacturer = new Manufacturer { Id = Guid.NewGuid(), Name = "Manufacturer" };
        var materials = new[] {
            new Material { Id = Guid.NewGuid(), Name = "Material A" },
            new Material { Id = Guid.NewGuid(), Name = "Material B" }
        };
        Context.AddRange(supplier, uom, manufacturer);
        Context.AddRange(materials);
        Request.SupplierId = supplier.Id;
        var quantities = new decimal[] { 12_000_000_000m, 900_000_000m, 123_000_000m };
        for (var index = 0; index < quantities.Length; index++)
        {
            var material = materials[index == 2 ? 1 : 0];
            var order = new PurchaseOrder {
                Id = Guid.NewGuid(), Code = $"PO-{index}", SupplierId = supplier.Id,
                Status = PurchaseOrderStatus.Completed, Approved = true,
                Items = [new PurchaseOrderItem {
                    Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = uom.Id,
                    Quantity = quantities[index], Price = 1.5m,
                    PriceUoM = legacy ? null! : "kg"
                }]
            };
            Context.PurchaseOrders.Add(order);
            Orders.Add(order);
            Request.Items.Add(new CreateShipmentInvoiceItem {
                MaterialId = material.Id, UoMId = uom.Id, ManufacturerId = manufacturer.Id,
                PurchaseOrderId = order.Id, ExpectedQuantity = quantities[index],
                ReceivedQuantity = quantities[index], Reason = ""
            });
        }
        // The nonparallel test collection isolates this temporary legacy-seeding switch.
        var previous = ApplicationDbContext.EnforcePriceUoM;
        ApplicationDbContext.EnforcePriceUoM = false;
        try { await Context.SaveChangesAsync(); }
        finally { ApplicationDbContext.EnforcePriceUoM = previous; }
    }

    public async Task AddQuotation(PurchaseOrder order, bool winner = true, string unit = "kg")
    {
        var quotation = new SupplierQuotation {
            Id = Guid.NewGuid(), SupplierId = winner ? order.SupplierId : Guid.NewGuid()
        };
        Context.SupplierQuotations.Add(quotation);
        Context.SupplierQuotationItems.Add(new SupplierQuotationItem {
            Id = Guid.NewGuid(), SupplierQuotationId = quotation.Id,
            PurchaseOrderId = order.Id, MaterialId = order.Items[0].MaterialId,
            UoMId = order.Items[0].UoMId, QuotedPrice = 1.5m, PriceUoM = unit,
            Status = SupplierQuotationItemStatus.Processed
        });
        await Context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await provider.DisposeAsync();
        await Context.DisposeAsync();
    }
}
