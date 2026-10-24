using DOMAIN.Entities.Procurement.Suppliers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Materials;

public static class SupplierPurchaseUnitReadModel
{
    public static async Task<List<SupplierManufacturerDto>> Apply(ApplicationDbContext context,
        Guid supplierId, List<SupplierManufacturerDto> rows)
    {
        var today = DateTime.UtcNow.Date;
        var prices = await context.SupplierPricingAgreements.AsNoTracking().Where(price => price.SupplierId == supplierId
            && price.ChangeKind != SupplierPricingAgreementChangeKind.Archive
            && (price.Status == SupplierPricingAgreementStatus.Pending || price.Status == SupplierPricingAgreementStatus.Approved
                && price.EffectiveFrom <= today && (!price.EffectiveTo.HasValue || price.EffectiveTo.Value >= today)))
            .ToListAsync();
        foreach (var row in rows)
        {
            var matching = prices.Where(price => price.MaterialId == row.Material?.Id).ToList();
            var current = matching.Where(price => price.Status == SupplierPricingAgreementStatus.Approved)
                .Select(price => PurchaseUnitSymbols.Normalize(price.PriceUoM)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (current.Count == 1) row.PurchaseUnit = current[0];
            else row.PurchaseUnit = PurchaseUnitSymbols.Normalize(row.PurchaseUnit ?? row.UoM?.Symbol);
            var pending = matching.Where(price => price.Status == SupplierPricingAgreementStatus.Pending)
                .Select(price => PurchaseUnitSymbols.Normalize(price.PriceUoM)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            row.PendingPurchaseUnit = pending.Count == 1 && !string.Equals(pending[0], row.PurchaseUnit,
                StringComparison.OrdinalIgnoreCase) ? pending[0] : null;
        }
        return rows;
    }
}
