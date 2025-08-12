// using APP.Extensions;
// using APP.IRepository;
// using DOMAIN.Entities.ItemInventoryTransactions;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace API.Controllers;
//
// [ApiController]
// [Route("api/v{version:apiVersion}/item-inventory-transactions")]
// [Authorize]
// public class ItemInventoryTransactionController(IItemInventoryTransactionRepository repository) : ControllerBase
// {
//     [HttpGet]
//     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemInventoryTransactionDto))]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     public async Task<IResult> GetItemInventoryTransactions([FromRoute] Guid id)
//     {
//         var result = await repository.ViewInventoryTransaction(id);
//         return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails() ;
//     }
// }