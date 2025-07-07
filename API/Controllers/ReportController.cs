using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reports;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/report")]
[ApiController]
[Authorize]
public class ReportController(IReportRepository repository) : ControllerBase
{
    /// <summary>
    /// Gets the production report for a specific department.
    /// </summary>
    /// <returns>Returns the production report.</returns>
    [HttpGet("production")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionReport([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (departmentId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetProductionReport(filter, Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets a list of materials that are below the minimum stock level for a specific department.
    /// </summary>
    /// <returns>Returns a list of materials below the minimum stock level.</returns>
    [HttpGet("production/materials-below-minimum")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialsBelowMinimumStockLevel([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (departmentId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetMaterialsBelowMinimumStockLevel(Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets the human resource report
    /// </summary>
    [HttpGet("human-resource")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HumanResourceReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetHumanResourceReport([FromQuery] ReportFilter filter)
    {
        var result = await repository.GetHumanResourceReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the report detailing the grade-wise count of permanent staff across departments.
    /// </summary>
    [HttpGet("staff-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PermanentStaffGradeCountDto>))]
    public async Task<IResult> GetPermanentStaffGradeReport([FromQuery] Guid? departmentId)
    {
        var result = await repository.GetPermanentStaffGradeReport(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the employee movement report based on the specified filter.
    /// </summary>
    [HttpGet("employee-movement")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MovementReportDto>))]
    public async Task<IResult> GetEmployeeMovementReport([FromQuery] MovementReportFilter filter)
    {
        var result = await repository.GetEmployeeMovementReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}