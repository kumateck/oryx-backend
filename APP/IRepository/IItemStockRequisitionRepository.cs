using APP.Utils;
using DOMAIN.Entities.ItemStockRequisitions;
using SHARED;

namespace APP.IRepository;

public interface IItemStockRequisitionRepository
{
    Task<Result<Guid>> CreateItemStockRequisition(CreateItemStockRequisitionRequest request);
    Task<Result<Paginateable<IEnumerable<ItemStockRequisitionDto>>>> GetItemStockRequisitions(int page, int pageSize, string searchQuery);
    Task<Result<ItemStockRequisitionDto>> GetItemStockRequisition(Guid id);
    Task<Result> UpdateItemStockRequisition(Guid id, CreateItemStockRequisitionRequest request);
    Task<Result> DeleteItemStockRequisition(Guid id, Guid userId);
    Task<Result> IssueStockRequisition(Guid id, IssueStockAgainstRequisitionRequest request);
}