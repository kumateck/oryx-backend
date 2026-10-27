using DOMAIN.Entities.Memos;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.StockEntries;
using DOMAIN.Entities.VendorQuotations;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Vendor quotations and the memos raised from the chosen prices. A quote is priced while it is unused, is put on one memo
/// at most, and choosing it retires the competing quotes for the same item; paying a memo item receives its quantity.
/// </summary>
public partial class InventoryProcurementRepository
{
    public async Task<Result> ReceiveQuotationFromVendor(List<VendorQuotationResponseDto> vendorQuotationResponse, Guid vendorQuotationId, Guid userId)
    {
        if (vendorQuotationResponse == null || vendorQuotationResponse.Count == 0)
            return Error.Validation("Vendor.Quotation", "No quotation responses provided.");
        if (vendorQuotationResponse.GroupBy(r => r.Id).Any(g => g.Count() > 1))
            return Error.Validation("Vendor.Quotation", "Each quoted item can be priced only once.");
        if (vendorQuotationResponse.Any(r => r.Price <= 0))
            return Error.Validation("Vendor.Quotation.Price", "Every quoted price must be more than zero.");
        var quotation = await context.VendorQuotations.Include(q => q.Items).FirstOrDefaultAsync(q => q.Id == vendorQuotationId);
        if (quotation == null) return Error.NotFound("Vendor.Quotation", "Vendor quotation not found.");
        if (quotation.Items.Count == 0) return Error.Validation("Vendor.Quotation", "No items found for this quotation.");
        if (quotation.Items.Any(i => i.Status != VendorQuotationItemStatus.NotProcessed))
            return Error.Validation("Vendor.Quotation.Used", "Prices from this quotation have already been used or set aside, so they can no longer change.");
        var responses = vendorQuotationResponse.ToDictionary(r => r.Id);
        if (responses.Keys.Except(quotation.Items.Select(i => i.Id)).Any())
            return Error.Validation("Vendor.Quotation.Item", "A priced item is not on this quotation.");
        foreach (var item in quotation.Items)
        {
            if (!responses.TryGetValue(item.Id, out var response))
                return Error.Validation("Vendor.Quotation.MissingItem", "Enter a price for every item on the quotation.");
            item.QuotedPrice = response.Price;
            item.PriceUoM = response.PriceUoM;
            item.LastUpdatedById = userId;
        }
        quotation.ReceivedQuotation = true;
        quotation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ProcessTrustedVendorMemo(List<CreateMemoItem> memos, Guid userId)
    {
        memos ??= [];
        if (memos.Count == 0 || memos.Any(m => m.VendorQuotationItemId == null))
            return Error.Validation("Memo.Items", "Choose a quoted price for every memo item.");
        var ids = memos.Select(m => m.VendorQuotationItemId!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count) return Error.Validation("Memo.Items", "Each quoted price can be used once.");
        var quotes = await context.VendorQuotationItems.Include(q => q.VendorQuotation).Where(q => ids.Contains(q.Id)).ToListAsync();
        if (quotes.Count != ids.Count) return Error.NotFound("VendorQuotationItem", "A chosen quotation item was not found.");
        if (memos.GroupBy(m => new { m.ItemId, m.UoMId }).Any(g => g.Count() > 1))
            return Error.Validation("Memo.Items", "Choose one vendor's price for each item.");

        var memo = new Memo { Id = Guid.NewGuid(), Code = await GenerateMemoCode(), CreatedById = userId, Items = [] };
        foreach (var request in memos)
        {
            var quote = quotes.Single(q => q.Id == request.VendorQuotationItemId);
            if (quote.Status != VendorQuotationItemStatus.NotProcessed || !quote.VendorQuotation.ReceivedQuotation || quote.QuotedPrice is not > 0)
                return Error.Validation("Memo.Quote", "A chosen price has already been used, or has not been received from the vendor.");
            if (request.ItemId != quote.ItemId || request.UoMId != quote.UoMId || request.Quantity != quote.Quantity)
                return Error.Validation("ItemRequest", "A memo item does not match its vendor quotation.");
            memo.Items.Add(new MemoItem
            {
                Id = Guid.NewGuid(), VendorQuotationItemId = quote.Id, ItemId = quote.ItemId, UoMId = quote.UoMId, Quantity = quote.Quantity,
                PricePerUnit = quote.QuotedPrice.Value, CreatedById = userId
            });
            quote.Status = VendorQuotationItemStatus.Processed;
            // The other vendors' quotes for the same request are set aside so they are not chosen again.
            var competing = await context.VendorQuotationItems.Where(q => q.Id != quote.Id && q.ItemId == quote.ItemId && q.UoMId == quote.UoMId
                && q.Quantity == quote.Quantity && q.Status == VendorQuotationItemStatus.NotProcessed).ToListAsync();
            foreach (var other in competing) other.Status = VendorQuotationItemStatus.NotUsed;
        }
        await ApplyThreshold(memo);
        context.Memos.Add(memo);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ProcessOpenMarketMemo(List<CreateMemoItem> memos, Guid userId)
    {
        memos ??= [];
        if (memos.Count == 0 || memos.Any(m => m.MarketRequisitionVendorId == null))
            return Error.Validation("Memo.Items", "Choose an open-market price for every memo item.");
        var ids = memos.Select(m => m.MarketRequisitionVendorId!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count) return Error.Validation("Memo.Items", "Each open-market price can be used once.");
        var offers = await context.MarketRequisitionVendors.Include(v => v.MarketRequisition).Where(v => ids.Contains(v.Id)).ToListAsync();
        if (offers.Count != ids.Count) return Error.Validation("MarketRequisitionVendor", "A chosen open-market price was not found.");
        if (offers.GroupBy(o => o.MarketRequisitionId).Any(g => g.Count() > 1))
            return Error.Validation("Memo.Items", "Choose one vendor for each open-market item.");

        var memo = new Memo { Id = Guid.NewGuid(), Code = await GenerateMemoCode(), CreatedById = userId, Items = [] };
        foreach (var request in memos)
        {
            var offer = offers.Single(o => o.Id == request.MarketRequisitionVendorId);
            if (offer.Complete || await context.MemoItems.AnyAsync(m => m.MarketRequisitionVendor.MarketRequisitionId == offer.MarketRequisitionId))
                return Error.Validation("Memo.Offer", "This open-market item already has a memo.");
            var market = offer.MarketRequisition;
            if (request.ItemId != market.ItemId || request.UoMId != market.UoMId || request.Quantity != market.Quantity)
                return Error.Validation("ItemRequest", "A memo item does not match its open-market requisition.");
            // One memo item per chosen price (the earlier code added each item twice).
            memo.Items.Add(new MemoItem
            {
                Id = Guid.NewGuid(), MarketRequisitionVendorId = offer.Id, ItemId = market.ItemId, UoMId = market.UoMId, Quantity = market.Quantity,
                PricePerUnit = offer.PricePerUnit, CreatedById = userId
            });
            var vendors = await context.MarketRequisitionVendors.Where(v => v.MarketRequisitionId == offer.MarketRequisitionId).ToListAsync();
            foreach (var vendor in vendors) vendor.Complete = true;
        }
        await ApplyThreshold(memo);
        context.Memos.Add(memo);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ConfirmMarketRequisitionVendor(Guid marketRequisitionVendorId)
    {
        var vendor = await context.MarketRequisitionVendors.FirstOrDefaultAsync(v => v.Id == marketRequisitionVendorId);
        if (vendor is null) return RequisitionErrors.NotFound(marketRequisitionVendorId);
        if (vendor.Complete) return Error.Validation("MarketRequisitionVendor.Complete", "This open-market price has already been confirmed.");
        vendor.Complete = true;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Records payment for a memo item and sends its quantity to the store's receiving area, attributed to the payer so the
    /// receipt can be approved by someone else. The memo is paid once all its items are.
    /// </summary>
    public async Task<Result> MarkMemoItemAsPaid(Guid memoItemId, Guid userId, DateTime? purchasedAt = null)
    {
        var memoItem = await context.MemoItems.Include(mi => mi.Memo).FirstOrDefaultAsync(mi => mi.Id == memoItemId);
        if (memoItem is null) return Error.NotFound("MemoItem", "Memo item not found.");
        if (memoItem.PurchasedAt.HasValue) return Error.Validation("MemoItem", "Memo item is already marked as paid.");
        memoItem.PurchasedAt = purchasedAt ?? DateTime.UtcNow;
        memoItem.LastUpdatedById = userId;
        // Each paid memo item is its own receipt, so two lines of the same item are both received.
        context.StockEntries.Add(new StockEntry
        {
            Id = Guid.NewGuid(), ItemId = memoItem.ItemId, MemoId = memoItem.MemoId, Quantity = memoItem.Quantity, CreatedById = userId
        });
        if (await context.MemoItems.Where(mi => mi.MemoId == memoItem.MemoId && mi.Id != memoItemId).AllAsync(mi => mi.PurchasedAt.HasValue))
            memoItem.Memo.Paid = true;
        return await APP.Services.Inventory.InventoryCommit.Save(context);
    }

    private async Task ApplyThreshold(Memo memo)
    {
        var threshold = await context.Threshold.SingleOrDefaultAsync();
        if (threshold != null && memo.Items.Sum(i => i.Quantity * i.PricePerUnit) > threshold.MemoThreshold)
            memo.Status = MemoStatus.PurchaseOrder;
    }
}
