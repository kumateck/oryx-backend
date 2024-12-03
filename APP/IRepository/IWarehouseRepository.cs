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
    Task<Result<Paginateable<IEnumerable<WarehouseLocationDto>>>> GetWarehouseLocations(int page,
        int pageSize, string searchQuery);
    Task<Result<List<WarehouseLocationDto>>> GetWarehouseLocations();
    Task<Result> UpdateWarehouse(CreateWarehouseRequest request, Guid warehouseId, Guid userId);
    Task<Result> DeleteWarehouse(Guid warehouseId, Guid userId);
}
