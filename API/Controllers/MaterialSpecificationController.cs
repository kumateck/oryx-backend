using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/material-specifications")]
[Authorize]
public class MaterialSpecificationController(IMaterialSpecificationRepository repository) : ControllerBase
{
     /// <summary>
    /// Creates a material specification
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMaterialSpecification(CreateMaterialSpecificationRequest request)
    {
        var result = await repository.CreateMaterialSpecification(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of material specifications
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialSpecificationDto>>))]
    public async Task<IResult> GetMaterialSpecifications([FromQuery] MaterialKind materialKind,[FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetMaterialSpecifications(page, pageSize, searchQuery, materialKind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a material specification by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialSpecificationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialSpecification([FromRoute] Guid id)
    {
        var result = await repository.GetMaterialSpecification(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a material specification by its material ID.
    /// </summary>
    [HttpGet("material/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialSpecificationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialSpecificationByMaterialId([FromRoute] Guid id)
    {
        var result = await repository.GetMaterialSpecificationByMaterialId(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a material specific by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(MaterialSpecificationDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateMaterialSpecification([FromRoute] Guid id, [FromBody] CreateMaterialSpecificationRequest request)
    {
        var result = await repository.UpdateMaterialSpecification(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a material specification by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteMaterialSpecification([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];

        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteMaterialSpecification(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}