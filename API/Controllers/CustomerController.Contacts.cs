using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class CustomerController
{
    [HttpGet("{id:guid}/contacts")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> GetContacts([FromRoute] Guid id)
    {
        var result = await repository.GetContacts(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/contacts")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> CreateContact([FromRoute]Guid id, [FromBody] CustomerContactRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.CreateContact(id, request, userId.Value);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/contacts/{contactId:guid}")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> UpdateContact([FromRoute] Guid id, [FromRoute] Guid contactId, [FromBody] CustomerContactRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.UpdateContact(id, contactId, request, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    [Authorize(PermissionKeys.CanManageCustomerContracts)]
    public async Task<IResult> DeleteContact([FromRoute] Guid id, [FromRoute] Guid contactId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.DeleteContact(id, contactId, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
