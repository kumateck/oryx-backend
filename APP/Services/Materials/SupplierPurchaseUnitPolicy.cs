using DOMAIN.Entities.Procurement.Suppliers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Materials;

/// <summary>Validates symbols and retains a compatible quantity-unit reference for legacy consumers.</summary>
public static class SupplierPurchaseUnitPolicy
{
    public static async Task<Result> Normalize(ApplicationDbContext context,
        CreateSupplierRequest request, Guid? supplierId)
    {
        var existing = supplierId.HasValue ? await context.SupplierManufacturers.Include(row => row.UoM)
            .Where(row => row.SupplierId == supplierId).ToListAsync() : [];
        var registered = await context.UnitOfMeasures.AsNoTracking().ToListAsync();
        foreach (var group in request.AssociatedManufacturers.GroupBy(row => row.MaterialId))
        {
            if (!group.Key.HasValue) return Error.Validation("Supplier.PurchaseUnit", "A material is required.");
            var linked = await new MaterialPurchaseUnitsReader(context).Read(group.Key.Value);
            var allowed = PurchaseUnitSymbols.Expand(linked.Select(unit => unit.Symbol));
            var prior = existing.FirstOrDefault(row => row.MaterialId == group.Key);
            var priorSymbol = PurchaseUnitSymbols.Normalize(prior?.PurchaseUnit ?? prior?.UoM?.Symbol);
            var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in group)
            {
                var explicitSymbol = !string.IsNullOrWhiteSpace(row.PurchaseUnit);
                var symbol = PurchaseUnitSymbols.Normalize(explicitSymbol ? row.PurchaseUnit
                    : registered.FirstOrDefault(unit => unit.Id == row.UoMId)?.Symbol);
                if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 20)
                    return Error.Validation("Supplier.PurchaseUnit", "Choose a purchase-unit symbol.");
                if (explicitSymbol && !allowed.Contains(symbol, StringComparer.OrdinalIgnoreCase)
                    && !string.Equals(symbol, priorSymbol, StringComparison.OrdinalIgnoreCase))
                    return Error.Validation("Supplier.PurchaseUnit", "Purchase unit is not linked to this material in any department.");
                symbols.Add(symbol);
                row.PurchaseUnit = symbol;
                var reference = registered.FirstOrDefault(unit => unit.Id == (prior?.UoMId ?? row.UoMId)
                    && UomConverter.CanPrice(unit.Symbol, symbol))
                    ?? registered.FirstOrDefault(unit => string.Equals(unit.Symbol?.Trim(), symbol, StringComparison.OrdinalIgnoreCase))
                    ?? registered.FirstOrDefault(unit => linked.Any(link => link.Id == unit.Id) && UomConverter.CanPrice(unit.Symbol, symbol));
                if (reference is null)
                    return Error.Validation("Supplier.PurchaseUnit", "No compatible material quantity unit is configured.");
                row.UoMId = reference.Id;
            }
            if (symbols.Count != 1)
                return Error.Validation("Supplier.PurchaseUnit", "All manufacturers for one supplier material must share its purchase unit.");
        }
        return Result.Success();
    }
}
