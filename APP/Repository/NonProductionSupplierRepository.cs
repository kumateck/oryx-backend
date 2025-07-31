using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.NonProductionSuppliers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class NonProductionSupplierRepository(ApplicationDbContext context, IMapper mapper) : INonProductionSupplierRepository
{
    public async Task<Result<Guid>> CreateNonProductionSupplier(CreateNonProductionSupplierRequest request)
    {
        var existingSupplier =
            await context.NonProductionSuppliers.FirstOrDefaultAsync(nps => nps.Name == request.Name);

        if (existingSupplier != null)
            return Error.Validation("NonProductionSupplier.Exists", "Non Production Supplier already exists");

        var validInventoryIds = await context.Items
            .Where(s => request.ItemIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ItemIds.Except(validInventoryIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Inventory.NotFound", $"Some inventory not found: {string.Join(", ", missingIds)}");

        var nonProductionSupplier = mapper.Map<NonProductionSupplier>(request);
        await context.AddAsync(nonProductionSupplier);
        await context.SaveChangesAsync();
        return nonProductionSupplier.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<NonProductionSupplierDto>>>> GetNonProductionSuppliers(int page, int pageSize, string searchQuery)
    {
        var query = context.NonProductionSuppliers.AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<NonProductionSupplierDto>);
    }

    public async Task<Result<NonProductionSupplierDto>> GetNonProductionSupplier(Guid id)
    {
        var supplier = await context.NonProductionSuppliers.FirstOrDefaultAsync(nps => nps.Id == id);
        return supplier is null
            ? Error.NotFound("NonProductionSupplier.NotFound", "Non Production Supplier not found")
            : mapper.Map<NonProductionSupplierDto>(supplier);
    }

    public async Task<Result> UpdateNonProductionSupplier(Guid id, CreateNonProductionSupplierRequest request)
    {
        var supplier = await context.NonProductionSuppliers.FirstOrDefaultAsync(nps => nps.Id == id);
        if (supplier == null)
            return Error.NotFound("NonProductionSupplier.NotFound", "Non Production Supplier not found");
        
        var validInventoryIds = await context.Items
            .Where(s => request.ItemIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ItemIds.Except(validInventoryIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Inventory.NotFound", $"Some inventory not found: {string.Join(", ", missingIds)}");
        
        mapper.Map(request, supplier);
        context.NonProductionSuppliers.Update(supplier);
        await context.SaveChangesAsync();
        return Result.Success();

    }

    public async Task<Result> DeleteNonProductionSupplier(Guid id, Guid userId)
    {
        var supplier = await context.NonProductionSuppliers.FirstOrDefaultAsync(nps => nps.Id == id);

        if (supplier == null)
            return Error.NotFound("NonProductionSupplier.NotFound", "Non Production Supplier not found");
        
        supplier.DeletedAt = DateTime.UtcNow;
        supplier.LastDeletedById = userId;
        
        context.NonProductionSuppliers.Update(supplier);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}