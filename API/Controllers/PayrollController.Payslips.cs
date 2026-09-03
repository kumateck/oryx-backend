using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.AttendanceRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class PayrollController
{
    [HttpGet("runs/{payrollRunId:guid}/payslips")]
    [Authorize(PermissionKeys.CanViewPayslip)]
    public async Task<IResult> GetPayslipsForRun(
        [FromRoute] Guid payrollRunId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetPayslipsForRun(payrollRunId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("payslips/{id:guid}")]
    [Authorize(PermissionKeys.CanViewPayslip)]
    public async Task<IResult> GetPayslip([FromRoute] Guid id)
    {
        var result = await repository.GetPayslip(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Self-service: an employee's own payslip history (processed runs only).
    /// </summary>
    [HttpGet("employees/{employeeId:guid}/payslips")]
    [Authorize(PermissionKeys.CanViewOwnPayslip)]
    public async Task<IResult> GetEmployeePayslips(
        [FromRoute] Guid employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetEmployeePayslips(employeeId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Exports a bulk bank-transfer advice for a payroll run (csv or xlsx via ?fileType=).
    /// </summary>
    [HttpGet("runs/{payrollRunId:guid}/bank-advice")]
    [Authorize(PermissionKeys.CanExportBankAdvice)]
    public async Task<IResult> ExportBankAdvice([FromRoute] Guid payrollRunId, [FromQuery] FileFormat format)
    {
        var result = await repository.ExportBankAdvice(payrollRunId, format);
        return result.IsSuccess
            ? TypedResults.File(result.Value.FileBytes, result.Value.ContentType, result.Value.FileName)
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Exports the full payroll register for a run (csv or xlsx via ?fileType=).
    /// </summary>
    [HttpGet("runs/{payrollRunId:guid}/register")]
    [Authorize(PermissionKeys.CanExportPayrollRegister)]
    public async Task<IResult> ExportPayrollRegister([FromRoute] Guid payrollRunId, [FromQuery] FileFormat format)
    {
        var result = await repository.ExportPayrollRegister(payrollRunId, format);
        return result.IsSuccess
            ? TypedResults.File(result.Value.FileBytes, result.Value.ContentType, result.Value.FileName)
            : result.ToProblemDetails();
    }
}
