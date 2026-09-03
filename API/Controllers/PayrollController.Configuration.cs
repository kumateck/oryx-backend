using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class PayrollController
{
    [HttpPost("pay-grades")]
    [Authorize(PermissionKeys.CanManagePayrollConfiguration)]
    public async Task<IResult> CreatePayGrade([FromBody] CreatePayGradeRequest request)
    {
        var result = await repository.CreatePayGrade(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("pay-grades")]
    [Authorize(PermissionKeys.CanViewPayrollConfiguration)]
    public async Task<IResult> GetPayGrades()
    {
        var result = await repository.GetPayGrades();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("pay-grades/{id:guid}")]
    [Authorize(PermissionKeys.CanManagePayrollConfiguration)]
    public async Task<IResult> UpdatePayGrade([FromRoute] Guid id, [FromBody] CreatePayGradeRequest request)
    {
        var result = await repository.UpdatePayGrade(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("pay-grades/{id:guid}")]
    [Authorize(PermissionKeys.CanManagePayrollConfiguration)]
    public async Task<IResult> DeletePayGrade([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.DeletePayGrade(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("tax-bands")]
    [Authorize(PermissionKeys.CanManagePayrollConfiguration)]
    public async Task<IResult> CreatePayeTaxBand([FromBody] CreatePayeTaxBandRequest request)
    {
        var result = await repository.CreatePayeTaxBand(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("tax-bands")]
    [Authorize(PermissionKeys.CanViewPayrollConfiguration)]
    public async Task<IResult> GetPayeTaxBands()
    {
        var result = await repository.GetPayeTaxBands();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("tax-bands/{id:guid}")]
    [Authorize(PermissionKeys.CanManagePayrollConfiguration)]
    public async Task<IResult> DeletePayeTaxBand([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.DeletePayeTaxBand(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("ssnit-rates")]
    [Authorize(PermissionKeys.CanManagePayrollConfiguration)]
    public async Task<IResult> CreateSsnitRate([FromBody] CreateSsnitRateRequest request)
    {
        var result = await repository.CreateSsnitRate(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("ssnit-rates")]
    [Authorize(PermissionKeys.CanViewPayrollConfiguration)]
    public async Task<IResult> GetSsnitRates()
    {
        var result = await repository.GetSsnitRates();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("ssnit-rates/{id:guid}")]
    [Authorize(PermissionKeys.CanManagePayrollConfiguration)]
    public async Task<IResult> DeleteSsnitRate([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.DeleteSsnitRate(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
