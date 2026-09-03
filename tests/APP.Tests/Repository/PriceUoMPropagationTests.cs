using System.Reflection;
using APP.Extensions;
using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// A price is uninterpretable without the unit it was quoted in. These tests pin the
/// three ways that unit used to get lost between the supplier quotation and the screens
/// that show the price.
/// </summary>
public class PriceUoMPropagationTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService()
        );

    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static RequisitionRepository CreateRequisitionRepository(ApplicationDbContext context) =>
        new(
            context,
            CreateMapper(context),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    // ------------------------------------------------------------------
    // Cross-supplier contamination
    // ------------------------------------------------------------------

    [Fact]
    public async Task Lookup_ignores_losing_suppliers_quotations()
    {
        // When a quotation is awarded, every LOSING supplier's line for that material is
        // also stamped with the winner's PurchaseOrderId so the reassign-supplier picker
        // can find them. A lookup keyed on the purchase order alone therefore picks a
        // competitor's unit at random.
        await using var context = CreateContext();

        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var uomId = Guid.NewGuid();

        context.Suppliers.AddRange(
            new Supplier { Id = winnerId, Name = "Winner" },
            new Supplier { Id = loserId, Name = "Loser" }
        );
        context.Materials.Add(new Material { Id = materialId, Name = "Material" });
        context.UnitOfMeasures.Add(new UnitOfMeasure { Id = uomId, Symbol = "g" });

        context.PurchaseOrders.Add(
            new PurchaseOrder
            {
                Id = purchaseOrderId,
                SupplierId = winnerId,
                Code = "PO-1",
            }
        );

        var winningQuotation = new SupplierQuotation { Id = Guid.NewGuid(), SupplierId = winnerId };
        var losingQuotation = new SupplierQuotation { Id = Guid.NewGuid(), SupplierId = loserId };
        context.SupplierQuotations.AddRange(winningQuotation, losingQuotation);

        context.SupplierQuotationItems.AddRange(
            new SupplierQuotationItem
            {
                Id = Guid.NewGuid(),
                SupplierQuotationId = winningQuotation.Id,
                SupplierQuotation = winningQuotation,
                PurchaseOrderId = purchaseOrderId,
                MaterialId = materialId,
                UoMId = uomId,
                QuotedPrice = 12.5m,
                PriceUoM = "kg",
                Status = SupplierQuotationItemStatus.Processed,
            },
            new SupplierQuotationItem
            {
                Id = Guid.NewGuid(),
                SupplierQuotationId = losingQuotation.Id,
                SupplierQuotation = losingQuotation,
                PurchaseOrderId = purchaseOrderId,
                MaterialId = materialId,
                UoMId = uomId,
                QuotedPrice = 0.02m,
                PriceUoM = "g",
                Status = SupplierQuotationItemStatus.NotUsed,
            }
        );

        await context.SaveChangesAsync();

        var lookup = await context.BuildQuotationPriceUoMLookup([purchaseOrderId]);
        var resolved = lookup.ResolvePriceUoM(null, purchaseOrderId, materialId, uomId);

        Assert.Equal("kg", resolved);
    }

    // ------------------------------------------------------------------
    // Clobbering
    // ------------------------------------------------------------------

    [Fact]
    public void Persisted_price_uom_wins_over_the_quotation()
    {
        // A purchase-order revision writes a new PriceUoM. The quotation still holds the
        // original, so a fallback that overwrites unconditionally silently reverts it.
        var purchaseOrderId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var uomId = Guid.NewGuid();

        var lookup = new Dictionary<PriceUoMExtensions.PriceUoMKey, string>
        {
            [new PriceUoMExtensions.PriceUoMKey(purchaseOrderId, materialId, uomId)] = "kg",
        };

        Assert.Equal("g", lookup.ResolvePriceUoM("g", purchaseOrderId, materialId, uomId));
    }

    [Fact]
    public void Missing_fallback_never_blanks_a_stored_value()
    {
        var lookup = new Dictionary<PriceUoMExtensions.PriceUoMKey, string>();

        Assert.Equal(
            "kg",
            lookup.ResolvePriceUoM("kg", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())
        );
    }

    // ------------------------------------------------------------------
    // Partial re-submit
    // ------------------------------------------------------------------

    [Fact]
    public async Task Receiving_a_partial_quotation_leaves_omitted_items_alone()
    {
        await using var context = CreateContext();

        var quotationId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var submittedItemId = Guid.NewGuid();
        var omittedItemId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var uomId = Guid.NewGuid();

        context.Suppliers.Add(new Supplier { Id = supplierId, Name = "Supplier" });
        context.Materials.Add(new Material { Id = materialId, Name = "Material" });
        context.UnitOfMeasures.Add(new UnitOfMeasure { Id = uomId, Symbol = "g" });

        context.SupplierQuotations.Add(
            new SupplierQuotation
            {
                Id = quotationId,
                SupplierId = supplierId,
                Items =
                [
                    new SupplierQuotationItem
                    {
                        Id = submittedItemId,
                        SupplierQuotationId = quotationId,
                        MaterialId = materialId,
                        UoMId = uomId,
                    },
                    new SupplierQuotationItem
                    {
                        Id = omittedItemId,
                        SupplierQuotationId = quotationId,
                        MaterialId = materialId,
                        UoMId = uomId,
                        QuotedPrice = 4m,
                        PriceUoM = "kg",
                    },
                ],
            }
        );
        await context.SaveChangesAsync();

        var repository = CreateRequisitionRepository(context);

        var result = await repository.ReceiveQuotationFromSupplier(
            [
                new SupplierQuotationResponseDto
                {
                    Id = submittedItemId,
                    Price = 9m,
                    PriceUoM = "g",
                },
            ],
            quotationId
        );

        Assert.True(
            result.IsSuccess,
            string.Join("; ", result.Errors.Select(e => e.Code + ": " + e.Description))
        );

        var omitted = await context.SupplierQuotationItems.FirstAsync(i => i.Id == omittedItemId);
        Assert.Equal(4m, omitted.QuotedPrice);
        Assert.Equal("kg", omitted.PriceUoM);

        var submitted = await context.SupplierQuotationItems.FirstAsync(i =>
            i.Id == submittedItemId
        );
        Assert.Equal(9m, submitted.QuotedPrice);
        Assert.Equal("g", submitted.PriceUoM);
    }

    // ------------------------------------------------------------------
    // Price and quantity in different units
    // ------------------------------------------------------------------

    [Theory]
    // The real purchase order EPRC-87: 5 mg quoted at 0.1 per kg.
    [InlineData(5, "mg", "kg", 0.000005)]
    [InlineData(5, "mg", "g", 0.005)]
    [InlineData(2, "kg", "g", 2000)]
    [InlineData(1500, "ml", "L", 1.5)]
    [InlineData(3, "kg", "kg", 3)]
    public void Convert_reconciles_units_within_a_dimension(
        decimal quantity,
        string from,
        string to,
        decimal expected
    )
    {
        Assert.Equal(expected, UomConverter.Convert(quantity, from, to));
    }

    [Theory]
    [InlineData("m", "kg")] // length priced by mass needs density the system does not hold
    [InlineData("kg", "L")]
    [InlineData("pcs", "kg")] // unknown symbol
    [InlineData("kg", "")]
    public void Convert_refuses_to_guess_across_dimensions(string from, string to)
    {
        Assert.Null(UomConverter.Convert(5m, from, to));
    }

    [Fact]
    public void Cost_reconciles_the_price_uom_against_the_quantity_uom()
    {
        // Purchase order EPRC-87 as returned by production: 5 mg at 0.1 per kg reported a
        // cost of 0.5 - the raw product, six orders of magnitude out.
        var item = new PurchaseOrderItemDto
        {
            Quantity = 5m,
            QuantityInvoiced = 0m,
            Price = 0.1m,
            PriceUoM = "kg",
            Uom = new UnitOfMeasureDto { Symbol = "mg" },
        };

        Assert.Equal(0.0000005m, item.Cost);
    }

    [Fact]
    public void Cost_falls_back_to_the_raw_product_when_units_cannot_be_reconciled()
    {
        var item = new PurchaseOrderItemDto
        {
            Quantity = 5m,
            Price = 0.1m,
            PriceUoM = "kg",
            Uom = new UnitOfMeasureDto { Symbol = "m" },
        };

        Assert.Equal(0.5m, item.Cost);
    }

    [Theory]
    [InlineData(5, "mg", "kg", true)] // 0.000005 of a kilogram - almost certainly an error
    [InlineData(5, "g", "kg", true)] // 0.005
    [InlineData(500, "g", "kg", false)] // 0.5 of a kilogram is an ordinary part order
    [InlineData(5, "kg", "kg", false)]
    [InlineData(5, "m", "kg", false)] // unconvertible, so not a scale judgement to make
    public void Negligible_quantities_against_the_price_uom_are_flagged(
        decimal quantity,
        string quantityUoM,
        string priceUoM,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            UomConverter.IsNegligibleAgainstPriceUoM(quantity, quantityUoM, priceUoM)
        );
    }

    // ------------------------------------------------------------------
    // The next DTO someone adds
    // ------------------------------------------------------------------

    [Fact]
    public void Every_priced_contract_in_the_procurement_chain_declares_a_price_uom()
    {
        // Scoped to the supplier-quotation -> purchase-order -> shipment chain. The
        // items/vendor and services chains have the same gap and are tracked separately;
        // widen these namespaces as they are fixed.
        string[] namespaces =
        [
            "DOMAIN.Entities.PurchaseOrders",
            "DOMAIN.Entities.Shipments",
            "DOMAIN.Entities.Requisitions",
        ];

        string[] priceProperties =
        [
            "Price",
            "UnitPrice",
            "UnitCost",
            "PricePerUnit",
            "QuotedPrice",
        ];

        var pricedTypes = typeof(PurchaseOrderItemDto)
            .Assembly.GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.Namespace is not null
                && namespaces.Any(ns => t.Namespace.StartsWith(ns, StringComparison.Ordinal))
                && (t.Name.EndsWith("Dto", StringComparison.Ordinal)
                    || t.Name.StartsWith("Create", StringComparison.Ordinal)
                    || t.Name.StartsWith("Update", StringComparison.Ordinal))
            )
            .Where(t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(prop =>
                        priceProperties.Contains(prop.Name)
                        && (prop.PropertyType == typeof(decimal)
                            || prop.PropertyType == typeof(decimal?))
                    )
            )
            .ToList();

        // Guard against the filter silently matching nothing and the test passing vacuously.
        Assert.Contains(typeof(PurchaseOrderItemDto), pricedTypes);
        Assert.Contains(typeof(SupplierQuotationItemDto), pricedTypes);

        var offenders = pricedTypes
            .Where(t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .All(prop => prop.Name != "PriceUoM")
            )
            .Select(t => t.FullName)
            .OrderBy(name => name)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "These carry a price but no PriceUoM, so consumers cannot interpret them:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders)
        );
    }
}
