using APP.IRepository;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Materials;

/// <summary>Records quotation prices and proposes unit changes in one transaction.</summary>
public sealed class SupplierQuotationPriceRecorder(ApplicationDbContext context, IApprovalRepository approvalRepository)
{
    public async Task<Result> Record(
        List<SupplierQuotationResponseDto> supplierQuotationResponse,
        Guid supplierQuotationId, Guid? userId = null
    )
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var supplierQuotation = await context
            .SupplierQuotations.AsSplitQuery()
            .Include(s => s.Items)
                .ThenInclude(s => s.Material)
            .Include(s => s.Items)
                .ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .FirstOrDefaultAsync(s => s.Id == supplierQuotationId);

        if (supplierQuotation is null)
        {
            return Error.NotFound(
                "Supplier.Quotation.NotFound",
                "No supplier quotation was found for the specified quotation."
            );
        }

        if (supplierQuotation.Items.Count == 0)
        {
            return Error.Validation(
                "Supplier.Quotation",
                "No items found to mark as quotation sent for the specified supplier."
            );
        }

        var responseLookup = supplierQuotationResponse
            .Where(s => s is not null)
            .GroupBy(s => s.Id)
            .ToDictionary(g => g.Key, g => g.Last());

        if (responseLookup.Keys.Any(id => supplierQuotation.Items.All(item => item.Id != id)))
            return Error.Validation("Supplier.Quotation.Item", "One of the supplied lines does not belong to this quotation.");
        foreach (var group in supplierQuotation.Items.Where(item => responseLookup.ContainsKey(item.Id)).GroupBy(item => item.MaterialId))
            if (group.Select(item => PurchaseUnitSymbols.Normalize(responseLookup[item.Id].PriceUoM)).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                return Error.Validation("Supplier.Quotation.Unit", "Choose one price-unit symbol per material in the quotation.");

        // Every price must arrive with a unit the line can be costed in. Checked for the whole
        // payload before anything changes, so a bad line never leaves the quotation half-priced.
        foreach (var item in supplierQuotation.Items)
        {
            if (!responseLookup.TryGetValue(item.Id, out var response))
                continue;
            if (item.Status != SupplierQuotationItemStatus.NotProcessed)
                return Error.Conflict("Supplier.Quotation.Processed", "Awarded or closed quotation lines cannot be changed.");
            if (response.Price <= 0) return Error.Validation("Supplier.Quotation.Price", "Quoted price must be greater than zero.");
            if (!UomConverter.CanPrice(item.UoM?.Symbol, response.PriceUoM))
                return PriceUoMErrors.Unusable(item.Material?.Name, item.UoM?.Symbol, response.PriceUoM);
        }

        foreach (var item in supplierQuotation.Items)
        {
            if (!responseLookup.TryGetValue(item.Id, out var response)) continue;
            var synchronization = await SupplierPurchaseUnitSync.Request(context, approvalRepository,
                supplierQuotation.SupplierId, item.MaterialId, response.PriceUoM, userId, response.Price);
            if (!synchronization.IsSuccess) return synchronization;
        }

        foreach (var item in supplierQuotation.Items)
        {
            // Only touch items actually present in the payload. Assigning
            // unconditionally would wipe QuotedPrice and PriceUoM off every item a
            // partial re-submit happens to omit.
            if (!responseLookup.TryGetValue(item.Id, out var response))
                continue;

            item.QuotedPrice = response.Price;
            item.PriceUoM = PurchaseUnitSymbols.Normalize(response.PriceUoM);
            item.LastUpdatedById = userId;
            item.UpdatedAt = DateTime.UtcNow;
        }

        // The quotation and its lines are tracked, so only what changed is written. Marking every
        // line as updated would re-save untouched legacy lines, which the price guard refuses.
        supplierQuotation.ReceivedQuotation = true;
        supplierQuotation.LastUpdatedById = userId;
        supplierQuotation.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

}
