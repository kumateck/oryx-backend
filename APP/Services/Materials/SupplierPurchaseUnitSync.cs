using APP.IRepository;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Materials;

/// <summary>Synchronizes current defaults; standing price changes use existing pricing approvals.</summary>
public static class SupplierPurchaseUnitSync
{
    public static async Task<Result> Request(ApplicationDbContext context, IApprovalRepository approvals,
        Guid supplierId, Guid materialId, string symbol, Guid? userId, decimal? quotedPrice = null)
    {
        symbol = PurchaseUnitSymbols.Normalize(symbol);
        var links = await context.SupplierManufacturers.Include(row => row.UoM)
            .Where(row => row.SupplierId == supplierId && row.MaterialId == materialId).ToListAsync();
        if (links.Count == 0) return Result.Success();
        var today = DateTime.UtcNow.Date;
        var active = await context.SupplierPricingAgreements.Where(row => row.SupplierId == supplierId
            && row.MaterialId == materialId && row.Status == SupplierPricingAgreementStatus.Approved
            && row.ChangeKind != SupplierPricingAgreementChangeKind.Archive
            && row.EffectiveFrom <= today && (!row.EffectiveTo.HasValue || row.EffectiveTo.Value >= today))
            .Take(2).ToListAsync();
        if (active.Count > 1)
            return Error.Conflict("Supplier.PurchaseUnit", "Multiple current pricing agreements require reconciliation before changing the purchase unit.");
        var current = active.SingleOrDefault();
        var pending = await context.SupplierPricingAgreements.Where(row => row.SupplierId == supplierId
            && row.MaterialId == materialId && row.Status == SupplierPricingAgreementStatus.Pending
            && row.ChangeKind != SupplierPricingAgreementChangeKind.Archive).ToListAsync();
        if (current is null || string.Equals(current.PriceUoM, symbol, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var link in links)
            {
                link.PurchaseUnit = symbol; link.LastUpdatedById = userId; link.UpdatedAt = DateTime.UtcNow;
            }
            return Result.Success();
        }
        if (pending.Any(row => !string.Equals(row.PriceUoM, symbol, StringComparison.OrdinalIgnoreCase)))
            return Error.Conflict("Supplier.PurchaseUnit", "A different pricing-unit change is already awaiting approval.");
        var factor = UomConverter.Convert(1m, symbol, current.PriceUoM);
        if (!factor.HasValue)
            return Error.Validation("Supplier.PurchaseUnit", "The proposed unit cannot be reconciled with standing pricing. A cross-dimension conversion is required first.");
        var price = quotedPrice ?? current.AgreedPrice * factor.Value;
        if (pending.Count > 0)
            return pending.All(row => row.ReplacesAgreementId == current.Id && row.AgreedPrice == price)
                ? Result.Success() : Error.Conflict("Supplier.PurchaseUnit", "A different pricing change is already awaiting approval.");
        if (!userId.HasValue)
            return Error.Validation("Supplier.PurchaseUnit", "An authenticated proposer is required for a pricing-unit change.");
        if (approvals is null)
            return Error.Validation("Supplier.PurchaseUnit", "Pricing approvals are unavailable. No unit change was applied.");
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanManageSupplierContracts))
            return Error.Forbidden("Supplier.PurchaseUnit.Permission", "Managing supplier contracts is required to propose a standing price-unit change.");
        var proposal = new SupplierPricingAgreement {
            Id = Guid.NewGuid(), SupplierId = supplierId, MaterialId = materialId, UoMId = current.UoMId,
            PriceUoM = symbol, AgreedPrice = price, CurrencyId = current.CurrencyId,
            EffectiveFrom = today, EffectiveTo = current.EffectiveTo,
            Status = SupplierPricingAgreementStatus.Pending, ChangeKind = SupplierPricingAgreementChangeKind.Replace,
            ReplacesAgreementId = current.Id, CreatedById = userId, CreatedAt = DateTime.UtcNow,
            Notes = $"Purchase-unit synchronization from {current.PriceUoM} to {symbol}; previous agreement {current.Id}.",
        };
        context.SupplierPricingAgreements.Add(proposal);
        await context.SaveChangesAsync();
        await approvals.CreateInitialApprovalsAsync(nameof(SupplierPricingAgreement), proposal.Id);
        return Result.Success();
    }

    public static async Task ApplyApproved(ApplicationDbContext context, SupplierPricingAgreement proposal, Guid? userId)
    {
        if (proposal.ChangeKind == SupplierPricingAgreementChangeKind.Archive || proposal.EffectiveFrom.Date > DateTime.UtcNow.Date) return;
        var links = await context.SupplierManufacturers.Where(row => row.SupplierId == proposal.SupplierId
            && row.MaterialId == proposal.MaterialId).ToListAsync();
        foreach (var link in links)
        {
            link.PurchaseUnit = PurchaseUnitSymbols.Normalize(proposal.PriceUoM);
            link.LastUpdatedById = userId; link.UpdatedAt = DateTime.UtcNow;
        }
    }
}
