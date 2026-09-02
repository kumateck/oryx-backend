using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class SupplierController
{
    [HttpGet("{supplierId:guid}/certifications")]
    [Authorize(PermissionKeys.CanViewSupplierCertifications)]
    public async Task<IResult> GetCertifications([FromRoute] Guid supplierId)
    {
        var result = await repository.GetCertifications(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("certifications/expiring")]
    [Authorize(PermissionKeys.CanViewSupplierCertifications)]
    public async Task<IResult> GetExpiringCertifications(
        [FromQuery] int withinDays = 30, [FromQuery] DateTime? asOf = null)
    {
        var result = await repository.GetExpiringCertifications(withinDays, asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/certifications")]
    [Authorize(PermissionKeys.CanManageSupplierCertifications)]
    public async Task<IResult> CreateCertification(
        [FromRoute] Guid supplierId, [FromBody] SupplierCertificationRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.CreateCertification(supplierId, request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{supplierId:guid}/certifications/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierCertifications)]
    public async Task<IResult> UpdateCertification(
        [FromRoute] Guid supplierId, [FromRoute] Guid id, [FromBody] SupplierCertificationRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.UpdateCertification(supplierId, id, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{supplierId:guid}/certifications/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierCertifications)]
    public async Task<IResult> DeleteCertification([FromRoute] Guid supplierId, [FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.DeleteCertification(supplierId, id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
