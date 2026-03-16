using APP.Utils;
using DOMAIN.Entities.ItemGrns;
using SHARED;

namespace APP.IRepository;

public interface IItemGrnRepository
{
    Task<Result<Guid>> CreateItemGrn(CreateItemGrnRequest request);
    Task<Result<ItemGrnDto>> GetItemGrn(Guid id);
    Task<Result<Paginateable<IEnumerable<ItemGrnDto>>>> GetItemGrns(int page, int pageSize, string searchQuery);
    Task<Result> UpdateItemGrn(Guid id, CreateItemGrnRequest request);
    Task<Result> DeleteItemGrn(Guid id, Guid userId);

}