using APP.Utils;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using SHARED;

namespace APP.IRepository;

public interface IItemStockRequisitionRepository
{
    Task<Result<Guid>> CreateItemStockRequisition(CreateItemStockRequisitionRequest request, Guid actor);
    Task<Result<Paginateable<IEnumerable<ItemStockRequisitionDto>>>> GetItemStockRequisitions(int page, int pageSize, string searchQuery,
        IReadOnlyCollection<Store> readableStores, Store? store, IssueItemStockRequisitionStatus? status);
    Task<Result<ItemStockRequisitionDto>> GetItemStockRequisition(Guid id);
    /// <summary>The stores a requisition draws on; empty when it does not exist.</summary>
    Task<List<Store>> StoresOf(Guid id);
    Task<Result> UpdateItemStockRequisition(Guid id, CreateItemStockRequisitionRequest request);
    Task<Result> DeleteItemStockRequisition(Guid id, Guid userId);
    Task<Result> IssueStock(Guid id, IssueStockAgainstRequisitionRequest request, Guid actor);
}
