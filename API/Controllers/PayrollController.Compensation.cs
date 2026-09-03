using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class PayrollController
{
    /// <summary>
    /// Creates a new effective-dated compensation record for an employee, superseding the current one.
    /// </summary>
    [HttpPost("compensation")]
    [Authorize(PermissionKeys.CanManageCompensation)]
    public async Task<IResult> CreateOrUpdateCompensation([FromBody] CreateEmployeeCompensationRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.CreateOrUpdateCompensation(request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the current compensation record for an employee.
    /// </summary>
    [HttpGet("compensation/{employeeId:guid}")]
    [Authorize(PermissionKeys.CanViewCompensation)]
    public async Task<IResult> GetCurrentCompensation([FromRoute] Guid employeeId)
    {
        var result = await repository.GetCurrentCompensation(employeeId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the full compensation history for an employee.
    /// </summary>
    [HttpGet("compensation/{employeeId:guid}/history")]
    [Authorize(PermissionKeys.CanViewCompensation)]
    public async Task<IResult> GetCompensationHistory(
        [FromRoute] Guid employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetCompensationHistory(employeeId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a recurring or one-off payroll deduction for an employee.
    /// </summary>
    [HttpPost("deductions")]
    [Authorize(PermissionKeys.CanManageCompensation)]
    public async Task<IResult> CreatePayrollDeduction([FromBody] CreatePayrollDeductionRequest request)
    {
        var result = await repository.CreatePayrollDeduction(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Lists payroll deductions, optionally filtered to one employee.
    /// </summary>
    [HttpGet("deductions")]
    [Authorize(PermissionKeys.CanViewCompensation)]
    public async Task<IResult> GetPayrollDeductions(
        [FromQuery] Guid? employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetPayrollDeductions(employeeId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a payroll deduction.
    /// </summary>
    [HttpDelete("deductions/{id:guid}")]
    [Authorize(PermissionKeys.CanManageCompensation)]
    public async Task<IResult> DeletePayrollDeduction([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollDeduction(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a bonus, commission, overtime, or other one-off/recurring payroll addition for an employee.
    /// </summary>
    [HttpPost("additions")]
    [Authorize(PermissionKeys.CanManageCompensation)]
    public async Task<IResult> CreatePayrollAddition([FromBody] CreatePayrollAdditionRequest request)
    {
        var result = await repository.CreatePayrollAddition(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("additions")]
    [Authorize(PermissionKeys.CanViewCompensation)]
    public async Task<IResult> GetPayrollAdditions(
        [FromQuery] Guid? employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetPayrollAdditions(employeeId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("additions/{id:guid}")]
    [Authorize(PermissionKeys.CanManageCompensation)]
    public async Task<IResult> DeletePayrollAddition([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.DeletePayrollAddition(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a GRA income tax relief (annual amount) for an employee, applied before PAYE bands.
    /// </summary>
    [HttpPost("tax-reliefs")]
    [Authorize(PermissionKeys.CanManageCompensation)]
    public async Task<IResult> CreateTaxRelief([FromBody] CreateEmployeeTaxReliefRequest request)
    {
        var result = await repository.CreateTaxRelief(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("tax-reliefs/{employeeId:guid}")]
    [Authorize(PermissionKeys.CanViewCompensation)]
    public async Task<IResult> GetTaxReliefs([FromRoute] Guid employeeId)
    {
        var result = await repository.GetTaxReliefs(employeeId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("tax-reliefs/{id:guid}")]
    [Authorize(PermissionKeys.CanManageCompensation)]
    public async Task<IResult> DeleteTaxRelief([FromRoute] Guid id)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();

        var result = await repository.DeleteTaxRelief(id, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
