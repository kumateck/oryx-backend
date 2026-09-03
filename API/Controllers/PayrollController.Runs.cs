using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class PayrollController
{
    [HttpPost("runs")]
    [Authorize(PermissionKeys.CanCreatePayrollRun)]
    public async Task<IResult> CreatePayrollRun([FromBody] CreatePayrollRunRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.CreatePayrollRun(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("runs")]
    [Authorize(PermissionKeys.CanViewPayrollRuns)]
    public async Task<IResult> GetPayrollRuns(
        [FromQuery] PayrollRunStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetPayrollRuns(page, pageSize, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("runs/{id:guid}")]
    [Authorize(PermissionKeys.CanViewPayrollRuns)]
    public async Task<IResult> GetPayrollRun([FromRoute] Guid id)
    {
        var result = await repository.GetPayrollRun(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Generates payslips for all active employees and submits the run for approval.
    /// </summary>
    [HttpPost("runs/{id:guid}/submit")]
    [Authorize(PermissionKeys.CanCreatePayrollRun)]
    public async Task<IResult> SubmitPayrollRunForApproval([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.SubmitPayrollRunForApproval(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Marks an approved payroll run as processed/paid.
    /// </summary>
    [HttpPost("runs/{id:guid}/process")]
    [Authorize(PermissionKeys.CanApprovePayrollRun)]
    public async Task<IResult> MarkPayrollRunProcessed([FromRoute] Guid id)
    {
        var result = await repository.MarkPayrollRunProcessed(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("runs/{id:guid}/cancel")]
    [Authorize(PermissionKeys.CanCancelPayrollRun)]
    public async Task<IResult> CancelPayrollRun([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.CancelPayrollRun(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
