using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class CustomerController
{
    [HttpGet("{id:guid}/pricing-agreements")]
    [Authorize(PermissionKeys.CanManageCosting)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CustomerPricingAgreementDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPricingAgreements([FromRoute] Guid id)
    {
        var result = await repository.GetPricingAgreements(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/pricing-agreements/active")]
    [Authorize(PermissionKeys.CanManageCosting)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerPricingAgreementDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> GetActivePricingAgreement(
        [FromRoute] Guid id, [FromQuery] Guid productId, [FromQuery] Guid productPackingId, [FromQuery] DateTime asOf)
    {
        var result = await repository.GetActivePricingAgreement(id, productId, productPackingId, asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/pricing-agreements")]
    [Authorize(PermissionKeys.CanManageCosting)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreatePricingAgreement([FromRoute] Guid id, [FromBody] CustomerPricingAgreementRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.CreatePricingAgreement(id, request, userId.Value);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/pricing-agreements/{agreementId:guid}")]
    [Authorize(PermissionKeys.CanManageCosting)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> UpdatePricingAgreement([FromRoute] Guid id, [FromRoute] Guid agreementId,
        [FromBody] CustomerPricingAgreementRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.UpdatePricingAgreement(id, agreementId, request, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/pricing-agreements/{agreementId:guid}")]
    [Authorize(PermissionKeys.CanManageCosting)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePricingAgreement([FromRoute] Guid id, [FromRoute] Guid agreementId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.DeletePricingAgreement(id, agreementId, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
