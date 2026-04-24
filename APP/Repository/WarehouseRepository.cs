using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class WarehouseRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IMaterialRepository materialRepository
) : IWarehouseRepository
{
    public async Task<Result<Guid>> CreateWarehouse(CreateWarehouseRequest request)
    {
        var warehouse = mapper.Map<Warehouse>(request);
        await context.Warehouses.AddAsync(warehouse);
        await context.SaveChangesAsync();

        return warehouse.Id;
    }

    public async Task<Result<WarehouseDto>> GetWarehouse(Guid warehouseId)
    {
        var warehouse = await context
            .Warehouses.Include(w => w.Locations)
                .ThenInclude(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Material)
            .Include(w => w.Locations)
                .ThenInclude(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Checklist)
            .FirstOrDefaultAsync(w => w.Id == warehouseId);

        return warehouse is null
            ? Error.NotFound("Warehouse.NotFound", "Warehouse not found")
            : mapper.Map<WarehouseDto>(warehouse);
    }

    public async Task<Result<List<WarehouseDto>>> GetWarehousesByDepartment(Guid departmentId)
    {
        var department = await context
            .Departments.IgnoreAutoIncludes()
            .FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null)
            return Error.NotFound("Department.NotFound", "Department not found");

        var warehouses = await context
            .Warehouses.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(w => w.Locations)
                .ThenInclude(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Material)
            .Include(w => w.Locations)
                .ThenInclude(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Checklist)
            .Where(w => w.DepartmentId == departmentId)
            .ToListAsync();

        if (department.Division == Division.BetaLactam)
        {
            var betaWarehouse = await context
                .Warehouses.AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(w => w.Locations)
                    .ThenInclude(wl => wl.Racks)
                        .ThenInclude(r => r.Shelves)
                            .ThenInclude(s => s.MaterialBatches)
                                .ThenInclude(smb => smb.MaterialBatch)
                                    .ThenInclude(mb => mb.Material)
                .Include(w => w.Locations)
                    .ThenInclude(wl => wl.Racks)
                        .ThenInclude(r => r.Shelves)
                            .ThenInclude(s => s.MaterialBatches)
                                .ThenInclude(smb => smb.MaterialBatch)
                                    .ThenInclude(mb => mb.Checklist)
                .FirstOrDefaultAsync(w =>
                    !w.DepartmentId.HasValue && w.Division == Division.BetaLactam
                );
            warehouses.Add(betaWarehouse);
        }
        else
        {
            var nonBetaWarehouse = await context
                .Warehouses.AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(w => w.Locations)
                    .ThenInclude(wl => wl.Racks)
                        .ThenInclude(r => r.Shelves)
                            .ThenInclude(s => s.MaterialBatches)
                                .ThenInclude(smb => smb.MaterialBatch)
                                    .ThenInclude(mb => mb.Material)
                .Include(w => w.Locations)
                    .ThenInclude(wl => wl.Racks)
                        .ThenInclude(r => r.Shelves)
                            .ThenInclude(s => s.MaterialBatches)
                                .ThenInclude(smb => smb.MaterialBatch)
                                    .ThenInclude(mb => mb.Checklist)
                .FirstOrDefaultAsync(w =>
                    !w.DepartmentId.HasValue && w.Division == Division.NonBetaLactam
                );
            warehouses.Add(nonBetaWarehouse);
        }

        return mapper.Map<List<WarehouseDto>>(warehouses);
    }

    public async Task<Result<Paginateable<IEnumerable<WarehouseDto>>>> GetWarehouses(
        Guid roleId,
        Guid departmentId,
        int page,
        int pageSize,
        string searchQuery,
        WarehouseType? type
    )
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role is null)
            return Error.NotFound("Role.NotFound", "Role not found");

        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null)
            return Error.NotFound("Department.NotFound", "Department not found");

        var query = context
            .Warehouses.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(w => w.Locations)
                .ThenInclude(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Material)
            .Include(w => w.Locations)
                .ThenInclude(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Checklist)
            .Where(w => w.Type != WarehouseType.Production)
            .AsQueryable();

        if (role.Type == DepartmentType.Production)
        {
            query = query.Where(w =>
                w.DepartmentId == departmentId
                || (
                    w.Type == WarehouseType.FinishedGoodsStorage
                    && w.Division == department.Division
                )
            );
        }

        if (type.HasValue)
        {
            query = query.Where(q => q.Type == type);
        }

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

    public async Task<Result> UpdateWarehouse(
        CreateWarehouseRequest request,
        Guid warehouseId,
        Guid userId
    )
    {
        var existingWarehouse = await context.Warehouses.FirstOrDefaultAsync(w =>
            w.Id == warehouseId
        );
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

    public async Task<Result<Guid>> CreateWarehouseLocation(
        CreateWarehouseLocationRequest request,
        Guid warehouseId,
        Guid userId
    )
    {
        var warehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);

        if (warehouse == null)
        {
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");
        }

        var existingLocation = await context.WarehouseLocations.AnyAsync(w =>
            w.WarehouseId == warehouseId
            && w.FloorName == request.FloorName
            && w.Name == request.Name
        );

        if (existingLocation)
            return Error.Conflict(
                "WarehouseLocation.AlreadyExists",
                "Warehouse location already exists"
            );

        var location = mapper.Map<WarehouseLocation>(request);
        location.WarehouseId = warehouseId;
        location.CreatedById = userId;

        await context.WarehouseLocations.AddAsync(location);
        await context.SaveChangesAsync();

        return location.Id;
    }

    public async Task<Result<WarehouseLocationRackDto>> GetWarehouseLocation(Guid locationId)
    {
        var rack = await context
            .WarehouseLocations.AsSplitQuery()
            .Include(r => r.Warehouse)
            .Include(wl => wl.Racks)
                .ThenInclude(r => r.Shelves)
                    .ThenInclude(s => s.MaterialBatches)
                        .ThenInclude(smb => smb.MaterialBatch)
                            .ThenInclude(mb => mb.Material)
            .Include(wl => wl.Racks)
                .ThenInclude(r => r.Shelves)
                    .ThenInclude(s => s.MaterialBatches)
                        .ThenInclude(smb => smb.MaterialBatch)
                            .ThenInclude(mb => mb.Checklist)
            .FirstOrDefaultAsync(r => r.Id == locationId);

        return rack is null
            ? Error.NotFound("WarehouseLocation.NotFound", "Warehouse location not found")
            : mapper.Map<WarehouseLocationRackDto>(rack);
    }

    public async Task<
        Result<Paginateable<IEnumerable<WarehouseLocationDto>>>
    > GetWarehouseLocations(int page, int pageSize, string searchQuery, MaterialKind? kind = null)
    {
        var query = context
            .WarehouseLocations.AsSplitQuery()
            .Include(r => r.Warehouse)
            .Include(wl => wl.Racks)
                .ThenInclude(r => r.Shelves)
                    .ThenInclude(s => s.MaterialBatches)
                        .ThenInclude(smb => smb.MaterialBatch)
                            .ThenInclude(mb => mb.Material)
            .Include(wl => wl.Racks)
                .ThenInclude(r => r.Shelves)
                    .ThenInclude(s => s.MaterialBatches)
                        .ThenInclude(smb => smb.MaterialBatch)
                            .ThenInclude(mb => mb.Checklist)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, w => w.Name);
        }

        if (kind.HasValue)
        {
            var warehouseType =
                kind == MaterialKind.Raw
                    ? WarehouseType.RawMaterialStorage
                    : WarehouseType.PackagedStorage;

            query = query.Where(q => q.Warehouse.Type == warehouseType);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationDto>
        );
    }

    public async Task<Result<List<WarehouseLocationDto>>> GetWarehouseLocations()
    {
        return mapper.Map<List<WarehouseLocationDto>>(
            await context
                .WarehouseLocations.AsSplitQuery()
                .Include(r => r.Warehouse)
                .Include(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Material)
                .Include(wl => wl.Racks)
                    .ThenInclude(r => r.Shelves)
                        .ThenInclude(s => s.MaterialBatches)
                            .ThenInclude(smb => smb.MaterialBatch)
                                .ThenInclude(mb => mb.Checklist)
                .ToListAsync()
        );
    }

    public async Task<Result> UpdateWarehouseLocation(
        CreateWarehouseLocationRequest request,
        Guid locationId,
        Guid userId
    )
    {
        var location = await context.WarehouseLocations.FirstOrDefaultAsync(l =>
            l.Id == locationId
        );

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
        var location = await context.WarehouseLocations.FirstOrDefaultAsync(l =>
            l.Id == locationId
        );

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

    public async Task<Result<Guid>> CreateWarehouseLocationRack(
        CreateWarehouseLocationRackRequest request,
        Guid warehouseLocationId,
        Guid userId
    )
    {
        var location = await context.WarehouseLocations.FirstOrDefaultAsync(w =>
            w.Id == warehouseLocationId
        );

        if (location == null)
        {
            return Error.NotFound("WarehouseLocation.NotFound", "Warehouse location not found");
        }

        var existingWarehouseLocationRacks =
            await context.WarehouseLocationRacks.FirstOrDefaultAsync(lr =>
                lr.Name == request.Name && lr.WarehouseLocationId == warehouseLocationId
            );

        if (existingWarehouseLocationRacks != null)
        {
            return Error.Conflict(
                "WarehouseLocationRack.AlreadyExists",
                "Warehouse location rack already exists"
            );
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
        var rack = await context
            .WarehouseLocationRacks.AsSplitQuery()
            .Include(r => r.WarehouseLocation)
            .Include(r => r.Shelves)
                .ThenInclude(s => s.MaterialBatches)
                    .ThenInclude(smb => smb.MaterialBatch)
                        .ThenInclude(mb => mb.Material)
            .Include(r => r.Shelves)
                .ThenInclude(s => s.MaterialBatches)
                    .ThenInclude(smb => smb.MaterialBatch.Checklist)
            .FirstOrDefaultAsync(r => r.Id == rackId);

        return rack is null
            ? Error.NotFound("WarehouseLocationRack.NotFound", "Warehouse location rack not found")
            : mapper.Map<WarehouseLocationRackDto>(rack);
    }

    public async Task<
        Result<Paginateable<IEnumerable<WarehouseLocationRackDto>>>
    > GetWarehouseLocationRacks(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind = null
    )
    {
        var query = context
            .WarehouseLocationRacks.AsSplitQuery()
            .Include(r => r.WarehouseLocation)
                .ThenInclude(r => r.Warehouse)
            .Include(r => r.Shelves)
                .ThenInclude(s => s.MaterialBatches)
                    .ThenInclude(smb => smb.MaterialBatch)
                        .ThenInclude(mb => mb.Material)
            .Include(r => r.Shelves)
                .ThenInclude(s => s.MaterialBatches)
                    .ThenInclude(smb => smb.MaterialBatch)
                        .ThenInclude(mb => mb.Checklist)
            .AsQueryable();

        if (kind.HasValue)
        {
            var warehouseType =
                kind == MaterialKind.Raw
                    ? WarehouseType.RawMaterialStorage
                    : WarehouseType.PackagedStorage;

            query = query.Where(q => q.WarehouseLocation.Warehouse.Type == warehouseType);
        }

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

    public async Task<Result<List<WarehouseLocationRackDto>>> GetWarehouseLocationRacks(
        MaterialKind kind,
        Guid userId
    )
    {
        var user = await context
            .Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouse =
            kind == MaterialKind.Raw
                ? user.GetUserRawWarehouse()
                : user.GetUserPackagingWarehouse();

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(kind);

        var query = await context
            .WarehouseLocationRacks.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(r => r.WarehouseLocation)
            .Include(r => r.Shelves)
                .ThenInclude(s => s.MaterialBatches)
                    .ThenInclude(smb => smb.MaterialBatch)
                        .ThenInclude(mb => mb.Material)
            .Include(r => r.Shelves)
                .ThenInclude(s => s.MaterialBatches)
                    .ThenInclude(smb => smb.MaterialBatch)
                        .ThenInclude(mb => mb.Checklist)
            .Where(r => r.WarehouseLocation.WarehouseId == warehouse.Id)
            .ToListAsync();

        return mapper.Map<List<WarehouseLocationRackDto>>(query);
    }

    public async Task<Result> UpdateWarehouseLocationRack(
        CreateWarehouseLocationRackRequest request,
        Guid rackId,
        Guid userId
    )
    {
        var rack = await context.WarehouseLocationRacks.FirstOrDefaultAsync(r => r.Id == rackId);

        if (rack is null)
        {
            return Error.NotFound(
                "WarehouseLocationRack.NotFound",
                "Warehouse location rack not found"
            );
        }

        mapper.Map(request, rack);
        rack.LastUpdatedById = userId;

        context.WarehouseLocationRacks.Update(rack);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteWarehouseLocationRack(Guid rackId, Guid userId)
    {
        var rack = await context.WarehouseLocationRacks.FirstOrDefaultAsync(r => r.Id == rackId);

        if (rack is null)
        {
            return Error.NotFound(
                "WarehouseLocationRack.NotFound",
                "Warehouse location rack not found"
            );
        }

        rack.DeletedAt = DateTime.UtcNow;
        rack.LastDeletedById = userId;

        context.WarehouseLocationRacks.Update(rack);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateWarehouseLocationShelf(
        CreateWarehouseLocationShelfRequest request,
        Guid warehouseLocationRackId,
        Guid userId
    )
    {
        var rack = await context.WarehouseLocationRacks.FirstOrDefaultAsync(r =>
            r.Id == warehouseLocationRackId
        );

        if (rack == null)
        {
            return Error.NotFound(
                "WarehouseLocationRack.NotFound",
                "Warehouse location rack not found"
            );
        }

        var existingShelf = await context.WarehouseLocationShelves.FirstOrDefaultAsync(s =>
            s.Name == request.Name && s.WarehouseLocationRackId == warehouseLocationRackId
        );
        if (existingShelf != null)
        {
            return Error.Conflict(
                "WarehouseLocationShelf.AlreadyExists",
                "Warehouse location shelf already exists"
            );
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
        var shelf = await context
            .WarehouseLocationShelves.AsSplitQuery()
            .Include(s => s.WarehouseLocationRack)
                .ThenInclude(s => s.WarehouseLocation)
            .Include(w => w.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Material)
            .Include(w => w.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Checklist)
            .FirstOrDefaultAsync(s => s.Id == shelfId);

        return shelf is null
            ? Error.NotFound(
                "WarehouseLocationShelf.NotFound",
                "Warehouse location shelf not found"
            )
            : mapper.Map<WarehouseLocationShelfDto>(shelf);
    }

    public async Task<
        Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>
    > GetWarehouseLocationShelves(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind = null
    )
    {
        var query = context
            .WarehouseLocationShelves.AsSplitQuery()
            .Include(s => s.WarehouseLocationRack)
                .ThenInclude(s => s.WarehouseLocation)
                    .ThenInclude(s => s.Warehouse)
            .Include(w => w.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Material)
            .Include(w => w.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Checklist)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, s => s.Name, s => s.Description);
        }

        if (kind.HasValue)
        {
            var warehouseType =
                kind == MaterialKind.Raw
                    ? WarehouseType.RawMaterialStorage
                    : WarehouseType.PackagedStorage;

            query = query.Where(q =>
                q.WarehouseLocationRack.WarehouseLocation.Warehouse.Type == warehouseType
            );
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationShelfDto>
        );
    }

    public async Task<Result<List<WarehouseLocationShelfDto>>> GetWarehouseLocationShelves(
        MaterialKind kind,
        Guid userId
    )
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouse =
            kind == MaterialKind.Raw
                ? user.GetUserRawWarehouse()
                : user.GetUserPackagingWarehouse();

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(kind);

        var query = await context
            .WarehouseLocationShelves.Include(s => s.WarehouseLocationRack)
                .ThenInclude(s => s.WarehouseLocation)
                    .ThenInclude(s => s.Warehouse)
            .Include(w => w.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Material)
            .Include(w => w.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Checklist)
            .Where(s => s.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouse.Id)
            .ToListAsync();

        return mapper.Map<List<WarehouseLocationShelfDto>>(query);
    }

    public async Task<Result> UpdateWarehouseLocationShelf(
        CreateWarehouseLocationShelfRequest request,
        Guid shelfId,
        Guid userId
    )
    {
        var shelf = await context.WarehouseLocationShelves.FirstOrDefaultAsync(s =>
            s.Id == shelfId
        );

        if (shelf is null)
        {
            return Error.NotFound(
                "WarehouseLocationShelf.NotFound",
                "Warehouse location shelf not found"
            );
        }

        mapper.Map(request, shelf);
        shelf.LastUpdatedById = userId;

        context.WarehouseLocationShelves.Update(shelf);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteWarehouseLocationShelf(Guid shelfId, Guid userId)
    {
        var shelf = await context.WarehouseLocationShelves.FirstOrDefaultAsync(s =>
            s.Id == shelfId
        );

        if (shelf is null)
        {
            return Error.NotFound(
                "WarehouseLocationShelf.NotFound",
                "Warehouse location shelf not found"
            );
        }

        shelf.DeletedAt = DateTime.UtcNow;
        shelf.LastDeletedById = userId;

        context.WarehouseLocationShelves.Update(shelf);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<WarehouseArrivalLocationDto>> GetArrivalLocationDetails(
        Guid warehouseId
    )
    {
        var warehouse = await context
            .Warehouses.AsSplitQuery()
            .Include(w => w.ArrivalLocation)
                .ThenInclude(al => al.DistributedRequisitionMaterials)
                    .ThenInclude(drm => drm.ShipmentInvoice)
            .Include(w => w.ArrivalLocation)
                .ThenInclude(al => al.DistributedRequisitionMaterials)
                    .ThenInclude(drm => drm.Material)
            .Include(w => w.ArrivalLocation)
                .ThenInclude(al => al.DistributedRequisitionMaterials)
                    .ThenInclude(ss => ss.MaterialItemDistributions)
            .Include(w => w.ArrivalLocation)
                .ThenInclude(al => al.DistributedRequisitionMaterials)
            .Include(w => w.ArrivalLocation)
                .ThenInclude(al => al.DistributedRequisitionMaterials)
                    .ThenInclude(sr => sr.CheckLists)
                        .ThenInclude(cl => cl.MaterialBatches)
            .FirstOrDefaultAsync(w => w.Id == warehouseId);

        if (warehouse == null || warehouse.ArrivalLocation == null)
        {
            return Error.NotFound(
                "Warehouse.ArrivalLocationNotFound",
                "Arrival location not found for the specified warehouse."
            );
        }

        var arrivalLocationDto = mapper.Map<WarehouseArrivalLocationDto>(warehouse.ArrivalLocation);
        return Result.Success(arrivalLocationDto);
    }

    public async Task<
        Result<DistributedRequisitionMaterialDto>
    > GetDistributedRequisitionMaterialById(Guid id)
    {
        var distributedMaterial = await context
            .DistributedRequisitionMaterials.AsSplitQuery()
            .Include(m => m.WarehouseArrivalLocation)
                .ThenInclude(m => m.Warehouse)
                    .ThenInclude(m => m.Department)
            .Include(drm => drm.ShipmentInvoice)
            .Include(drm => drm.Material)
            .Include(drm => drm.WarehouseArrivalLocation)
            .Include(drm => drm.MaterialItemDistributions)
            .Include(sr => sr.CheckLists)
                .ThenInclude(cl => cl.MaterialBatches)
            .FirstOrDefaultAsync(drm => drm.Id == id);

        if (distributedMaterial == null)
        {
            return Error.NotFound(
                "DistributedRequisitionMaterial.NotFound",
                "Distributed requisition material not found"
            );
        }

        return mapper.Map<DistributedRequisitionMaterialDto>(distributedMaterial);
    }

    public async Task<Result<Guid>> CreateArrivalLocation(CreateArrivalLocationRequest request)
    {
        var warehouse = await context.Warehouses.FirstOrDefaultAsync(w =>
            w.Id == request.WarehouseId
        );
        if (warehouse == null)
        {
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");
        }

        var arrivalLocation = mapper.Map<WarehouseArrivalLocation>(request);
        arrivalLocation.WarehouseId = request.WarehouseId;

        await context.WarehouseArrivalLocations.AddAsync(arrivalLocation);
        await context.SaveChangesAsync();

        return Result.Success(arrivalLocation.Id);
    }

    public async Task<Result> UpdateArrivalLocation(UpdateArrivalLocationRequest request)
    {
        var arrivalLocation = await context.WarehouseArrivalLocations.FirstOrDefaultAsync(al =>
            al.Id == request.Id
        );
        if (arrivalLocation == null)
        {
            return Error.NotFound(
                "WarehouseArrivalLocation.NotFound",
                "Arrival location not found"
            );
        }

        mapper.Map(request, arrivalLocation);
        context.WarehouseArrivalLocations.Update(arrivalLocation);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ConfirmArrival(Guid distributedMaterialId)
    {
        var distributedMaterial = await context.DistributedRequisitionMaterials.FirstOrDefaultAsync(
            dm => dm.Id == distributedMaterialId
        );

        if (distributedMaterial == null)
        {
            return Error.NotFound("DistributedMaterial.NotFound", "Distributed material not found");
        }

        distributedMaterial.Status = DistributedRequisitionMaterialStatus.Arrived;
        distributedMaterial.ArrivedAt = DateTime.UtcNow;

        context.DistributedRequisitionMaterials.Update(distributedMaterial);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateChecklist(CreateChecklistRequest request, Guid userId)
    {
        if (request.MaterialBatches.Count == 0)
            return Error.Validation("Material.Batches", "Must have at least one batch.");

        var checklist = mapper.Map<Checklist>(request);
        checklist.CreatedById = userId;
        await context.Checklists.AddAsync(checklist);

        request.MaterialBatches.ForEach(mb => mb.ChecklistId = checklist.Id);
        var result = await materialRepository.CreateMaterialBatchWithoutBatchMovement(
            request.MaterialBatches,
            userId
        );
        if (result.IsFailure)
            return result.Error;

        var distributedMaterial = await context.DistributedRequisitionMaterials.FirstOrDefaultAsync(
            dm => dm.Id == request.DistributedRequisitionMaterialId
        );

        if (distributedMaterial == null)
        {
            return Error.NotFound("DistributedMaterial.NotFound", "Distributed material not found");
        }

        distributedMaterial.Status = DistributedRequisitionMaterialStatus.Checked;
        distributedMaterial.CheckedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Result.Success(checklist.Id);
    }

    public async Task<Result<List<MaterialBatchDto>>> GetMaterialBatchByDistributedMaterial(
        Guid distributedMaterialId
    )
    {
        var checklist = await context
            .Checklists.AsSplitQuery()
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Manufacturer)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Supplier)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.ShipmentInvoice)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Material)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.DistributedRequisitionMaterial)
            .FirstOrDefaultAsync(c => c.DistributedRequisitionMaterialId == distributedMaterialId);

        if (checklist == null)
        {
            return Error.NotFound(
                "Checklist.NotFound",
                "Checklist not found for the specified distributed requisition material."
            );
        }

        var materialBatches = checklist.MaterialBatches.ToList();

        var materialBatchDto = mapper.Map<List<MaterialBatchDto>>(materialBatches);
        return Result.Success(materialBatchDto);
    }

    public async Task<Result<ChecklistDto>> GetChecklistByDistributedMaterialId(
        Guid distributedMaterialId
    )
    {
        var checklist = await context
            .Checklists.AsSplitQuery()
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.SampleWeights)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Material)
            .Include(c => c.Manufacturer)
            .Include(c => c.Supplier)
            .Include(c => c.ShipmentInvoice)
            .FirstOrDefaultAsync(c => c.DistributedRequisitionMaterialId == distributedMaterialId);

        if (checklist == null)
        {
            return Error.NotFound(
                "Checklist.NotFound",
                "Checklist not found for the specified distributed requisition material."
            );
        }

        return Result.Success(mapper.Map<ChecklistDto>(checklist));
    }

    public async Task<Result<List<MaterialBatchDto>>> GetMaterialBatchByDistributedMaterials(
        List<Guid> distributedMaterialIds
    )
    {
        var checklists = await context
            .Checklists.AsSplitQuery()
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Manufacturer)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Supplier)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.ShipmentInvoice)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Material)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.DistributedRequisitionMaterial)
            .Where(c => distributedMaterialIds.Contains(c.DistributedRequisitionMaterialId))
            .ToListAsync();

        if (checklists.Count == 0)
        {
            return Error.NotFound(
                "Checklist.NotFound",
                "Checklists not found for the specified distributed requisition materials."
            );
        }

        var materialBatches = checklists.SelectMany(c => c.MaterialBatches).ToList();

        var materialBatchDto = mapper.Map<List<MaterialBatchDto>>(materialBatches);
        return Result.Success(materialBatchDto);
    }

    public async Task<Result<ChecklistDto>> GetChecklist(Guid id)
    {
        var checklist = await context
            .Checklists.AsSplitQuery()
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.SampleWeights)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (checklist == null)
        {
            return Error.NotFound("Checklist.NotFound", "Checklist not found");
        }

        var checklistDto = mapper.Map<ChecklistDto>(checklist);
        return Result.Success(checklistDto);
    }

    public async Task<Result<Guid>> CreateGrn(CreateGrnRequest request, List<Guid> materialBatchIds)
    {
        var grn = mapper.Map<Grn>(request);
        await context.Grns.AddAsync(grn);
        await context.SaveChangesAsync();

        var materialBatches = await context
            .MaterialBatches.Where(mb => materialBatchIds.Contains(mb.Id))
            .Include(mb => mb.Checklist.DistributedRequisitionMaterial)
            .ToListAsync();

        if (materialBatches.Count != materialBatchIds.Count)
        {
            return Error.NotFound(
                "MaterialBatch.NotFound",
                "One or more material batches not found"
            );
        }

        foreach (var batch in materialBatches)
        {
            batch.GrnId = grn.Id;
            batch.Status = BatchStatus.Quarantine;
            batch.Checklist.DistributedRequisitionMaterial.Status =
                DistributedRequisitionMaterialStatus.GrnGenerated;
            batch.Checklist.DistributedRequisitionMaterial.GrnGeneratedAt = DateTime.UtcNow;
        }

        context.MaterialBatches.UpdateRange(materialBatches);
        await context.SaveChangesAsync();

        return Result.Success(grn.Id);
    }

    public async Task<Result<GrnDto>> GetGrn(Guid id)
    {
        var grn = await context
            .Grns.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Manufacturer)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Supplier)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.ShipmentInvoice)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Material)
            .Include(c => c.MaterialBatches)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(mb => mb.DistributedRequisitionMaterial)
            .Include(mb => mb.CreatedBy)
            .FirstOrDefaultAsync(g => g.Id == id);

        return grn is null
            ? Error.NotFound("Grn.NotFound", "GRN not found")
            : mapper.Map<GrnDto>(grn);
    }

    public async Task<Result<Paginateable<IEnumerable<GrnListDto>>>> GetGrns(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind,
        Status? status
    )
    {
        var query = context
            .Grns.AsSplitQuery()
            .Include(c => c.MaterialBatches)
            .Include(c => c.CreatedBy)
            .AsQueryable();

        if (kind.HasValue)
        {
            query = query.Where(q => q.MaterialBatches.Any(b => b.Material.Kind == kind));
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.MaterialBatches.Any(b => b.Grn.Status == status));
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, w => w.GrnNumber, w => w.CarrierName);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<GrnListDto>
        );
    }

    public async Task<Result<Paginateable<IEnumerable<GrnListDto>>>> GetGrnsForQc(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind,
        Status? status,
        bool? onlyApproved
    )
    {
        var query = context
            .Grns.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(c => c.MaterialBatches)
            .Include(c => c.CreatedBy)
            .AsQueryable();

        if (kind.HasValue)
        {
            query = query.Where(q => q.MaterialBatches.Any(b => b.Material.Kind == kind));
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.MaterialBatches.Any(b => b.Grn.Status == status));
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, w => w.GrnNumber, w => w.CarrierName);
        }

        if (onlyApproved.HasValue)
        {
            if (onlyApproved.Value)
            {
                query = query.Where(q =>
                    q.MaterialBatches.All(m =>
                        m.Status == BatchStatus.Approved || m.Status == BatchStatus.Available
                    )
                );
            }
        }
        else
        {
            query = query.Where(q =>
                !q.MaterialBatches.All(m =>
                    m.Status == BatchStatus.Approved || m.Status == BatchStatus.Available
                )
            );
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<GrnListDto>
        );
    }

    public async Task<Result<Paginateable<IEnumerable<BinCardInformationDto>>>> GetBinCardInformation(int page,
        int pageSize,
        string searchQuery,
        DateTime? date,
        Guid materialId, Guid departmentId)
    {
        var query = context.BinCardInformation
            .AsSplitQuery()
            .Include(bci => bci.MaterialBatch)
            .ThenInclude(mb => mb.Material)
            .Include(bci => bci.MaterialBatch)
            .ThenInclude(mb => mb.Checklist)
            .ThenInclude(c => c.Supplier)
            .Include(bci => bci.MaterialBatch)
            .ThenInclude(mb => mb.Checklist)
            .ThenInclude(c => c.Manufacturer)
            .Include(bci => bci.Product)
            .Include(bci => bci.UoM)
            .Where(bci => bci.MaterialBatch.MaterialId == materialId &&
                          bci.MaterialBatch.Material.Departments.Any(d => d.Id == departmentId))
            .OrderBy(b => b.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                b => b.Description,
                b => b.ArNumber,
                b => b.MaterialBatch.BatchNumber
            );
        }

        if (date.HasValue)
        {
            query = query.Where(bci => bci.CreatedAt == date.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BinCardInformationDto>
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<ProductBinCardInformationDto>>>
    > GetProductBinCardInformation(int page, int pageSize, string searchQuery, Guid productId)
    {
        var query = context
            .ProductBinCardInformation.AsSplitQuery()
            .Include(bci => bci.Batch)
                .ThenInclude(bci => bci.ProductionScheduleProduct)
                    .ThenInclude(p => p.Product)
            .Include(bci => bci.UoM)
            .Where(bci => bci.Batch.ProductionScheduleProduct.ProductId == productId)
            .OrderBy(b => b.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductBinCardInformationDto>
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>
    > GetShelvesByMaterialId(
        int page,
        int pageSize,
        string searchQuery,
        Guid warehouseId,
        Guid materialId
    )
    {
        var query = context
            .WarehouseLocationShelves.AsSplitQuery()
            .Include(s => s.WarehouseLocationRack)
                .ThenInclude(r => r.WarehouseLocation)
            .Include(s => s.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Material)
            .Where(s =>
                s.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouseId
                && s.MaterialBatches.Any(mb => mb.MaterialBatch.MaterialId == materialId)
            )
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationShelfDto>
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>
    > GetShelvesByRackId(int page, int pageSize, string searchQuery, Guid rackId)
    {
        var query = context
            .WarehouseLocationShelves.AsSplitQuery()
            .Include(s => s.WarehouseLocationRack)
                .ThenInclude(r => r.WarehouseLocation)
            .Include(s => s.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Material)
            .Where(s => s.WarehouseLocationRackId == rackId)
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

    public async Task<
        Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>
    > GetShelvesByMaterialBatchId(
        int page,
        int pageSize,
        string searchQuery,
        Guid warehouseId,
        Guid materialBatchId
    )
    {
        var query = context
            .WarehouseLocationShelves.AsSplitQuery()
            .Include(s => s.WarehouseLocationRack)
                .ThenInclude(r => r.WarehouseLocation)
            .Include(s => s.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Material)
            .Where(s =>
                s.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouseId
                && s.MaterialBatches.Any(mb => mb.MaterialBatchId == materialBatchId)
            )
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationShelfDto>
        );
    }

    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetAllShelves(
        int page,
        int pageSize,
        string searchQuery,
        Guid warehouseId
    )
    {
        var query = context
            .WarehouseLocationShelves.AsSplitQuery()
            .Include(s => s.WarehouseLocationRack)
                .ThenInclude(r => r.WarehouseLocation)
            .Include(s => s.MaterialBatches)
                .ThenInclude(smb => smb.MaterialBatch)
                    .ThenInclude(mb => mb.Material)
            .Where(s => s.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouseId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WarehouseLocationShelfDto>
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<DistributedRequisitionMaterialDto>>>
    > GetDistributedRequisitionMaterials(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind kind,
        DistributedRequisitionMaterialStatus? status,
        Guid userId
    )
    {
        var user = await context.Users.AsSplitQuery().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouses = await context
            .Warehouses.IgnoreQueryFilters()
            .AsSplitQuery()
            .Where(w => w.DepartmentId == user.DepartmentId)
            .ToListAsync();

        var rawMaterialWarehouse = warehouses.FirstOrDefault(w =>
            w.Type == WarehouseType.RawMaterialStorage
        );

        if (rawMaterialWarehouse is null)
            return Error.NotFound(
                "Warehouse.Raw",
                "This user has no raw material configured for his department"
            );

        var packageMaterialWarehouse = warehouses.FirstOrDefault(w =>
            w.Type == WarehouseType.PackagedStorage
        );

        if (packageMaterialWarehouse is null)
            return Error.NotFound(
                "Warehouse.Package",
                "This user has no packaging material configured for his department"
            );

        var query = context
            .DistributedRequisitionMaterials.AsSplitQuery()
            .Include(m => m.WarehouseArrivalLocation)
                .ThenInclude(m => m.Warehouse)
                    .ThenInclude(m => m.Department)
            .Include(drm => drm.ShipmentInvoice)
            .Include(drm => drm.Material)
            .Include(drm => drm.UoM)
            .Include(drm => drm.WarehouseArrivalLocation)
            .Include(drm => drm.MaterialItemDistributions)
            .Include(sr => sr.CheckLists)
                .ThenInclude(cl => cl.MaterialBatches)
            .Where(drm => !drm.Status.Equals(DistributedRequisitionMaterialStatus.GrnGenerated))
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        query =
            kind == MaterialKind.Raw
                ? query.Where(q =>
                    q.WarehouseArrivalLocation.WarehouseId == rawMaterialWarehouse.Id
                )
                : query.Where(q =>
                    q.WarehouseArrivalLocation.WarehouseId == packageMaterialWarehouse.Id
                );

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                drm => drm.Material.Name,
                drm => drm.Material.Code
            );
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DistributedRequisitionMaterialDto>
        );
    }

    public async Task<
        Result<DistributedRequisitionMaterialDto>
    > GetDistributedRequisitionMaterialsById(Guid distributedMaterialId)
    {
        return mapper.Map<DistributedRequisitionMaterialDto>(
            await context
                .DistributedRequisitionMaterials.AsSplitQuery()
                .Include(m => m.WarehouseArrivalLocation)
                    .ThenInclude(m => m.Warehouse)
                        .ThenInclude(m => m.Department)
                .Include(drm => drm.ShipmentInvoice)
                .Include(drm => drm.Material)
                .Include(drm => drm.WarehouseArrivalLocation)
                .Include(drm => drm.MaterialItemDistributions)
                .Include(sr => sr.CheckLists)
                    .ThenInclude(cl => cl.MaterialBatches)
                .FirstOrDefaultAsync(drm => drm.Id == distributedMaterialId)
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<DistributedFinishedProductDto>>>
    > GetFinishedGoodsDetails(int page, int pageSize, string searchQuery, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouses = await context
            .Warehouses.Where(w => w.DepartmentId == user.DepartmentId)
            .ToListAsync();

        var finishedGoodsWarehouse = warehouses.FirstOrDefault(w =>
            w.Type == WarehouseType.FinishedGoodsStorage
        );

        if (finishedGoodsWarehouse is null)
            return Error.NotFound(
                "Warehouse.FinishedGoods",
                "This user has no finished goods configured for his department"
            );

        var query = context
            .DistributedFinishedProducts.AsSplitQuery()
            .Include(drm => drm.BatchManufacturingRecord)
            .Include(drm => drm.Product)
            .AsQueryable();

        query = query.Where(q =>
            q.WarehouseArrivalLocation.WarehouseId == finishedGoodsWarehouse.Id
        );

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, drm => drm.Product.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DistributedFinishedProductDto>
        );
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>> GetStockTransferDetails(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind kind,
        Guid userId
    )
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouses = await context
            .Warehouses.Where(w => w.DepartmentId == user.DepartmentId)
            .ToListAsync();

        var rawMaterialWarehouse = warehouses.FirstOrDefault(w =>
            w.Type == WarehouseType.RawMaterialStorage
        );

        if (rawMaterialWarehouse is null)
            return Error.NotFound(
                "Warehouse.Raw",
                "This user has no raw material configured for his department"
            );

        var packageMaterialWarehouse = warehouses.FirstOrDefault(w =>
            w.Type == WarehouseType.PackagedStorage
        );

        if (packageMaterialWarehouse is null)
            return Error.NotFound(
                "Warehouse.Package",
                "This user has no packaging material configured for his department"
            );

        var query = context
            .Warehouses.AsSplitQuery()
            .Include(w => w.ArrivalLocation)
                .ThenInclude(al => al.DistributedStockTransferBatches)
                    .ThenInclude(sb => sb.StockTransfer)
                        .ThenInclude(sts => sts.Sources)
            .Include(w => w.ArrivalLocation)
                .ThenInclude(al => al.DistributedStockTransferBatches)
                    .ThenInclude(sb => sb.Material)
            .AsQueryable();

        query =
            kind == MaterialKind.Raw
                ? query.Where(w =>
                    w.ArrivalLocation.DistributedStockTransferBatches.Any(sb =>
                        sb.Material.Kind == MaterialKind.Raw
                    )
                )
                : query.Where(w =>
                    w.ArrivalLocation.DistributedStockTransferBatches.Any(sb =>
                        sb.Material.Kind == MaterialKind.Package
                    )
                );

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, drm => drm.Department.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialBatchDto>
        );
    }

    public async Task<Result> CreateSwapRequest(CreateSwapRequest request, Guid userId)
    {
        // Validate quantity balance
        if (!request.QuantityIsValid)
            return Error.Validation(
                "Swap.QuantityMismatch",
                "The total quantity to swap between warehouses must be equal."
            );

        // Validate warehouse existence
        var warehouses = await context
            .Warehouses.IgnoreQueryFilters()
            .Where(w => w.Id == request.FirstWarehouseId || w.Id == request.SecondWarehouseId)
            .ToListAsync();

        if (warehouses.Count != 2)
            return Error.NotFound("Warehouse.NotFound", "One or both warehouses do not exist.");

        // Prevent duplicate pending swap request
        var pendingSwaps = await context
            .SwapRequests.IgnoreQueryFilters()
            .Where(s =>
                s.Status == SwapRequestStatus.Pending
                && (
                    (
                        s.FirstWarehouseId == request.FirstWarehouseId
                        && s.SecondWarehouseId == request.SecondWarehouseId
                    )
                    || (
                        s.FirstWarehouseId == request.SecondWarehouseId
                        && s.SecondWarehouseId == request.FirstWarehouseId
                    )
                )
            )
            .Include(s => s.FirstSwapShelfMaterialBatches)
            .Include(s => s.SecondSwapShelfMaterialBatches)
            .ToListAsync();

        var duplicateExists = pendingSwaps.Any(s =>
        {
            var isSameOrder = s.FirstWarehouseId == request.FirstWarehouseId;
            var reqFirst = isSameOrder
                ? request.FirstSwapShelfMaterialBatches
                : request.SecondSwapShelfMaterialBatches;
            var reqSecond = isSameOrder
                ? request.SecondSwapShelfMaterialBatches
                : request.FirstSwapShelfMaterialBatches;

            return s.FirstSwapShelfMaterialBatches.Count == reqFirst.Count
                && s.SecondSwapShelfMaterialBatches.Count == reqSecond.Count
                && s.FirstSwapShelfMaterialBatches.All(b =>
                    reqFirst.Any(r =>
                        r.ShelfMaterialBatchId == b.ShelfMaterialBatchId && r.Quantity == b.Quantity
                    )
                )
                && s.SecondSwapShelfMaterialBatches.All(b =>
                    reqSecond.Any(r =>
                        r.ShelfMaterialBatchId == b.ShelfMaterialBatchId && r.Quantity == b.Quantity
                    )
                );
        });

        if (duplicateExists)
            return Error.Validation(
                "Swap.Duplicate",
                "A pending swap request with the same batches already exists between these warehouses."
            );

        if (request.StockRequisitionId.HasValue)
        {
            var stockRequisition = await context.Requisitions.FirstOrDefaultAsync(r =>
                r.Id == request.StockRequisitionId.Value
            );
            if (stockRequisition == null)
                return Error.NotFound(
                    "StockRequisition.NotFound",
                    "The source requisition does not exist."
                );

            if (stockRequisition.Status == RequestStatus.Completed)
                return Error.Validation(
                    "StockRequisition.Issued",
                    "The source requisition has already been issued."
                );
        }

        // Validate that both warehouses are of the same type
        var firstWarehouse = warehouses.First(w => w.Id == request.FirstWarehouseId);
        var secondWarehouse = warehouses.First(w => w.Id == request.SecondWarehouseId);

        if (firstWarehouse.Type != secondWarehouse.Type)
            return Error.Validation(
                "Swap.WarehouseTypeMismatch",
                "Both warehouses must be of the same type to create a swap request."
            );

        // Validate shelf material batches exist
        var allShelfBatchIds = request
            .FirstSwapShelfMaterialBatches.Select(m => m.ShelfMaterialBatchId)
            .Concat(request.SecondSwapShelfMaterialBatches.Select(m => m.ShelfMaterialBatchId))
            .ToList();

        var existingShelfBatches = await context
            .ShelfMaterialBatches.Where(b => allShelfBatchIds.Contains(b.Id))
            .ToListAsync();

        if (existingShelfBatches.Count != allShelfBatchIds.Count)
            return Error.Validation(
                "Swap.InvalidBatch",
                "Some provided shelf material batches could not be found."
            );

        // Create entity
        var swapRequest = new SwapRequest
        {
            FirstWarehouseId = request.FirstWarehouseId,
            SecondWarehouseId = request.SecondWarehouseId,
            FirstSwapShelfMaterialBatches = request
                .FirstSwapShelfMaterialBatches.Select(m => new SwapShelfMaterialBatch
                {
                    ShelfMaterialBatchId = m.ShelfMaterialBatchId,
                    MaterialBatchId = m.MaterialBatchId,
                    UoMId = m.UoMId,
                    Quantity = m.Quantity,
                })
                .ToList(),
            SecondSwapShelfMaterialBatches = request
                .SecondSwapShelfMaterialBatches.Select(m => new SwapShelfMaterialBatch
                {
                    ShelfMaterialBatchId = m.ShelfMaterialBatchId,
                    MaterialBatchId = m.MaterialBatchId,
                    UoMId = m.UoMId,
                    Quantity = m.Quantity,
                })
                .ToList(),
        };

        await context.SwapRequests.AddAsync(swapRequest);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<SwapRequestDto>>>> GetSwapRequests(
        GetSwapRequestsFilter request
    )
    {
        var query = context
            .SwapRequests.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(s => s.FirstWarehouse)
            .Include(s => s.SecondWarehouse)
            .Include(s => s.FirstSwapShelfMaterialBatches)
                .ThenInclude(b => b.MaterialBatch)
            .Include(s => s.FirstSwapShelfMaterialBatches)
                .ThenInclude(b => b.UoM)
            .Include(s => s.SecondSwapShelfMaterialBatches)
                .ThenInclude(b => b.MaterialBatch)
            .Include(s => s.SecondSwapShelfMaterialBatches)
                .ThenInclude(b => b.UoM)
            .AsQueryable();

        if (request.DepartmentId.HasValue)
        {
            if (request.Direction.HasValue)
            {
                if (request.Direction == SwapRequestDirection.Incoming)
                {
                    query = query.Where(s =>
                        s.SecondWarehouse.DepartmentId == request.DepartmentId
                    );
                }
                else
                {
                    query = query.Where(s => s.FirstWarehouse.DepartmentId == request.DepartmentId);
                }
            }
            else
            {
                query = query.Where(s =>
                    s.FirstWarehouse.DepartmentId == request.DepartmentId
                    || s.SecondWarehouse.DepartmentId == request.DepartmentId
                );
            }
        }

        if (!string.IsNullOrEmpty(request.SearchQuery))
        {
            query = query.WhereSearch(
                request.SearchQuery,
                s => s.FirstWarehouse.Name,
                s => s.SecondWarehouse.Name
            );
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query.OrderByDescending(s => s.CreatedAt),
            request.Page,
            request.PageSize,
            mapper.Map<SwapRequestDto>
        );
    }

    public async Task<Result<SwapRequestDto>> GetSwapRequestDetails(Guid swapRequestId)
    {
        var swapRequest = await context
            .SwapRequests.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(s => s.FirstWarehouse)
            .Include(s => s.SecondWarehouse)
            .Include(s => s.FirstSwapShelfMaterialBatches)
                .ThenInclude(b => b.MaterialBatch)
                .ThenInclude(b => b.Material)
            .Include(s => s.FirstSwapShelfMaterialBatches)
                .ThenInclude(b => b.UoM)
            .Include(s => s.SecondSwapShelfMaterialBatches)
                .ThenInclude(b => b.MaterialBatch)
                .ThenInclude(b => b.Material)
            .Include(s => s.SecondSwapShelfMaterialBatches)
                .ThenInclude(b => b.UoM)
            .Include(b => b.ActionedBy)
            .FirstOrDefaultAsync(s => s.Id == swapRequestId && !s.DeletedAt.HasValue);

        if (swapRequest is null)
            return Error.NotFound("Swap.NotFound", "The requested swap could not be found.");

        return mapper.Map<SwapRequestDto>(swapRequest);
    }

    public async Task<Result> ApproveSwapRequest(Guid swapRequestId, Guid approverId)
    {
        var swapRequest = await context
            .SwapRequests.IgnoreQueryFilters()
            .Include(s => s.FirstWarehouse)
            .Include(s => s.SecondWarehouse)
            .Include(s => s.FirstSwapShelfMaterialBatches)
            .Include(s => s.SecondSwapShelfMaterialBatches)
                .ThenInclude(swapShelfMaterialBatch => swapShelfMaterialBatch.MaterialBatch)
                    .ThenInclude(materialBatch => materialBatch.Material)
            .FirstOrDefaultAsync(s => s.Id == swapRequestId);

        if (swapRequest is null)
            return Error.NotFound("Swap.NotFound", "Swap request not found.");

        if (swapRequest.Status == SwapRequestStatus.Approved)
            return Error.Validation(
                "Swap.AlreadyApproved",
                "This swap request has already been approved."
            );

        if (
            swapRequest.FirstSwapShelfMaterialBatches.Sum(x => x.Quantity)
            != swapRequest.SecondSwapShelfMaterialBatches.Sum(x => x.Quantity)
        )
            return Error.Validation(
                "Swap.QuantityMismatch",
                "The total quantities to swap do not match."
            );

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            string firstWarehouseName = swapRequest.FirstWarehouse?.Name ?? "Unknown Warehouse";
            string secondWarehouseName = swapRequest.SecondWarehouse?.Name ?? "Unknown Warehouse";

            // --- Process First Warehouse → Second Warehouse
            var firstShelfBatchIds = swapRequest
                .FirstSwapShelfMaterialBatches.Select(x => x.ShelfMaterialBatchId)
                .ToList();

            var firstShelfBatches = await context
                .ShelfMaterialBatches.Where(x => firstShelfBatchIds.Contains(x.Id))
                .Include(x => x.WarehouseLocationShelf)
                .ToDictionaryAsync(x => x.Id);

            foreach (var batch in swapRequest.FirstSwapShelfMaterialBatches)
            {
                var shelfBatch = firstShelfBatches[batch.ShelfMaterialBatchId];
                if (shelfBatch.Quantity < batch.Quantity)
                    return Error.Validation(
                        "Swap.InsufficientQuantity",
                        $"Not enough stock in shelf batch {shelfBatch.Id}"
                    );

                shelfBatch.Quantity -= batch.Quantity;

                //  Match by MaterialBatchId to find target shelf in the second warehouse side
                var targetShelfId = swapRequest
                    .SecondSwapShelfMaterialBatches.FirstOrDefault(x =>
                        x.MaterialBatchId == batch.MaterialBatchId
                    )
                    ?.ShelfMaterialBatch.WarehouseLocationShelfId;

                if (targetShelfId == null)
                    return Error.Validation(
                        "Swap.MissingTargetShelf",
                        $"No matching shelf found in second warehouse for material batch {batch.MaterialBatchId}"
                    );

                await context.ShelfMaterialBatches.AddAsync(
                    new ShelfMaterialBatch
                    {
                        WarehouseLocationShelfId = targetShelfId.Value,
                        MaterialBatchId = batch.MaterialBatchId,
                        Quantity = batch.Quantity,
                        UoMId = batch.UoMId,
                        Note = $"Swapped from {firstWarehouseName} → {secondWarehouseName}",
                    }
                );
            }

            // --- Process Second Warehouse → First Warehouse
            var secondShelfBatchIds = swapRequest
                .SecondSwapShelfMaterialBatches.Select(x => x.ShelfMaterialBatchId)
                .ToList();

            var secondShelfBatches = await context
                .ShelfMaterialBatches.Where(x => secondShelfBatchIds.Contains(x.Id))
                .Include(x => x.WarehouseLocationShelf)
                .ToDictionaryAsync(x => x.Id);

            foreach (var batch in swapRequest.SecondSwapShelfMaterialBatches)
            {
                var shelfBatch = secondShelfBatches[batch.ShelfMaterialBatchId];
                if (shelfBatch.Quantity < batch.Quantity)
                    return Error.Validation(
                        "Swap.InsufficientQuantity",
                        $"Not enough stock in shelf batch {shelfBatch.Id}"
                    );

                shelfBatch.Quantity -= batch.Quantity;

                //  Match by MaterialBatchId to find target shelf in the first warehouse side
                var targetShelfId = swapRequest
                    .FirstSwapShelfMaterialBatches.FirstOrDefault(x =>
                        x.MaterialBatchId == batch.MaterialBatchId
                    )
                    ?.ShelfMaterialBatch.WarehouseLocationShelfId;

                if (targetShelfId == null)
                    return Error.Validation(
                        "Swap.MissingTargetShelf",
                        $"No matching shelf found in first warehouse for material batch {batch.MaterialBatchId}"
                    );

                await context.ShelfMaterialBatches.AddAsync(
                    new ShelfMaterialBatch
                    {
                        WarehouseLocationShelfId = targetShelfId.Value,
                        MaterialBatchId = batch.MaterialBatchId,
                        Quantity = batch.Quantity,
                        UoMId = batch.UoMId,
                        Note = $"Swapped from {secondWarehouseName} → {firstWarehouseName}",
                    }
                );
            }

            // --- Mark as approved
            swapRequest.Status = SwapRequestStatus.Approved;
            swapRequest.ActionedById = approverId;
            swapRequest.ActionedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Error.Failure("Swap.ApproveFailed", ex.Message);
        }
    }

    public async Task<Result> RejectSwapRequest(
        Guid swapRequestId,
        Guid approverId,
        string reason = null
    )
    {
        var swapRequest = await context.SwapRequests.FirstOrDefaultAsync(s =>
            s.Id == swapRequestId
        );

        if (swapRequest is null)
            return Error.NotFound("Swap.NotFound", "Swap request not found.");

        if (swapRequest.Status == SwapRequestStatus.Approved)
            return Error.Validation(
                "Swap.AlreadyApproved",
                "Cannot reject an already approved swap request."
            );

        if (swapRequest.Status == SwapRequestStatus.Rejected)
            return Error.Validation(
                "Swap.AlreadyRejected",
                "This swap request has already been rejected."
            );

        swapRequest.Status = SwapRequestStatus.Rejected;
        swapRequest.ActionedById = approverId;
        swapRequest.ActionedAt = DateTime.UtcNow;
        swapRequest.ActionNote = reason ?? "No reason provided.";

        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Failure("Swap.RejectFailed", ex.Message);
        }
    }
}
