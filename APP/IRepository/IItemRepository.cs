using APP.Utils;
using DOMAIN.Entities.Items;
using SHARED;

namespace APP.IRepository;

public interface IItemRepository
{
    Task<Result<Guid>> CreateItem(CreateItemsRequest request);
    Task<Result<Paginateable<IEnumerable<ItemDto>>>> GetItems(int page, int pageSize, string searchQuery, Store? store);
    Task<Result<ItemDto>> GetItem(Guid id);
    Task<Result> UpdateItem(Guid id, CreateItemsRequest request);
    Task<Result> DeleteItem(Guid id, Guid userId);
}