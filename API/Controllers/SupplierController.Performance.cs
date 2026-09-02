using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public partial class SupplierController
{
    [HttpGet("requalification-due")]
    [Authorize(PermissionKeys.CanViewSupplierCertifications)]
    public async Task<IResult> GetRequalificationDue(
        [FromQuery] int withinDays = 30, [FromQuery] DateTime? asOf = null)
    {
        var result = await repository.GetRequalificationDue(withinDays, asOf);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/performance/compute")]
    [Authorize(PermissionKeys.CanViewSupplierPerformance)]
    public async Task<IResult> ComputePerformance(
        [FromRoute] Guid supplierId, [FromBody] ComputeSupplierPerformanceRequest request)
    {
        var result = await repository.ComputePerformance(supplierId, request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{supplierId:guid}/performance")]
    [Authorize(PermissionKeys.CanViewSupplierPerformance)]
    public async Task<IResult> PersistPerformance(
        [FromRoute] Guid supplierId, [FromBody] ComputeSupplierPerformanceRequest request)
    {
        if (!TryGetUserId(out var userId)) return TypedResults.Unauthorized();
        var result = await repository.PersistPerformance(supplierId, request, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{supplierId:guid}/performance")]
    [Authorize(PermissionKeys.CanViewSupplierPerformance)]
    public async Task<IResult> GetPerformanceRecords([FromRoute] Guid supplierId)
    {
        var result = await repository.GetPerformanceRecords(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{supplierId:guid}/spend-summary")]
    [Authorize(PermissionKeys.CanViewSupplierSpend)]
    public async Task<IResult> GetSpendSummary(
        [FromRoute] Guid supplierId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await repository.GetSpendSummary(supplierId, from, to);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
