using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.AttendanceRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/attendance-records")]
[Authorize]
public class AttendanceRecordController(IAttendanceRepository repository) : ControllerBase
{
    
    /// <summary>
    /// Upload daily attendance record
    /// </summary>
    [HttpPost("upload")]
    public async Task<IResult> UploadAttendance([FromForm] CreateAttendanceRequest request)
    {
        var result = await repository.UploadAttendance(request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Get a daily attendance record summary based on a department and the date.
    /// </summary>
    [HttpGet("daily-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<AttendanceRecordDepartmentDto>))]
    public async Task<IResult> DepartmentDailySummaryAttendance([FromQuery] string departmentName, [FromQuery] DateTime date)
    {
        var result = await repository.DepartmentDailySummaryAttendance(departmentName, date);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get a daily attendance record summary based on and the date.
    /// </summary>
    [HttpGet("general-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GeneralAttendanceReportResponse))]
    public async Task<IResult> GeneralDailySummary()
    {
        var result = await repository.GeneralAttendanceReport();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Exports attendance summary as a CSV or Excel file (excel by default)
    /// </summary>
    [HttpGet("export")]
    public async Task<IResult> ExportAttendanceSummary([FromQuery] FileFormat format)
    {
        var result = await repository.ExportAttendanceSummary(format);
        return result.IsSuccess ? TypedResults.File(result.Value.FileBytes, result.Value.ContentType, result.Value.FileName) : result.ToProblemDetails();
    }
}