using DOMAIN.Entities.ItemInventoryTransactions;
using SHARED;

namespace APP.IRepository;

public interface IItemInventoryTransactionRepository
{
    Task<Result<ItemInventoryTransactionDto>> ViewInventoryTransaction(Guid id);
}