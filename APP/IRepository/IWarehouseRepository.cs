using APP.Utils;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using SHARED;

namespace APP.IRepository;

public interface IWarehouseRepository
{
    Task<Result<Guid>> CreateWarehouse(CreateWarehouseRequest request, Guid userId);
    Task<Result<WarehouseDto>> GetWarehouse(Guid warehouseId);
    Task<Result<Paginateable<IEnumerable<WarehouseDto>>>> GetWarehouses(int page, int pageSize, string searchQuery);
    Task<Result> UpdateWarehouse(CreateWarehouseRequest request, Guid warehouseId, Guid userId);
    Task<Result> DeleteWarehouse(Guid warehouseId, Guid userId);
    Task<Result<Guid>> CreateWarehouseLocation(CreateWarehouseLocationRequest request, Guid warehouseId,
        Guid userId);
    Task<Result<WarehouseLocationRackDto>> GetWarehouseLocation(Guid locationId);
    Task<Result<Paginateable<IEnumerable<WarehouseLocationDto>>>> GetWarehouseLocations(int page,
        int pageSize, string searchQuery);
    Task<Result<List<WarehouseLocationDto>>> GetWarehouseLocations();
    Task<Result> UpdateWarehouseLocation(CreateWarehouseLocationRequest request, Guid locationId,
        Guid userId);
    Task<Result> DeleteWarehouseLocation(Guid locationId, Guid userId);
    Task<Result<Guid>> CreateWarehouseLocationRack(CreateWarehouseLocationRackRequest request,
        Guid warehouseLocationId, Guid userId);
    Task<Result<WarehouseLocationRackDto>> GetWarehouseLocationRack(Guid rackId);
    Task<Result<Paginateable<IEnumerable<WarehouseLocationRackDto>>>> GetWarehouseLocationRacks(int page,
        int pageSize, string searchQuery);
    Task<Result> UpdateWarehouseLocationRack(CreateWarehouseLocationRackRequest request, Guid rackId,
        Guid userId); 
    Task<Result> DeleteWarehouseLocationRack(Guid rackId, Guid userId);
    Task<Result<Guid>> CreateWarehouseLocationShelf(CreateWarehouseLocationShelfRequest request,
        Guid warehouseLocationRackId, Guid userId);
    Task<Result<WarehouseLocationShelfDto>> GetWarehouseLocationShelf(Guid shelfId);
    Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetWarehouseLocationShelves(
        int page, int pageSize, string searchQuery);
    Task<Result> UpdateWarehouseLocationShelf(CreateWarehouseLocationShelfRequest request, Guid shelfId,
        Guid userId);
    Task<Result> DeleteWarehouseLocationShelf(Guid shelfId, Guid userId);
}
