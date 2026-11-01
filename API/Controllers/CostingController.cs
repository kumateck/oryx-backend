using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

/// <summary>
/// Finance costing: what products are priced at, and in which currency. Guarded by
/// <c>ManageCosting</c> (Finance → Costing on the role permissions page).
/// </summary>
[ApiController]
[Route("api/v{Version:apiVersion}/costing")]
[Authorize(PermissionKeys.CanManageCosting)]
public class CostingController(ICostingRepository repository) : ControllerBase
{
    /// <summary>Lists products with their current list price and its currency.</summary>
    [HttpGet("list-prices")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductListPriceDto>>))]
    public async Task<IResult> GetListPrices([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? departmentId = null, [FromQuery] Guid? currencyId = null)
    {
        var result = await repository.GetListPrices(page, pageSize, searchQuery, departmentId, currencyId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Every list price a product has had, newest first.</summary>
    [HttpGet("list-prices/{productId:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ListPriceHistoryDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetListPriceHistory([FromRoute] Guid productId)
    {
        var result = await repository.GetListPriceHistory(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Sets a product's list price from now on, in the chosen currency.</summary>
    [HttpPut("list-prices/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SetListPrice([FromRoute] Guid productId, [FromBody] SetListPriceRequest request)
    {
        if (!(HttpContext.Items["Sub"] is string value && Guid.TryParse(value, out var userId)))
            return TypedResults.Unauthorized();
        var result = await repository.SetListPrice(productId, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Lists customer pricing agreements across all customers.</summary>
    [HttpGet("pricing-agreements")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<CustomerPricingAgreementDto>>))]
    public async Task<IResult> GetPricingAgreements([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? customerId = null, [FromQuery] Guid? productId = null,
        [FromQuery] DateTime? activeOn = null)
    {
        var result = await repository.GetPricingAgreements(page, pageSize, searchQuery, customerId, productId, activeOn);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>One customer pricing agreement.</summary>
    [HttpGet("pricing-agreements/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerPricingAgreementDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPricingAgreement([FromRoute] Guid id)
    {
        var result = await repository.GetPricingAgreement(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
