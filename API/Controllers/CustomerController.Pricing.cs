using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class CustomerController
{
    [HttpGet("{id:guid}/pricing-agreements")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> GetPricingAgreements([FromRoute] Guid id)
    {
        var result = await repository.GetPricingAgreements(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/pricing-agreements/active")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> GetActivePricingAgreement(
        [FromRoute] Guid id, [FromQuery] Guid productId, [FromQuery] Guid uomId, [FromQuery] DateTime asOf)
    {
        var result = await repository.GetActivePricingAgreement(id, productId, uomId, asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/pricing-agreements")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> CreatePricingAgreement([FromRoute] Guid id, [FromBody] CustomerPricingAgreementRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.CreatePricingAgreement(id, request, userId.Value);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/pricing-agreements/{agreementId:guid}")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> UpdatePricingAgreement([FromRoute] Guid id, [FromRoute] Guid agreementId,
        [FromBody] CustomerPricingAgreementRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.UpdatePricingAgreement(id, agreementId, request, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/pricing-agreements/{agreementId:guid}")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> DeletePricingAgreement([FromRoute] Guid id, [FromRoute] Guid agreementId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.DeletePricingAgreement(id, agreementId, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
