using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using Xunit;

namespace APP.Tests.Repository;

public class SupplierPricingAssociationTests
{
    [Fact]
    public async Task NewAgreement_RejectsMaterialUnitOutsideSupplierAssociation()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var supplierId = Guid.NewGuid();
        var manufacturerId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var associatedUomId = Guid.NewGuid();
        var unassociatedUomId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        context.AddRange(
            new Supplier { Id = supplierId, Name = "Supplier" },
            new Manufacturer { Id = manufacturerId, Name = "Manufacturer" },
            new Material { Id = materialId, Name = "Material" },
            new UnitOfMeasure { Id = associatedUomId, Name = "Kilogram", Symbol = "kg" },
            new UnitOfMeasure { Id = unassociatedUomId, Name = "Gram", Symbol = "g" },
            new Currency { Id = currencyId, Name = "US Dollar", Symbol = "$" },
            new SupplierManufacturer { Id = Guid.NewGuid(), SupplierId = supplierId,
                ManufacturerId = manufacturerId, MaterialId = materialId,
                UoMId = associatedUomId });
        await context.SaveChangesAsync();

        var request = new SupplierPricingAgreementRequest {
            MaterialId = materialId, UoMId = unassociatedUomId,
            CurrencyId = currencyId, AgreedPrice = 20m, PriceUoM = "g",
            EffectiveFrom = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var result = await SupplierRelationshipTestContext.CreateRepository(context)
            .CreatePricingAgreement(supplierId, request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Empty(context.SupplierPricingAgreements);
    }
}
