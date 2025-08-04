using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.LeaveRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ItemStockRequisitionRepository(ApplicationDbContext context, IMapper mapper) : IItemStockRequisitionRepository
{
    public async Task<Result<Guid>> CreateItemStockRequisition(CreateItemStockRequisitionRequest request)
    {
        var existingItemStockReq =
            await context.ItemStockRequisitions.FirstOrDefaultAsync(nps => nps.RequisitionNo == request.RequisitionNo);

        if (existingItemStockReq != null)
            return Error.Validation("Vendor.Exists", "Vendor already exists");
        
        var validInventoryIds = await context.Items
            .Where(s => request.ItemIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ItemIds.Except(validInventoryIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Items.NotFound", $"Some items not found: {string.Join(", ", missingIds)}");
        
        var itemStockReq = mapper.Map<ItemStockRequisition>(request);
        await context.AddAsync(itemStockReq);
        await context.SaveChangesAsync();
        return itemStockReq.Id;

    }

    public async Task<Result<Paginateable<IEnumerable<ItemStockRequisitionDto>>>> GetItemStockRequisitions(int page, int pageSize, string searchQuery)
    {
        var query =  context.ItemStockRequisitions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.RequisitionNo,
                q => q.Department.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<LeaveStatus>(searchQuery, true, out var status))
            {
                query = query.Where(q => q.Status == status);
            }
        }

        return await PaginationHelper.GetPaginatedResultAsync(query,page, pageSize, mapper.Map<ItemStockRequisitionDto>);
    }

    public async Task<Result<ItemStockRequisitionDto>> GetItemStockRequisition(Guid id)
    {
        var itemStockReq = await context.ItemStockRequisitions
            .Include(u => u.CreatedBy)
            .FirstOrDefaultAsync(isr => isr.Id == id);
        return itemStockReq is null ? 
            Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found") 
            : mapper.Map<ItemStockRequisitionDto>(itemStockReq);
    }

    public async Task<Result> UpdateItemStockRequisition(Guid id, CreateItemStockRequisitionRequest request)
    {
        var itemStockReq = await context.ItemStockRequisitions.FirstOrDefaultAsync(isr => isr.Id == id);
        if (itemStockReq == null) return Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found");

        var validItemIds = await context.Items
            .Where(s => request.ItemIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ItemIds.Except(validItemIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Items.NotFound", $"Some items not found: {string.Join(", ", missingIds)}");

        
        mapper.Map(request, itemStockReq);
        context.ItemStockRequisitions.Update(itemStockReq);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemStockRequisition(Guid id, Guid userId)
    {
        var itemStockReq = await context.ItemStockRequisitions.FirstOrDefaultAsync(isr => isr.Id == id);
        if (itemStockReq == null) return Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found");
        
        itemStockReq.DeletedAt = DateTime.UtcNow;
        itemStockReq.LastDeletedById = userId;
        
        context.ItemStockRequisitions.Update(itemStockReq);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}