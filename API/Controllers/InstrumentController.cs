using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Instruments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qc/instruments")]
[Authorize]
public class InstrumentController(IInstrumentRepository repository) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of instruments, including calibration status.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewInstrumentCalibration)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<InstrumentDto>>))]
    public async Task<IResult> GetInstruments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetInstruments(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an instrument by its ID, including calibration status.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(PermissionKeys.CanViewInstrumentCalibration)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InstrumentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetInstrument([FromRoute] Guid id)
    {
        var result = await repository.GetInstrument(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an instrument's calibration record.
    /// </summary>
    [HttpPut("{id:guid}/calibration")]
    [Authorize(PermissionKeys.CanUpdateInstrumentCalibration)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateCalibration(
        [FromRoute] Guid id,
        [FromBody] UpdateInstrumentCalibrationRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateCalibration(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves instruments whose calibration is due within the given number of days.
    /// </summary>
    [HttpGet("calibration-due")]
    [Authorize(PermissionKeys.CanViewInstrumentCalibration)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InstrumentDto>))]
    public async Task<IResult> GetInstrumentsWithCalibrationDue([FromQuery] int withinDays = 30)
    {
        var result = await repository.GetInstrumentsWithCalibrationDue(withinDays);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
