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
    public async Task<IResult> GetContacts([FromRoute] Guid supplierId)
    {
        var result = await repository.GetContacts(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/contacts")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> CreateContact([FromRoute] Guid supplierId, [FromBody] SupplierContactRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.CreateContact(supplierId, request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{supplierId:guid}/contacts/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> UpdateContact(
        [FromRoute] Guid supplierId, [FromRoute] Guid id, [FromBody] SupplierContactRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.UpdateContact(supplierId, id, request, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{supplierId:guid}/contacts/{id:guid}")]
    [Authorize(PermissionKeys.CanManageSupplierContracts)]
    public async Task<IResult> DeleteContact([FromRoute] Guid supplierId, [FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.DeleteContact(supplierId, id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
