using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class SupplierController
{
    [HttpGet("{supplierId:guid}/bank-details")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> GetBankDetails([FromRoute] Guid supplierId)
    {
        var result = await repository.GetBankDetails(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/bank-details")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> CreateBankDetail(
        [FromRoute] Guid supplierId, [FromBody] SupplierBankDetailRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.CreateBankDetail(supplierId, request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{supplierId:guid}/bank-details/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> UpdateBankDetail(
        [FromRoute] Guid supplierId, [FromRoute] Guid id, [FromBody] SupplierBankDetailRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.UpdateBankDetail(supplierId, id, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{supplierId:guid}/bank-details/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> DeleteBankDetail([FromRoute] Guid supplierId, [FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.DeleteBankDetail(supplierId, id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
