using API.Authorization;
using Microsoft.EntityFrameworkCore;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.ItemGrns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/item-grns")]
[ApiController]
[Authorize]
public class ItemGrnController(IItemGrnRepository repository, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{

    /// <summary>
    /// Creates an item grn.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateItemGrn)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItemGrn([FromBody] CreateItemGrnRequest request)
    {
        if (!await MayReceive(request.ItemId)) return TypedResults.Forbid();

        var result = await repository.CreateItemGrn(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of item grns based on search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemGrnDto>>))]
    public async Task<IResult> GetItemGrns([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {

        var result = await repository.GetItemGrns(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    /// <summary>
    /// Retrieves the details of a specific item grn
    /// </summary>

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemGrnDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemGrn([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetItemGrn(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates the details of an existing item grn
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanEditItemGrn)]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(DesignationDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateItemGrn([FromRoute] Guid id, [FromBody] CreateItemGrnRequest request)
    {
        if (!await MayReceive(await GrnItem(id)) || !await MayReceive(request.ItemId)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateItemGrn(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific item grn by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(PermissionKeys.CanDeleteItemGrn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItemGrn([FromRoute] Guid id)
    {
        if (!await MayReceive(await GrnItem(id))) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        var result = await repository.DeleteItemGrn(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private async Task<Guid?> GrnItem(Guid id) =>
        await context.ItemGrns.IgnoreQueryFilters().AsNoTracking().Where(grn => grn.Id == id).Select(grn => (Guid?)grn.ItemId).FirstOrDefaultAsync();

    /// <summary>
    /// An item GRN is the store's receiving-area entry: it needs that store's receiving-area approve key, checked from
    /// current grants. An unknown item or GRN is refused.
    /// </summary>
    private async Task<bool> MayReceive(Guid? itemId)
    {
        if (itemId is null || !Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return false;
        var store = await context.Items.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == itemId)
            .Select(item => (DOMAIN.Entities.Items.Store?)item.Store).FirstOrDefaultAsync();
        var name = store switch
        {
            DOMAIN.Entities.Items.Store.ItStore => "It", DOMAIN.Entities.Items.Store.GeneralStore => "General",
            DOMAIN.Entities.Items.Store.EquipmentStore => "Equipment", DOMAIN.Entities.Items.Store.ReagentStore => "Reagent", _ => null,
        };
        return name is not null && (await APP.Services.Authorization.CurrentPermissionSet.Load(context, actor)).Contains($"CanApprove{name}StoreReceivingAreaEntry");
    }
}
