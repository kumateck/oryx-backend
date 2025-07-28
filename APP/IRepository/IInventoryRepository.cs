using APP.Utils;
using DOMAIN.Entities.Inventory;
using SHARED;

namespace APP.IRepository;

public interface IInventoryRepository
{
    Task<Result<Guid>> CreateInventory(CreateInventoryRequest request);
    Task<Result<Paginateable<IEnumerable<InventoryDto>>>> GetInventories(int page, int pageSize, string searchQuery);
    Task<Result<InventoryDto>> GetInventory(Guid id);
    Task<Result> UpdateInventory(Guid id, CreateInventoryRequest request);
    Task<Result> DeleteInventory(Guid id, Guid userId);
}