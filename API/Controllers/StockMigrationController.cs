using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials.StockMigrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Opening-stock migration: upload a department's stock count, check and correct it row by row, then
/// approve it to replace the department's storage stock.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/stock-migrations")]
// Approving replaces a department's stock, so every endpoint needs the key that changes stock (the desktop gates on it too).
[Authorize(PermissionKeys.CanCreateMaterialStockAdjustment)]
public class StockMigrationController(IStockMigrationRepository repository) : ControllerBase
{
    /// <summary>
    /// Reads a stock-count workbook into a draft migration. Every tab with a material code and quantity
    /// column is read. Nothing changes in the stock until the migration is approved.
    /// </summary>
    /// <param name="file">The workbook.</param>
    /// <param name="departmentId">The department whose stores the stock belongs to.</param>
    /// <param name="massUnit">The unit the sheet's weights are written in: kg, g or mg.</param>
    /// <param name="volumeUnit">The unit the sheet's liquids are written in: L or ml.</param>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Upload(IFormFile file, [FromQuery] Guid departmentId, [FromQuery] string massUnit = "kg", [FromQuery] string volumeUnit = "L")
    {
        if (Actor() is not { } actor)
            return TypedResults.Unauthorized();
        var result = await repository.Upload(file, departmentId, massUnit, volumeUnit, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Lists stock migrations, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockMigrationDto>))]
    public async Task<IResult> List([FromQuery] Guid? departmentId)
    {
        var result = await repository.List(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>A migration with its row counts and the department's stores and shelves.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StockMigrationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Get(Guid id)
    {
        var result = await repository.Get(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>The migration's rows, filtered by status, tab or search text.</summary>
    [HttpGet("{id:guid}/lines")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<StockMigrationLineDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetLines(Guid id, [FromQuery] StockMigrationLineFilter filter)
    {
        var result = await repository.GetLines(id, filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Corrects one row; every row is checked again.</summary>
    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> UpdateLine(Guid id, Guid lineId, [FromBody] UpdateStockMigrationLineRequest request)
    {
        var result = await repository.UpdateLine(id, lineId, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Sets the shelf of, or leaves out, many rows at once.</summary>
    [HttpPut("{id:guid}/lines")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> BulkUpdate(Guid id, [FromBody] BulkUpdateStockMigrationLinesRequest request)
    {
        var result = await repository.BulkUpdate(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Changes the units the sheet's quantities are read in.</summary>
    [HttpPut("{id:guid}/units")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateUnits(Guid id, [FromBody] UpdateStockMigrationUnitsRequest request)
    {
        var result = await repository.UpdateUnits(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Checks every row again, for example after adding a missing shelf or material.</summary>
    [HttpPost("{id:guid}/recheck")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> Recheck(Guid id)
    {
        var result = await repository.Recheck(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Each material's storage stock now against what the migration leaves.</summary>
    [HttpGet("{id:guid}/comparison")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockMigrationComparisonDto>))]
    public async Task<IResult> Comparison(Guid id)
    {
        var result = await repository.Comparison(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Writes off the department's current storage stock of the kinds in the sheet and loads the checked
    /// rows in its place. Refused while any row is blocked.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Approve(Guid id)
    {
        if (Actor() is not { } actor)
            return TypedResults.Unauthorized();
        var result = await repository.Approve(id, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>Discards a migration that has not been approved.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Discard(Guid id)
    {
        var result = await repository.Discard(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private Guid? Actor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;
}
