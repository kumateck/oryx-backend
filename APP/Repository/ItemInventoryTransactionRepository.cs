using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.ItemInventoryTransactions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ItemInventoryTransactionRepository(ApplicationDbContext context, IMapper mapper) : IItemInventoryTransactionRepository
{
    public async Task<Result<ItemInventoryTransactionDto>> ViewInventoryTransaction(Guid id)
    {
        var memo = await context.ItemInventoryTransactions.FirstOrDefaultAsync(i => i.MemoId == id);
        return memo is null ? 
            Error.NotFound("ItemInventoryTransaction.NotFound", "Item Inventory Transaction not found") 
            : Result.Success(mapper.Map<ItemInventoryTransactionDto>(memo));
    }
}