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
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get a daily attendance record summary based on a department and the date.
    /// </summary>
    [HttpGet("daily-summary")]
    public async Task<IResult> DepartmentDailySummaryAttendance([FromQuery] string departmentName, [FromQuery] DateTime date)
    {
        var result = await repository.DepartmentDailySummaryAttendance(departmentName, date);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get a daily attendance record summary based on and the date.
    /// </summary>
    [HttpGet("general-summary")]
    public async Task<IResult> GeneralDailySummary([FromQuery] DateTime date)
    {
        var result = await repository.GeneralAttendanceReport(date);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}