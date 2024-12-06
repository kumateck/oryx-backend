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
    
    public async Task<Result<Guid>> CreateWarehouseLocation(CreateWarehouseLocationRequest request, Guid warehouseId, Guid userId)
    {
        var warehouse = await context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == warehouseId);

        if (warehouse == null)
        {
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");
        }

        var location = mapper.Map<WarehouseLocation>(request);
        location.WarehouseId = warehouseId;
        location.CreatedById = userId;

        await context.WarehouseLocations.AddAsync(location);
        await context.SaveChangesAsync();

        return location.Id;
    }
    
    public async Task<Result<WarehouseLocationRackDto>> GetWarehouseLocation(Guid locationId)
    {
        var rack = await context.WarehouseLocations
            .Include(r => r.Warehouse)
            .FirstOrDefaultAsync(r => r.Id == locationId);

        return rack is null
            ? Error.NotFound("WarehouseLocation.NotFound", "Warehouse location not found")
            : mapper.Map<WarehouseLocationRackDto>(rack);
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
    
    public async Task<Result> UpdateWarehouseLocation(CreateWarehouseLocationRequest request, Guid locationId, Guid userId)
    {
        var location = await context.WarehouseLocations
            .FirstOrDefaultAsync(l => l.Id == locationId);

        if (location is null)
        {
            return Error.NotFound("WarehouseLocation.NotFound", "Warehouse location not found");
        }

        mapper.Map(request, location);
        location.LastUpdatedById = userId;

        context.WarehouseLocations.Update(location);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteWarehouseLocation(Guid locationId, Guid userId)
    {
        var location = await context.WarehouseLocations
            .FirstOrDefaultAsync(l => l.Id == locationId);

        if (location is null)
        {
            return Error.NotFound("WarehouseLocation.NotFound", "Warehouse location not found");
        }

        location.DeletedAt = DateTime.UtcNow;
        location.LastDeletedById = userId;

        context.WarehouseLocations.Update(location);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    
    public async Task<Result<Guid>> CreateWarehouseLocationRack(CreateWarehouseLocationRackRequest request, Guid warehouseLocationId, Guid userId)
    {
        var location = await context.WarehouseLocations
            .FirstOrDefaultAsync(w => w.Id == warehouseLocationId);

        if (location == null)
        {
            return Error.NotFound("WarehouseLocation.NotFound", "Warehouse location not found");
        }

        var rack = mapper.Map<WarehouseLocationRack>(request);
        rack.WarehouseLocationId = warehouseLocationId;
        rack.CreatedById = userId;

        await context.WarehouseLocationRacks.AddAsync(rack);
        await context.SaveChangesAsync();

        return rack.Id;
    }

    public async Task<Result<WarehouseLocationRackDto>> GetWarehouseLocationRack(Guid rackId)
    {
        var rack = await context.WarehouseLocationRacks
            .Include(r => r.WarehouseLocation)
            .FirstOrDefaultAsync(r => r.Id == rackId);

        return rack is null
            ? Error.NotFound("WarehouseLocationRack.NotFound", "Warehouse location rack not found")
            : mapper.Map<WarehouseLocationRackDto>(rack);
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationRackDto>>>> GetWarehouseLocationRacks(int page, int pageSize, string searchQuery)
    {
        var query = context.WarehouseLocationRacks
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, r => r.Name, r => r.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationRackDto>
        );
    }


    public async Task<Result> UpdateWarehouseLocationRack( CreateWarehouseLocationRackRequest request, Guid rackId, Guid userId)
    {
        var rack = await context.WarehouseLocationRacks
            .FirstOrDefaultAsync(r => r.Id == rackId);

        if (rack is null)
        {
            return Error.NotFound("WarehouseLocationRack.NotFound", "Warehouse location rack not found");
        }

        mapper.Map(request, rack);
        rack.LastUpdatedById = userId;

        context.WarehouseLocationRacks.Update(rack);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteWarehouseLocationRack(Guid rackId, Guid userId)
    {
        var rack = await context.WarehouseLocationRacks
            .FirstOrDefaultAsync(r => r.Id == rackId);

        if (rack is null)
        {
            return Error.NotFound("WarehouseLocationRack.NotFound", "Warehouse location rack not found");
        }

        rack.DeletedAt = DateTime.UtcNow;
        rack.LastDeletedById = userId;

        context.WarehouseLocationRacks.Update(rack);
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateWarehouseLocationShelf(CreateWarehouseLocationShelfRequest request, Guid warehouseLocationRackId, Guid userId)
    {
        var rack = await context.WarehouseLocationRacks
            .FirstOrDefaultAsync(r => r.Id == warehouseLocationRackId);

        if (rack == null)
        {
            return Error.NotFound("WarehouseLocationRack.NotFound", "Warehouse location rack not found");
        }

        var shelf = mapper.Map<WarehouseLocationShelf>(request);
        shelf.WarehouseLocationRackId = warehouseLocationRackId;
        shelf.CreatedById = userId;

        await context.WarehouseLocationShelves.AddAsync(shelf);
        await context.SaveChangesAsync();

        return shelf.Id;
    }

    public async Task<Result<WarehouseLocationShelfDto>> GetWarehouseLocationShelf(Guid shelfId)
    {
        var shelf = await context.WarehouseLocationShelves
            .Include(s => s.WarehouseLocationRack)
            .FirstOrDefaultAsync(s => s.Id == shelfId);

        return shelf is null
            ? Error.NotFound("WarehouseLocationShelf.NotFound", "Warehouse location shelf not found")
            : mapper.Map<WarehouseLocationShelfDto>(shelf);
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetWarehouseLocationShelves(int page, int pageSize, string searchQuery)
    {
        var query = context.WarehouseLocationShelves
            .Include(s => s.WarehouseLocationRack)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, s => s.Name, s => s.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationShelfDto>
        );
    }

    public async Task<Result> UpdateWarehouseLocationShelf(CreateWarehouseLocationShelfRequest request, Guid shelfId, Guid userId)
    {
        var shelf = await context.WarehouseLocationShelves
            .FirstOrDefaultAsync(s => s.Id == shelfId);

        if (shelf is null)
        {
            return Error.NotFound("WarehouseLocationShelf.NotFound", "Warehouse location shelf not found");
        }

        mapper.Map(request, shelf);
        shelf.LastUpdatedById = userId;

        context.WarehouseLocationShelves.Update(shelf);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteWarehouseLocationShelf(Guid shelfId, Guid userId)
    {
        var shelf = await context.WarehouseLocationShelves
            .FirstOrDefaultAsync(s => s.Id == shelfId);

        if (shelf is null)
        {
            return Error.NotFound("WarehouseLocationShelf.NotFound", "Warehouse location shelf not found");
        }

        shelf.DeletedAt = DateTime.UtcNow;
        shelf.LastDeletedById = userId;

        context.WarehouseLocationShelves.Update(shelf);
        await context.SaveChangesAsync();

        return Result.Success();
    }

}
