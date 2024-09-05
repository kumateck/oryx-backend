using DOMAIN.Entities.Base;
using SHARED;

namespace APP.IRepository;

public interface ICollectionRepository
{
    Task<Result<IEnumerable<CollectionItemDto>>> GetItemCollection(string itemType);
    Result<IEnumerable<string>> GetItemTypes();
    Task<Result<Guid>> CreateItem(CreateItemRequest request, string itemType);
}