using APP.Extensions;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class CustomerController
{
    [HttpGet("{id:guid}/credit-status")]
    [Authorize(PermissionKeys.CanViewCustomerCreditStatus)]
    public async Task<IResult> GetCreditStatus(Guid id, [FromQuery] decimal additionalOrderValue = 0)
    {
        var result = await repository.GetAvailableCredit(id, additionalOrderValue);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
