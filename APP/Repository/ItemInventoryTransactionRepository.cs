// using APP.IRepository;
// using AutoMapper;
// using DOMAIN.Entities.ItemInventoryTransactions;
// using INFRASTRUCTURE.Context;
// using SHARED;
//
// namespace APP.Repository;
//
// public class ItemInventoryTransactionRepository(ApplicationDbContext context, IMapper mapper) : IItemInventoryTransactionRepository
// {
//     public async Task<Result<ItemInventoryTransactionDto>> ViewInventoryTransaction(Guid id)
//     {
//         var memo = context.ItemInventoryTransactions.FirstOrDefault(i => i.Id == id);
//         return memo is null ? 
//             Error.NotFound("ItemInventoryTransaction.NotFound", "Item Inventory Transaction not found") 
//             : Result.Success(mapper.Map<ItemInventoryTransactionDto>(memo));
//     }
// }