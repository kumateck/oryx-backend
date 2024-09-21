using DOMAIN.Entities.Base;
using SHARED;

namespace APP.IRepository;

public interface ICollectionRepository
{
    Task<Result<IEnumerable<CollectionItemDto>>> GetItemCollection(string itemType);
    Result<IEnumerable<string>> GetItemTypes();
    Task<Result<Guid>> CreateItem(CreateItemRequest request, string itemType);
    Task<Result<Guid>> UpdateItem(CreateItemRequest request, Guid itemId, string itemType, Guid userId);
    Task<Result> SoftDeleteItem(Guid itemId, string itemType, Guid userId);
}