using APP.Extensions;
using APP.Services.Costing;
using APP.Utils;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/costing/price-lists")]
[Tags("Costing")]
public class CostingPriceListController(CostingPriceListService lists) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor) ? actor : null;

    [HttpGet("products")]
    [Authorize(PermissionKeys.CanViewCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<CostingPackingOptionDto>>))]
    public async Task<IResult> GetPriceListProducts([FromQuery] Division division, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string searchQuery = null) =>
        TypedResults.Ok(await lists.Options(division, page, pageSize, searchQuery));

    [HttpGet]
    [Authorize(PermissionKeys.CanViewCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CostingPriceListPageDto))]
    public async Task<IResult> GetPriceLists([FromQuery] Division division, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string searchQuery = null) => TypedResults.Ok(await lists.List(division, page, pageSize, searchQuery));

    [HttpGet("{id:guid}")]
    [Authorize(PermissionKeys.CanViewCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CostingPriceListDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPriceList(Guid id)
    {
        var result = await lists.Get(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/lines")]
    [Authorize(PermissionKeys.CanViewCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<CostingPriceListLineDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPriceListLines(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string searchQuery = null)
    {
        var result = await lists.Lines(id, page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/print")]
    [Authorize(PermissionKeys.CanPrintCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CostingPriceListPrintDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPriceListPrint(Guid id)
    {
        var result = await lists.Print(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(PermissionKeys.CanCreateCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> PreparePriceList([FromBody] PrepareCostingPriceListRequest request) =>
        await Prepare(request, false);

    [HttpPost("revise")]
    [Authorize(PermissionKeys.CanReviseCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> RevisePriceList([FromBody] PrepareCostingPriceListRequest request) =>
        await Prepare(request, true);

    private async Task<IResult> Prepare(PrepareCostingPriceListRequest request, bool revise)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.Prepare(request, actor, revise);
        return result.IsSuccess ? TypedResults.Created($"/api/v1/costing/price-lists/{result.Value}", result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanEditCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> UpdatePriceList(Guid id, [FromBody] UpdateCostingPriceListRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.Update(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/lines")]
    [Authorize(PermissionKeys.CanEditCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SavePriceListLine(Guid id, [FromBody] SaveCostingPriceListLineRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.SaveLine(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(PermissionKeys.CanSubmitCostingPriceList)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SubmitPriceList(Guid id, [FromBody] SubmitCostingPriceListRequest request)
    {
        if (Actor is not { } actor) return TypedResults.Unauthorized();
        var result = await lists.Submit(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
