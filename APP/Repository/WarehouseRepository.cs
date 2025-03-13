using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class WarehouseRepository(ApplicationDbContext context, IMapper mapper, IMaterialRepository materialRepository) : IWarehouseRepository
{
    public async Task<Result<Guid>> CreateWarehouse(CreateWarehouseRequest request, Guid userId)
    {
        var warehouse = mapper.Map<Warehouse>(request);
        await context.Warehouses.AddAsync(warehouse);
        await context.SaveChangesAsync();

        return warehouse.Id;
    }

    public async Task<Result<WarehouseDto>> GetWarehouse(Guid warehouseId)
    {
        var warehouse = await context.Warehouses
            .Include(w => w.Locations)
            .ThenInclude(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(w => w.Locations)
            .ThenInclude(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .FirstOrDefaultAsync(w => w.Id == warehouseId);

        return warehouse is null
            ? Error.NotFound("Warehouse.NotFound", "Warehouse not found")
            : mapper.Map<WarehouseDto>(warehouse);
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseDto>>>> GetWarehouses(int page, int pageSize, string searchQuery, WarehouseType? type)
    {
        var query = context.Warehouses
            .Include(w => w.Locations)
            .ThenInclude(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(w => w.Locations)
            .ThenInclude(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .Where(w => w.Type != WarehouseType.Production)
            .AsQueryable();

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
            .Include(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .FirstOrDefaultAsync(r => r.Id == locationId);

        return rack is null
            ? Error.NotFound("WarehouseLocation.NotFound", "Warehouse location not found")
            : mapper.Map<WarehouseLocationRackDto>(rack);
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationDto>>>> GetWarehouseLocations(int page, int pageSize, string searchQuery)
    {
        var query = context.WarehouseLocations
            .Include(r => r.Warehouse)
            .Include(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
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
        return mapper.Map<List<WarehouseLocationDto>>(await context.WarehouseLocations.Include(r => r.Warehouse)
            .Include(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(wl=>wl.Racks)
            .ThenInclude(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .ToListAsync());
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
            .Include(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .Include(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .FirstOrDefaultAsync(r => r.Id == rackId);

        return rack is null
            ? Error.NotFound("WarehouseLocationRack.NotFound", "Warehouse location rack not found")
            : mapper.Map<WarehouseLocationRackDto>(rack);
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationRackDto>>>> GetWarehouseLocationRacks(int page, int pageSize, string searchQuery, MaterialKind? kind = null)
    {
        var query = context.WarehouseLocationRacks
            .Include(r => r.WarehouseLocation)
            .Include(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .AsQueryable();

        if (kind.HasValue)
        {
            var warehouseType = kind == MaterialKind.Raw
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
    
    public async Task<Result<List<WarehouseLocationRackDto>>> GetWarehouseLocationRacks(MaterialKind kind, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouse = kind == MaterialKind.Raw ? user.GetUserRawWarehouse() : user.GetUserPackagingWarehouse();

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(kind);
        
        var query = await context.WarehouseLocationRacks
            .Include(r => r.WarehouseLocation)
            .Include(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(r=>r.Shelves)
            .ThenInclude(s=>s.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .Where(r => r.WarehouseLocation.WarehouseId == warehouse.Id)
            .ToListAsync();


        return mapper.Map<List<WarehouseLocationRackDto>>(query);
    }


    public async Task<Result> UpdateWarehouseLocationRack(CreateWarehouseLocationRackRequest request, Guid rackId, Guid userId)
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
            .Include(s => s.WarehouseLocationRack).ThenInclude(s => s.WarehouseLocation)
            .Include(w=>w.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(w=>w.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .FirstOrDefaultAsync(s => s.Id == shelfId);

        return shelf is null
            ? Error.NotFound("WarehouseLocationShelf.NotFound", "Warehouse location shelf not found")
            : mapper.Map<WarehouseLocationShelfDto>(shelf);
    }
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetWarehouseLocationShelves(int page, int pageSize, string searchQuery)
    {
        var query = context.WarehouseLocationShelves
            .Include(s => s.WarehouseLocationRack)
            .ThenInclude(s => s.WarehouseLocation).ThenInclude(s => s.Warehouse)
            .Include(w=>w.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(w=>w.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
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
    
    public async Task<Result<List<WarehouseLocationShelfDto>>> GetWarehouseLocationShelves(MaterialKind kind, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouse = kind == MaterialKind.Raw ? user.GetUserRawWarehouse() : user.GetUserPackagingWarehouse();

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(kind);
        
        var query =  await context.WarehouseLocationShelves
            .Include(s => s.WarehouseLocationRack)
            .ThenInclude(s => s.WarehouseLocation).ThenInclude(s => s.Warehouse)
            .Include(w=>w.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Include(w=>w.MaterialBatches)
            .ThenInclude(smb=>smb.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .Where(s => s.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouse.Id)
            .ToListAsync();

        return mapper.Map<List<WarehouseLocationShelfDto>>(query);
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

    public async Task<Result<WarehouseArrivalLocationDto>> GetArrivalLocationDetails(Guid warehouseId)
    {
        var warehouse = await context.Warehouses
            .Include(w => w.ArrivalLocation)
            .ThenInclude(al => al.DistributedRequisitionMaterials)
            .ThenInclude(drm => drm.ShipmentInvoice)
            .Include(w => w.ArrivalLocation)
            .ThenInclude(al => al.DistributedRequisitionMaterials)
            .Include(w => w.ArrivalLocation)
            .ThenInclude(al => al.DistributedRequisitionMaterials)
            .Include(w => w.ArrivalLocation)
            .ThenInclude(al => al.DistributedRequisitionMaterials)
            .ThenInclude(drm => drm.Material)
            .Include(w => w.ArrivalLocation)
            .ThenInclude(al => al.DistributedRequisitionMaterials)
            .ThenInclude(ss=>ss.MaterialItemDistributions)
            .Include(w => w.ArrivalLocation)
            .ThenInclude(al => al.DistributedRequisitionMaterials)
            .ThenInclude(sr=>sr.RequisitionItem)
            .FirstOrDefaultAsync(w => w.Id == warehouseId);

        if (warehouse == null || warehouse.ArrivalLocation == null)
        {
            return Error.NotFound("Warehouse.ArrivalLocationNotFound", "Arrival location not found for the specified warehouse.");
        }

        var arrivalLocationDto = mapper.Map<WarehouseArrivalLocationDto>(warehouse.ArrivalLocation);
        return Result.Success(arrivalLocationDto);
    }
    
    public async Task<Result<Paginateable<IEnumerable<DistributedRequisitionMaterialDto>>>> GetDistributedRequisitionMaterials(Guid warehouseId,int page, int pageSize, string searchQuery)
    {
        try
        {
            var query = context.DistributedRequisitionMaterials
                .Include(drm => drm.ShipmentInvoice)
                .Include(drm => drm.Material)
                .Include(drm => drm.RequisitionItem)
                .Include(drm=>drm.MaterialItemDistributions)
                .Where(drm => drm.WarehouseArrivalLocation.WarehouseId == warehouseId && !drm.Status.Equals(DistributedRequisitionMaterialStatus.GrnGenerated))
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.WhereSearch(searchQuery, drm => drm.Material.Name);
            }

            return await PaginationHelper.GetPaginatedResultAsync(
                query,
                page,
                pageSize,
                mapper.Map<DistributedRequisitionMaterialDto>
            );
        }
        catch (Exception e)
        {
            return Result.Failure<Paginateable<IEnumerable<DistributedRequisitionMaterialDto>>>(Error.Failure("500",e.Message));
        }
    }
    
    public async Task<Result<DistributedRequisitionMaterialDto>> GetDistributedRequisitionMaterialById(Guid id)
    {
        var distributedMaterial = await context.DistributedRequisitionMaterials
            .Include(drm => drm.ShipmentInvoice)
            .Include(drm => drm.MaterialItemDistributions)
            .Include(drm => drm.Material)
            .Include(drm => drm.RequisitionItem)
            .FirstOrDefaultAsync(drm => drm.Id == id);

        if (distributedMaterial == null)
        {
            return Error.NotFound("DistributedRequisitionMaterial.NotFound", "Distributed requisition material not found");
        }

        return mapper.Map<DistributedRequisitionMaterialDto>(distributedMaterial);
    }
    
    public async Task<Result<Guid>> CreateArrivalLocation(CreateArrivalLocationRequest request)
    {
        var warehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId);
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
        var arrivalLocation = await context.WarehouseArrivalLocations.FirstOrDefaultAsync(al => al.Id == request.Id);
        if (arrivalLocation == null)
        {
            return Error.NotFound("WarehouseArrivalLocation.NotFound", "Arrival location not found");
        }

        mapper.Map(request, arrivalLocation);
        context.WarehouseArrivalLocations.Update(arrivalLocation);
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    public async Task<Result> ConfirmArrival(Guid distributedMaterialId)
    {
        var distributedMaterial = await context.DistributedRequisitionMaterials
            .FirstOrDefaultAsync(dm => dm.Id == distributedMaterialId);

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
        var checklist = mapper.Map<Checklist>(request);
        checklist.CreatedById = userId;
        await context.Checklists.AddAsync(checklist);
        
        request.MaterialBatches.ForEach(mb => mb.ChecklistId = checklist.Id);
        await materialRepository.CreateMaterialBatchWithoutBatchMovement(request.MaterialBatches, userId);

        var distributedMaterial = await context.DistributedRequisitionMaterials
            .FirstOrDefaultAsync(dm => dm.Id == request.DistributedRequisitionMaterialId);

        if (distributedMaterial == null)
        {
            return Error.NotFound("DistributedMaterial.NotFound", "Distributed material not found");
        }

        distributedMaterial.Status = DistributedRequisitionMaterialStatus.Checked;
        distributedMaterial.CheckedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Result.Success(checklist.Id);
    }

    public async Task<Result<List<MaterialBatchDto>>> GetMaterialBatchByDistributedMaterial(Guid distributedMaterialId)
    {
        var checklist = await context.Checklists
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Manufacturer)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Supplier)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.ShipmentInvoice)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Material)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.DistributedRequisitionMaterial)
            .FirstOrDefaultAsync(c => c.DistributedRequisitionMaterialId == distributedMaterialId);

        if (checklist == null)
        {
            return Error.NotFound("Checklist.NotFound", "Checklist not found for the specified distributed requisition material.");
        }

        var materialBatches = checklist.MaterialBatches.ToList();

        var materialBatchDto = mapper.Map<List<MaterialBatchDto>>(materialBatches);
        return Result.Success(materialBatchDto);
    }
    
    public async Task<Result<ChecklistDto>> GetChecklistByDistributedMaterialId(Guid distributedMaterialId)
    {
        var checklist = await context.Checklists
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb => mb.Material)
            .Include(c => c.Manufacturer)
            .Include(c => c.Supplier)
            .Include(c => c.ShipmentInvoice)
            .FirstOrDefaultAsync(c => c.DistributedRequisitionMaterialId == distributedMaterialId);
    
        if (checklist == null)
        {
            return Error.NotFound("Checklist.NotFound", "Checklist not found for the specified distributed requisition material.");
        }
    
        return Result.Success(mapper.Map<ChecklistDto>(checklist));
    }
    
    public async Task<Result<List<MaterialBatchDto>>> GetMaterialBatchByDistributedMaterials(List<Guid> distributedMaterialIds)
    {
        var checklists = await context.Checklists
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Manufacturer)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Supplier)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.ShipmentInvoice)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Material)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.DistributedRequisitionMaterial)
            .Where(c => distributedMaterialIds.Contains(c.DistributedRequisitionMaterialId))
            .ToListAsync();

        if (!checklists.Any())
        {
            return Error.NotFound("Checklist.NotFound", "Checklists not found for the specified distributed requisition materials.");
        }

        var materialBatches = checklists.SelectMany(c => c.MaterialBatches).ToList();

        var materialBatchDto = mapper.Map<List<MaterialBatchDto>>(materialBatches);
        return Result.Success(materialBatchDto);
    }

    public async Task<Result<ChecklistDto>> GetChecklist(Guid id)
    {
        var checklist = await context.Checklists
            .AsSplitQuery()
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.SampleWeights)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (checklist == null)
        {
            return Error.NotFound("Checklist.NotFound", "Checklist not found");
        }

        var checklistDto = mapper.Map<ChecklistDto>(checklist);
        return Result.Success(checklistDto);
    }
    
    public async Task<Result<Guid>> CreateGrn(CreateGrnRequest request, List<Guid> materialBatchIds, Guid userId)
    {
        var grn = mapper.Map<Grn>(request);
        await context.Grns.AddAsync(grn);
        await context.SaveChangesAsync();

        var materialBatches = await context.MaterialBatches
            .Where(mb => materialBatchIds.Contains(mb.Id))
            .Include(mb=>mb.Checklist.DistributedRequisitionMaterial)
            .ToListAsync();

        if (materialBatches.Count != materialBatchIds.Count)
        {
            return Error.NotFound("MaterialBatch.NotFound", "One or more material batches not found");
        }

        foreach (var batch in materialBatches)
        {
            batch.GrnId = grn.Id;
            batch.Status = BatchStatus.Quarantine;
            batch.Checklist.DistributedRequisitionMaterial.Status = DistributedRequisitionMaterialStatus.GrnGenerated;
            batch.Checklist.DistributedRequisitionMaterial.GrnGeneratedAt = DateTime.UtcNow;
        }

        context.MaterialBatches.UpdateRange(materialBatches);
        await context.SaveChangesAsync();

        return Result.Success(grn.Id);
    }
    
    public async Task<Result<GrnDto>> GetGrn(Guid id)
    {
        var grn = await context.Grns
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Manufacturer)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Supplier)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.ShipmentInvoice)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Material)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.DistributedRequisitionMaterial)
            .FirstOrDefaultAsync(g => g.Id == id);

        return grn is null
            ? Error.NotFound("Grn.NotFound", "GRN not found")
            : mapper.Map<GrnDto>(grn);
    }
    
    public async Task<Result<Paginateable<IEnumerable<GrnDto>>>> GetGrns(int page, int pageSize, string searchQuery)
    {
        var query = context.Grns
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Manufacturer)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Supplier)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.ShipmentInvoice)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Material)
            .Include(c => c.MaterialBatches)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.DistributedRequisitionMaterial)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, w => w.GrnNumber, w => w.CarrierName);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<GrnDto>
        );
    }
    
    public async Task<Result<Paginateable<IEnumerable<BinCardInformationDto>>>> GetBinCardInformation(int page, int pageSize, string searchQuery, Guid materialId)
    {
        var query = context.BinCardInformation
            .Include(bci => bci.MaterialBatch)
            .ThenInclude(mb => mb.Material)
            .Include(bci => bci.Product)
            .Include(bci => bci.UoM)
            .Where(bci => bci.MaterialBatch.MaterialId == materialId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BinCardInformationDto>
        );
    }
    
    public async Task<Result<Paginateable<IEnumerable<ProductBinCardInformationDto>>>> GetProductBinCardInformation(int page, int pageSize, string searchQuery, Guid productId)
    {
        var query = context.ProductBinCardInformation
            .Include(bci => bci.Batch)
            .Include(bci => bci.Product)
            .Include(bci => bci.UoM)
            .Where(bci => bci.Batch.Id == productId)
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
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetShelvesByMaterialId(int page, int pageSize, string searchQuery,Guid warehouseId, Guid materialId)
    {
        var query =  context.WarehouseLocationShelves
            .Include(s=>s.WarehouseLocationRack)
            .ThenInclude(r=>r.WarehouseLocation)
            .Include(s => s.MaterialBatches)
            .ThenInclude(smb => smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Where(s => s.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouseId && s.MaterialBatches.Any(mb => mb.MaterialBatch.MaterialId == materialId))
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
    
    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetShelvesByRackId(int page, int pageSize, string searchQuery, Guid rackId)
    {
        var query = context.WarehouseLocationShelves
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

    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetShelvesByMaterialBatchId(int page, int pageSize, string searchQuery,Guid warehouseId, Guid materialBatchId)
    {
        var query =  context.WarehouseLocationShelves
            .Include(s=>s.WarehouseLocationRack)
            .ThenInclude(r=>r.WarehouseLocation)
            .Include(s => s.MaterialBatches)
            .ThenInclude(smb => smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
            .Where(s => s.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouseId && s.MaterialBatches.Any(mb => mb.MaterialBatchId == materialBatchId))
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

    public async Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetAllShelves(int page, int pageSize, string searchQuery,Guid warehouseId)
    {
        var query =  context.WarehouseLocationShelves
            .Include(s=>s.WarehouseLocationRack)
            .ThenInclude(r=>r.WarehouseLocation)
            .Include(s => s.MaterialBatches)
            .ThenInclude(smb => smb.MaterialBatch)
            .ThenInclude(mb=>mb.Material)
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

    public async Task<Result<Paginateable<IEnumerable<DistributedRequisitionMaterialDto>>>> GetDistributedRequisitionMaterials(int page, int pageSize, string searchQuery, MaterialKind kind, Guid userId)
    {

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouses = await context.Warehouses.Where(w => w.DepartmentId == user.DepartmentId).ToListAsync();

        var rawMaterialWarehouse = warehouses.FirstOrDefault(w => w.Type == WarehouseType.RawMaterialStorage);

        if (rawMaterialWarehouse is null)
            return Error.NotFound("Warehouse.Raw", "This user has no raw material configured for his department");

        var packageMaterialWarehouse = warehouses.FirstOrDefault(w => w.Type == WarehouseType.PackagedStorage);
        
        if (packageMaterialWarehouse is null)
            return Error.NotFound("Warehouse.Package", "This user has no packaging material configured for his department");

        
        var query = context.DistributedRequisitionMaterials
            .Include(drm => drm.ShipmentInvoice)
            .Include(drm => drm.Material)
            .Include(drm => drm.RequisitionItem)
            .Include(drm => drm.WarehouseArrivalLocation)
            .Include(drm=>drm.MaterialItemDistributions)
            .Where(drm => !drm.Status.Equals(DistributedRequisitionMaterialStatus.GrnGenerated))
            .AsQueryable();

        query = kind == MaterialKind.Raw
            ? query.Where(q => q.WarehouseArrivalLocation.WarehouseId == rawMaterialWarehouse.Id)
            : query.Where(q => q.WarehouseArrivalLocation.WarehouseId == packageMaterialWarehouse.Id);

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, drm => drm.Material.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DistributedRequisitionMaterialDto>
        );
    }

    public async Task<Result<DistributedRequisitionMaterialDto>> GetDistributedRequisitionMaterialsById(
        Guid distributedMaterialId)
    {
        return mapper.Map<DistributedRequisitionMaterialDto>(await context.DistributedRequisitionMaterials
            .Include(drm => drm.ShipmentInvoice)
            .Include(drm => drm.Material)
            .Include(drm => drm.RequisitionItem)
            .Include(drm => drm.WarehouseArrivalLocation)
            .Include(drm => drm.MaterialItemDistributions)
            .FirstOrDefaultAsync(drm => drm.Id == distributedMaterialId));
    }
}
    
    

