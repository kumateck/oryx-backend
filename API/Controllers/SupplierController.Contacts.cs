using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class SupplierController
{
    [HttpGet("{supplierId:guid}/contacts")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> GetContacts(Guid supplierId)
    {
        var result = await _repository.GetContacts(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/contacts")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> CreateContact(Guid supplierId, [FromBody] SupplierContactRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await _repository.CreateContact(supplierId, request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{supplierId:guid}/contacts/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> UpdateContact(
        Guid supplierId, Guid id, [FromBody] SupplierContactRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await _repository.UpdateContact(supplierId, id, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{supplierId:guid}/contacts/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> DeleteContact(Guid supplierId, Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await _repository.DeleteContact(supplierId, id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
