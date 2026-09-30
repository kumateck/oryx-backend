using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class SupplierController
{
    [HttpGet("{supplierId:guid}/pricing-agreements")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> GetPricingAgreements([FromRoute] Guid supplierId)
    {
        var result = await repository.GetPricingAgreements(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{supplierId:guid}/pricing-agreements/proposals/{id:guid}")]
    [Authorize]
    public async Task<IResult> GetPricingAgreementProposal([FromRoute] Guid supplierId, [FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var roleIds = HttpContext.Items["Roles"] as List<Guid> ?? [];
        var result = await repository.GetPricingAgreementProposal(supplierId, id, userId, roleIds);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{supplierId:guid}/pricing-agreements/active")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> GetActivePricingAgreement(
        [FromRoute] Guid supplierId, [FromQuery] Guid materialId, [FromQuery] Guid uomId, [FromQuery] DateTime asOf)
    {
        var result = await repository.GetActivePricingAgreement(supplierId, materialId, uomId, asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/pricing-agreements")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> CreatePricingAgreement(
        [FromRoute] Guid supplierId, [FromBody] SupplierPricingAgreementRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.CreatePricingAgreement(supplierId, request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{supplierId:guid}/pricing-agreements/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> UpdatePricingAgreement(
        [FromRoute] Guid supplierId, [FromRoute] Guid id, [FromBody] SupplierPricingAgreementRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.UpdatePricingAgreement(supplierId, id, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{supplierId:guid}/pricing-agreements/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> DeletePricingAgreement([FromRoute] Guid supplierId, [FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.DeletePricingAgreement(supplierId, id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
