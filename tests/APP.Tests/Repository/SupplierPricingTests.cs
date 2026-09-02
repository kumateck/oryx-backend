using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using Xunit;

namespace APP.Tests.Repository;

public class SupplierPricingTests
{
    private static readonly DateTime Boundary = new(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ActiveAgreement_IsInclusiveAtEffectiveToBoundary()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        context.SupplierPricingAgreements.Add(Agreement(ids, Boundary.AddMonths(-1), Boundary, 12m));
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);

        var result = await repository.GetActivePricingAgreement(
            ids.SupplierId, ids.MaterialId, ids.UomId, Boundary);

        Assert.True(result.IsSuccess);
        Assert.Equal(12m, result.Value.AgreedPrice);
    }

    [Fact]
    public async Task ActiveAgreement_ReturnsConflictWhenLegacyDataIsAmbiguous()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        context.SupplierPricingAgreements.AddRange(
            Agreement(ids, Boundary.AddMonths(-2), null, 12m),
            Agreement(ids, Boundary.AddMonths(-1), null, 14m)
        );
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);

        var result = await repository.GetActivePricingAgreement(
            ids.SupplierId, ids.MaterialId, ids.UomId, Boundary);

        Assert.False(result.IsSuccess);
    }

    private static PricingIds SeedReferenceData(INFRASTRUCTURE.Context.ApplicationDbContext context)
    {
        var ids = new PricingIds();
        context.AddRange(
            new Supplier { Id = ids.SupplierId, Name = "Supplier" },
            new Material { Id = ids.MaterialId, Name = "Material" },
            new UnitOfMeasure { Id = ids.UomId, Name = "Kilogram", Symbol = "kg" },
            new Currency { Id = ids.CurrencyId, Name = "US Dollar", Symbol = "$" }
        );
        return ids;
    }

    private static SupplierPricingAgreement Agreement(
        PricingIds ids, DateTime from, DateTime? to, decimal price) => new()
    {
        Id = Guid.NewGuid(), SupplierId = ids.SupplierId, MaterialId = ids.MaterialId,
        UoMId = ids.UomId, CurrencyId = ids.CurrencyId, AgreedPrice = price,
        PriceUoM = "kg", EffectiveFrom = from, EffectiveTo = to,
    };

    private sealed class PricingIds
    {
        public Guid SupplierId { get; } = Guid.NewGuid();
        public Guid MaterialId { get; } = Guid.NewGuid();
        public Guid UomId { get; } = Guid.NewGuid();
        public Guid CurrencyId { get; } = Guid.NewGuid();
    }
}
