using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using SHARED;

namespace APP.IRepository;

public interface ICollectionRepository
{ 
    Task<Result<Dictionary<string, IEnumerable<CollectionItemDto>>>> GetItemCollection(
        List<string> itemTypes, MaterialKind? materialKind);
    Task<Result<IEnumerable<CollectionItemDto>>> GetItemCollection(string itemType, MaterialKind? materialKind); 
    Task<Result<IEnumerable<UnitOfMeasureDto>>> GetUoM();
    Result<IEnumerable<string>> GetItemTypes();
    Task<Result<Guid>> CreateItem(CreateItemRequest request, string itemType);
    Task<Result<Guid>> UpdateItem(CreateItemRequest request, Guid itemId, string itemType, Guid userId);
    Task<Result> SoftDeleteItem(Guid itemId, string itemType, Guid userId);
    Task<Result<IEnumerable<PackageStyleDto>>> GetPackageStyles();
}