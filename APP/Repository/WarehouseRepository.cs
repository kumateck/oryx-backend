using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class WarehouseRepository(ApplicationDbContext context, IMapper mapper) : IWarehouseRepository
{
    public async Task<Result<Guid>> CreateWarehouse(CreateWarehouseRequest request, Guid userId)
    {
        var warehouse = mapper.Map<Warehouse>(request);
        warehouse.CreatedById = userId;
        await context.Warehouses.AddAsync(warehouse);
        await context.SaveChangesAsync();

        return warehouse.Id;
    }

    public async Task<Result<WarehouseDto>> GetWarehouse(Guid warehouseId)
    {
        var warehouse = await context.Warehouses
            .Include(w => w.Locations)
            .FirstOrDefaultAsync(w => w.Id == warehouseId);

        return warehouse is null
            ? Error.NotFound("Warehouse.NotFound", "Warehouse not found")
            : mapper.Map<WarehouseDto>(warehouse);
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseDto>>>> GetWarehouses(int page, int pageSize, string searchQuery)
    {
        var query = context.Warehouses
            .Include(w => w.Locations)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, w => w.Name, w => w.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseDto>
        );
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationDto>>>> GetWarehouseLocations(int page, int pageSize, string searchQuery)
    {
        var query = context.WarehouseLocations
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, w => w.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationDto>
        );
    }
    
    public async Task<Result<List<WarehouseLocationDto>>>  GetWarehouseLocations()
    {
        return mapper.Map<List<WarehouseLocationDto>>(await context.WarehouseLocations.ToListAsync());
    }
    
    public async Task<Result> UpdateWarehouse(CreateWarehouseRequest request, Guid warehouseId, Guid userId)
    {
        var existingWarehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (existingWarehouse is null)
        {
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");
        }

        mapper.Map(request, existingWarehouse);
        existingWarehouse.LastUpdatedById = userId;

        context.Warehouses.Update(existingWarehouse);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Warehouse (soft delete)
    public async Task<Result> DeleteWarehouse(Guid warehouseId, Guid userId)
    {
        var warehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (warehouse is null)
        {
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");
        }

        warehouse.DeletedAt = DateTime.UtcNow;
        warehouse.LastDeletedById = userId;

        context.Warehouses.Update(warehouse);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
