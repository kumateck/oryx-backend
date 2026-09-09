using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/payments")]
[ApiController]
public class PaymentController(IPaymentRepository repository) : ControllerBase
{
    [HttpGet]
    [Authorize(PermissionKeys.CanViewPayments)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaymentListDto))]
    public async Task<IResult> GetPayments([FromQuery] PaymentListRequest request)
    {
        var result = await repository.GetPayments(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("currency-configuration")]
    [Authorize(PermissionKeys.CanViewCashflowReports)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CashflowCurrencyDto))]
    public async Task<IResult> GetCurrencyConfiguration([FromQuery] DateTime? asOf = null)
    {
        var result = await repository.GetCurrencyConfiguration(asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(PermissionKeys.CanRecordPayment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> RecordPayment([FromBody] RecordPaymentRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.RecordPayment(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{paymentId:guid}")]
    [Authorize(PermissionKeys.CanViewPayments)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaymentDto))]
    public async Task<IResult> GetPayment([FromRoute] Guid paymentId)
    {
        var result = await repository.GetPayment(paymentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{paymentId:guid}/approval-details")]
    [Authorize(PermissionKeys.CanApprovePayment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaymentDto))]
    public async Task<IResult> GetPaymentApprovalDetails([FromRoute] Guid paymentId)
    {
        var result = await repository.GetPayment(paymentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{paymentId:guid}/review")]
    [Authorize(PermissionKeys.CanApprovePayment)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> ReviewPayment([FromRoute] Guid paymentId, [FromBody] ReviewPaymentRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var roleIds = HttpContext.Items["Roles"] as List<Guid> ?? [];
        var result = await repository.ReviewPayment(paymentId, request, userId, roleIds);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpGet("ap-aging")]
    [Authorize(PermissionKeys.CanViewCashflowReports)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AgingReportDto))]
    public async Task<IResult> GetApAging([FromQuery] DateTime? asOf = null)
    {
        var result = await repository.GetApAging(asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("ar-aging")]
    [Authorize(PermissionKeys.CanViewCashflowReports)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AgingReportDto))]
    public async Task<IResult> GetArAging([FromQuery] DateTime? asOf = null)
    {
        var result = await repository.GetArAging(asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("cashflow-summary")]
    [Authorize(PermissionKeys.CanViewCashflowReports)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CashflowSummaryDto))]
    public async Task<IResult> GetCashflowSummary([FromQuery] DateTime? asOf = null)
    {
        var result = await repository.GetCashflowSummary(asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("base-currency/{currencyId:guid}")]
    [Authorize(PermissionKeys.CanRecordPayment)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> SetBaseCurrency([FromRoute] Guid currencyId)
    {
        var result = await repository.SetBaseCurrency(currencyId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("exchange-rates")]
    [Authorize(PermissionKeys.CanRecordPayment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IResult> AddExchangeRate([FromBody] CreateExchangeRateRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.AddExchangeRate(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("exchange-rates/{currencyId:guid}")]
    [Authorize(PermissionKeys.CanViewCashflowReports)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExchangeRateDto))]
    public async Task<IResult> GetExchangeRate([FromRoute] Guid currencyId, [FromQuery] DateTime asOf)
    {
        var result = await repository.GetExchangeRate(currencyId, asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Items["Sub"] as string, out userId);
}
