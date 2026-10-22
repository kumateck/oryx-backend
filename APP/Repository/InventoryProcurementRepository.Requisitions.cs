using APP.Utils;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.VendorQuotations;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Store purchase requisitions and their sourcing. A requisition draws on one store, can be changed only until its first
/// item is sourced, and each item is sourced once — to trusted vendors or to the open market — after which the
/// requisition completes when every item has been sourced.
/// </summary>
public partial class InventoryProcurementRepository
{
    public async Task<Result<Guid>> CreateInventoryPurchaseRequisition(CreateInventoryPurchaseRequisition request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) return Error.Validation("Requisition.Code", "The requisition needs a code.");
        if (await context.InventoryPurchaseRequisitions.IgnoreQueryFilters().AnyAsync(r => r.Code == request.Code))
            return Error.Validation("Requisition.Exists", "A purchase requisition with this code already exists.");
        var valid = await ValidPurchaseLines(request.Items);
        if (valid.IsFailure) return valid.Error;
        var requisition = new InventoryPurchaseRequisition
        {
            Id = Guid.NewGuid(), Code = request.Code.Trim(), ExpectedDeliveryDate = request.ExpectedDeliveryDate, Remarks = request.Remarks,
            Status = InventoryPurchaseRequisitionStatus.Pending, CreatedById = userId,
            Items = request.Items.Select(i => new InventoryPurchaseRequisitionItem
            {
                Id = Guid.NewGuid(), ItemId = i.ItemId, UoMId = i.UoMId, Quantity = i.Quantity, Status = RequestStatus.Pending, CreatedById = userId
            }).ToList()
        };
        context.InventoryPurchaseRequisitions.Add(requisition);
        await context.SaveChangesAsync();
        return requisition.Id;
    }

    public async Task<Result> UpdateInventoryPurchaseRequisition(Guid id, CreateInventoryPurchaseRequisition request, Guid userId)
    {
        var requisition = await context.InventoryPurchaseRequisitions.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null) return RequisitionErrors.NotFound(id);
        if (await IsSourced(requisition)) return Error.Validation("Requisition.Sourced", "Items of this requisition have been sourced, so it can no longer be changed.");
        var valid = await ValidPurchaseLines(request.Items);
        if (valid.IsFailure) return valid.Error;
        // The code stays as created.
        requisition.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
        requisition.Remarks = request.Remarks;
        requisition.LastUpdatedById = userId;
        context.InventoryPurchaseRequisitionItems.RemoveRange(requisition.Items);
        context.InventoryPurchaseRequisitionItems.AddRange(request.Items.Select(i => new InventoryPurchaseRequisitionItem
        {
            Id = Guid.NewGuid(), InventoryPurchaseRequisitionId = id, ItemId = i.ItemId, UoMId = i.UoMId, Quantity = i.Quantity, Status = RequestStatus.Pending, CreatedById = userId
        }));
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteInventoryPurchaseRequisition(Guid id, Guid userId)
    {
        var requisition = await context.InventoryPurchaseRequisitions.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null) return RequisitionErrors.NotFound(id);
        if (await IsSourced(requisition)) return Error.Validation("Requisition.Sourced", "Items of this requisition have been sourced, so it cannot be deleted.");
        requisition.DeletedAt = DateTime.UtcNow;
        requisition.LastDeletedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<InventoryPurchaseRequisitionDto>> GetInventoryPurchaseRequisition(Guid id)
    {
        var requisition = await context.InventoryPurchaseRequisitions.AsSplitQuery()
            .Include(r => r.CreatedBy)
            .Include(r => r.Items).ThenInclude(i => i.Item).ThenInclude(i => i.UnitOfMeasure)
            .Include(r => r.Items).ThenInclude(i => i.UoM)
            .FirstOrDefaultAsync(r => r.Id == id);
        return requisition == null ? RequisitionErrors.NotFound(id) : mapper.Map<InventoryPurchaseRequisitionDto>(requisition);
    }

    public async Task<Result<Paginateable<IEnumerable<InventoryPurchaseRequisitionDto>>>> GetInventoryPurchaseRequisitions(int page, int pageSize,
        string searchQuery, InventoryPurchaseRequisitionStatus? status, IReadOnlyCollection<Store> readableStores, Store? store)
    {
        var query = context.InventoryPurchaseRequisitions.AsSplitQuery()
            .Include(r => r.CreatedBy)
            .Include(r => r.Items).ThenInclude(i => i.Item)
            .Include(r => r.Items).ThenInclude(i => i.UoM)
            .Where(r => r.Items.Any() && r.Items.All(i => readableStores.Contains(i.Item.Store)));
        if (status.HasValue) query = query.Where(r => r.Status == status);
        if (store.HasValue) query = query.Where(r => r.Items.Any(i => i.Item.Store == store));
        if (!string.IsNullOrWhiteSpace(searchQuery)) query = query.Where(r => r.Code.ToLower().Contains(searchQuery.ToLower()));
        return await PaginationHelper.GetPaginatedResultAsync(query.OrderByDescending(r => r.CreatedAt), page, pageSize, mapper.Map<InventoryPurchaseRequisitionDto>);
    }

    /// <summary>The stores a purchase requisition draws on; empty when it does not exist.</summary>
    public async Task<List<Store>> PurchaseRequisitionStores(Guid id) =>
        await context.InventoryPurchaseRequisitionItems.Where(i => i.InventoryPurchaseRequisitionId == id && context.InventoryPurchaseRequisitions.Any(r => r.Id == id))
            .Select(i => i.Item.Store).Distinct().ToListAsync();

    /// <summary>
    /// Sends requisition items to trusted vendors for quotation. Each item must be on the requisition, not yet sourced, with
    /// at least one vendor; requests to the same vendor that have not been sent yet are batched into one quotation request.
    /// </summary>
    public async Task<Result> CreateSourceRequisition(CreateSourceInventoryRequisition request, Guid userId)
    {
        var requisition = await context.InventoryPurchaseRequisitions.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.InventoryPurchaseRequisitionId);
        if (requisition is null) return RequisitionErrors.NotFound(request.InventoryPurchaseRequisitionId);
        if (requisition.Status != InventoryPurchaseRequisitionStatus.Pending) return Error.Validation("Requisition.Complete", "This requisition has already been sourced.");
        var items = request.Items ?? [];
        if (items.Count == 0) return Error.Validation("Sourcing.Items", "Choose at least one item to source.");
        if (items.GroupBy(i => i.ItemId).Any(g => g.Count() > 1)) return Error.Validation("Sourcing.Items", "Each item can be sourced only once.");
        var lines = new Dictionary<Guid, InventoryPurchaseRequisitionItem>();
        foreach (var item in items)
        {
            var line = requisition.Items.FirstOrDefault(l => l.ItemId == item.ItemId && l.UoMId == item.UoMId);
            if (line == null) return Error.Validation("Sourcing.Item", "An item to source is not on this requisition.");
            if (line.Status is RequestStatus.Sourced or RequestStatus.Completed) return Error.Validation("Sourcing.Sourced", "An item has already been sourced.");
            if (item.Quantity != line.Quantity) return Error.Validation("Sourcing.Quantity", "The quantity to source must be the requisition's quantity.");
            var vendors = (item.Vendors ?? []).Select(v => v.VendorId).ToList();
            if (vendors.Count == 0) return Error.Validation("Sourcing.Vendors", "Choose at least one vendor for every item.");
            if (vendors.Distinct().Count() != vendors.Count) return Error.Validation("Sourcing.Vendors", "Choose each vendor only once per item.");
            lines[item.ItemId] = line;
        }
        var vendorIds = items.SelectMany(i => i.Vendors.Select(v => v.VendorId)).Distinct().ToList();
        if (await context.Vendors.CountAsync(v => vendorIds.Contains(v.Id)) != vendorIds.Count)
            return Error.NotFound("Sourcing.Vendor", "A chosen vendor was not found.");

        foreach (var vendorGroup in items.SelectMany(i => i.Vendors.Select(v => new { i, v.VendorId })).GroupBy(x => x.VendorId))
        {
            var source = await context.SourceInventoryRequisitions.Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.VendorId == vendorGroup.Key && s.SentQuotationRequestAt == null);
            if (source == null)
            {
                source = new SourceInventoryRequisition { Id = Guid.NewGuid(), InventoryPurchaseRequisitionId = requisition.Id, VendorId = vendorGroup.Key, CreatedById = userId };
                context.SourceInventoryRequisitions.Add(source);
            }
            var quotation = await context.VendorQuotations.Include(q => q.Items).FirstOrDefaultAsync(q => q.SourceInventoryRequisitionId == source.Id);
            if (quotation == null)
            {
                quotation = new VendorQuotation { Id = Guid.NewGuid(), VendorId = vendorGroup.Key, SourceInventoryRequisitionId = source.Id, CreatedById = userId };
                context.VendorQuotations.Add(quotation);
            }
            foreach (var entry in vendorGroup)
            {
                source.Items.Add(new SourceInventoryRequisitionItem
                {
                    Id = Guid.NewGuid(), ItemId = entry.i.ItemId, UoMId = entry.i.UoMId, Quantity = entry.i.Quantity, CreatedById = userId
                });
                quotation.Items.Add(new VendorQuotationItem
                {
                    Id = Guid.NewGuid(), ItemId = entry.i.ItemId, UoMId = entry.i.UoMId, Quantity = entry.i.Quantity, CreatedById = userId
                });
            }
        }
        foreach (var line in lines.Values) line.Status = RequestStatus.Sourced;
        CompleteWhenSourced(requisition, userId);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>Sends requisition items to the open market; each item is sourced once, in its requisition quantity.</summary>
    public async Task<Result> CreateMarketRequisition(List<CreateMarketRequisition> requests, Guid userId)
    {
        requests ??= [];
        if (requests.Count == 0) return Error.Validation("Market.Items", "Choose at least one item for the open market.");
        var ids = requests.Select(r => r.InventoryPurchaseRequisitionItemId).ToList();
        if (ids.Distinct().Count() != ids.Count) return Error.Validation("Market.Items", "Each item can be sent to the open market only once.");
        var lines = await context.InventoryPurchaseRequisitionItems.Include(i => i.InventoryPurchaseRequisition).ThenInclude(r => r.Items)
            .Where(i => ids.Contains(i.Id)).ToListAsync();
        if (lines.Count != ids.Count) return Error.NotFound("Requisition.ItemNotFound", "One or more requisition items were not found.");
        foreach (var request in requests)
        {
            var line = lines.Single(l => l.Id == request.InventoryPurchaseRequisitionItemId);
            if (line.InventoryPurchaseRequisition.Status != InventoryPurchaseRequisitionStatus.Pending || line.Status is RequestStatus.Sourced or RequestStatus.Completed)
                return Error.Validation("Market.Sourced", "An item has already been sourced.");
            if (request.ItemId != line.ItemId || request.UoMId != line.UoMId || request.Quantity != line.Quantity)
                return Error.Validation("Market.Item", "An open-market item must match its requisition item and quantity.");
            context.MarketRequisitions.Add(new MarketRequisition
            {
                Id = Guid.NewGuid(), InventoryPurchaseRequisitionItemId = line.Id, ItemId = line.ItemId, UoMId = line.UoMId, Quantity = line.Quantity, CreatedById = userId
            });
            line.Status = RequestStatus.Sourced;
        }
        foreach (var requisition in lines.Select(l => l.InventoryPurchaseRequisition).Distinct()) CompleteWhenSourced(requisition, userId);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private static void CompleteWhenSourced(InventoryPurchaseRequisition requisition, Guid userId)
    {
        if (requisition.Items.All(i => i.Status is RequestStatus.Sourced or RequestStatus.Completed))
        {
            requisition.Status = InventoryPurchaseRequisitionStatus.Complete;
            requisition.LastUpdatedById = userId;
        }
    }

    private async Task<bool> IsSourced(InventoryPurchaseRequisition requisition) =>
        requisition.Status != InventoryPurchaseRequisitionStatus.Pending
        || requisition.Items.Any(i => i.Status is RequestStatus.Sourced or RequestStatus.Completed)
        || await context.SourceInventoryRequisitions.AnyAsync(s => s.InventoryPurchaseRequisitionId == requisition.Id)
        || await context.MarketRequisitions.AnyAsync(m => m.InventoryPurchaseRequisitionItem.InventoryPurchaseRequisitionId == requisition.Id);

    private async Task<Result> ValidPurchaseLines(List<CreateInventoryPurchaseRequisitionItem> lines)
    {
        lines ??= [];
        if (lines.Count == 0) return Error.Validation("Requisition.Items", "Add at least one item.");
        if (lines.GroupBy(i => i.ItemId).Any(g => g.Count() > 1)) return Error.Validation("Requisition.Items", "Each item can appear only once.");
        if (lines.Any(i => i.Quantity <= 0)) return Error.Validation("Requisition.Quantity", "Every quantity must be more than zero.");
        var ids = lines.Select(i => i.ItemId).ToList();
        var items = await context.Items.Where(i => ids.Contains(i.Id)).Select(i => new { i.Id, i.Store, i.IsActive }).ToListAsync();
        if (items.Count != ids.Count) return Error.NotFound("Requisition.Item", "Some items were not found.");
        if (items.Any(i => !i.IsActive)) return Error.Validation("Requisition.Inactive", "Inactive items cannot be requested.");
        if (items.Select(i => i.Store).Distinct().Count() != 1) return Error.Validation("Requisition.Stores", "A purchase requisition draws on one store. Raise one for each store.");
        var units = lines.Select(i => i.UoMId).Distinct().ToList();
        if (await context.UnitOfMeasures.CountAsync(u => units.Contains(u.Id)) != units.Count) return Error.NotFound("Requisition.Unit", "A unit of measure was not found.");
        return Result.Success();
    }
}
