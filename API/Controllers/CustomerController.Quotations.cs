using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class CustomerController
{
    [HttpGet("{id:guid}/quotations")]
    [Authorize(PermissionKeys.CanViewCustomerQuotations)]
    public async Task<IResult> GetActiveQuotations(
        [FromRoute] Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] DateTime? asOf = null)
    {
        var result = await repository.GetActiveQuotations(id, page, pageSize, asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("quotations/{quotationId:guid}")]
    [Authorize(PermissionKeys.CanViewCustomerQuotations)]
    public async Task<IResult> GetQuotation([FromRoute] Guid quotationId)
    {
        var result = await repository.GetQuotation(quotationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/quotations")]
    [Authorize(PermissionKeys.CanCreateCustomerQuotation)]
    public async Task<IResult> CreateQuotation([FromRoute] Guid id, [FromBody] CreateCustomerQuotationRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.CreateQuotation(id, request, userId.Value);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("quotations/{quotationId:guid}/send")]
    [Authorize(PermissionKeys.CanCreateCustomerQuotation)]
    public async Task<IResult> SendQuotation([FromRoute] Guid quotationId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.SendQuotation(quotationId, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("quotations/{quotationId:guid}/approval")]
    [Authorize(PermissionKeys.CanApproveCustomerQuotation)]
    public async Task<IResult> ApproveQuotation([FromRoute] Guid quotationId, [FromBody] CustomerQuotationApprovalRequest request)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.ApproveQuotation(quotationId, request, userId.Value);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("quotations/{quotationId:guid}/convert")]
    [Authorize(PermissionKeys.CanConvertCustomerQuotation)]
    public async Task<IResult> ConvertQuotation([FromRoute] Guid quotationId)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return TypedResults.Unauthorized();
        var result = await repository.ConvertQuotationToProductionOrder(quotationId, userId.Value);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
