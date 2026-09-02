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
    public async Task<IResult> GetBankDetails(Guid supplierId)
    {
        var result = await _repository.GetBankDetails(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/bank-details")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> CreateBankDetail(
        Guid supplierId, [FromBody] SupplierBankDetailRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await _repository.CreateBankDetail(supplierId, request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{supplierId:guid}/bank-details/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> UpdateBankDetail(
        Guid supplierId, Guid id, [FromBody] SupplierBankDetailRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await _repository.UpdateBankDetail(supplierId, id, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{supplierId:guid}/bank-details/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> DeleteBankDetail(Guid supplierId, Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await _repository.DeleteBankDetail(supplierId, id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
