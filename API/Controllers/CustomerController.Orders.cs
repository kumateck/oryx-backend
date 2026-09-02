using APP.Extensions;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class CustomerController
{
    [HttpGet("{id:guid}/order-history")]
    [Authorize(PermissionKeys.CanViewCustomers)]
    public async Task<IResult> GetOrderHistory(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetOrderHistory(id, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/summary")]
    [Authorize(PermissionKeys.CanViewCustomerCreditStatus)]
    public async Task<IResult> GetSummary(Guid id)
    {
        var result = await repository.GetSummary(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
