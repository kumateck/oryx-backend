using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Products.Equipments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qc-equipment")]
[Authorize]
public class QcEquipmentController(IAnalyticalTestRequestRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a QC Equipment item.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateQcEquipment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateQcEquipment([FromBody] CreateQcEquipment request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateQcEquipment(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of QC Equipment.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewQcEquipment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<QcEquipmentDto>>))]
    public async Task<IResult> GetQcEquipments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetQcEquipments(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves all QC Equipment items.
    /// </summary>
    [HttpGet("all")]
    [Authorize(PermissionKeys.CanViewQcEquipment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<QcEquipmentDto>))]
    public async Task<IResult> GetAllQcEquipments()
    {
        var result = await repository.GetQcEquipments();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a QC Equipment item by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(PermissionKeys.CanViewQcEquipment)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QcEquipmentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetQcEquipment([FromRoute] Guid id)
    {
        var result = await repository.GetQcEquipment(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a QC Equipment item.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanEditQcEquipmentDetails)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateQcEquipment([FromRoute] Guid id, [FromBody] CreateQcEquipment request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateQcEquipment(request, id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a QC Equipment item.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(PermissionKeys.CanDeleteQcEquipment)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteQcEquipment([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.DeleteQcEquipment(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves QC Equipment items whose calibration is due within the given number of days.
    /// </summary>
    [HttpGet("calibration-due")]
    [Authorize(PermissionKeys.CanViewQcEquipmentCalibration)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<QcEquipmentDto>))]
    public async Task<IResult> GetQcEquipmentsWithCalibrationDue([FromQuery] int withinDays = 30)
    {
        var result = await repository.GetQcEquipmentsWithCalibrationDue(withinDays);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
