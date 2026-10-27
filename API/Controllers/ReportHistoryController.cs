using API.Filters;
using SHARED;
using Microsoft.EntityFrameworkCore;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Reports;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED.Services.Identity;
namespace API.Controllers;
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/report/history")]
public class ReportHistoryController(ApplicationDbContext context, ICurrentUserService user) : ControllerBase
{
    [HttpGet("capabilities")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReportHistoryCapabilitiesDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Capabilities(CancellationToken cancellationToken)
    {
        var permissions = await CurrentPermissionSet.Load(context, user.UserId);
        string[] reportPermissions = [PermissionKeys.CanViewHumanResourcesReport, PermissionKeys.CanViewWarehouseReport,
            PermissionKeys.CanViewQcAndQaReport, PermissionKeys.CanViewStoreAndInventoryReport, PermissionKeys.CanViewFgtnReport, PermissionKeys.CanViewProcurementReport, PermissionKeys.CanViewAttendanceReportUpload];
        if (!reportPermissions.Any(permissions.Contains)) return TypedResults.Forbid();
        var coverage = await ReportHistoryScope.Coverage(context, cancellationToken);
        return TypedResults.Ok(new ReportHistoryCapabilitiesDto { ServerTime = DateTime.UtcNow, Available = coverage.Count > 0, AvailableFrom = coverage.Count > 0 ? coverage.Values.Max() : null });
    }
    [HttpGet("vendors")]
    [ReportHistory(PermissionKeys.CanViewStoreAndInventoryReport, RequireForCurrent = true)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CollectionItemDto>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Vendors(CancellationToken cancellationToken)
    {
        // Report-only names/IDs avoid requiring unrelated vendor administration permissions.
        var rows = await context.Vendors.AsNoTracking().OrderBy(vendor=>vendor.Name)
            .Select(vendor=>new CollectionItemDto {Id=vendor.Id,Name=vendor.Name}).ToListAsync(cancellationToken);
        return TypedResults.Ok(rows);
    }

}
