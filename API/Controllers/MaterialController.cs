using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/material")]
    [ApiController]
    public class MaterialController(IMaterialRepository repository) : ControllerBase
    {
        /// <summary>
        /// Creates a new material.
        /// </summary>
        /// <param name="request">The CreateMaterialRequest object.</param>
        /// <returns>Returns the ID of the created material.</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IResult> CreateMaterial([FromBody] CreateMaterialRequest request)
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            var result = await repository.CreateMaterial(request, Guid.Parse(userId));
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Retrieves a specific material by its ID.
        /// </summary>
        /// <param name="materialId">The ID of the material.</param>
        /// <returns>Returns the material details.</returns>
        [HttpGet("{materialId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> GetMaterial(Guid materialId)
        {
            var result = await repository.GetMaterial(materialId);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Retrieves a paginated list of materials.
        /// </summary>
        /// <param name="page">The current page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="searchQuery">Search query for filtering results.</param>
        /// <returns>Returns a paginated list of materials.</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MaterialDto>>))]
        public async Task<IResult> GetMaterials([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
        {
            var result = await repository.GetMaterials(page, pageSize, searchQuery);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Updates a specific material by its ID.
        /// </summary>
        /// <param name="request">The UpdateMaterialRequest object containing updated material data.</param>
        /// <param name="materialId">The ID of the material to be updated.</param>
        /// <returns>Returns a success or failure result.</returns>
        [HttpPut("{materialId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> UpdateMaterial([FromBody] CreateMaterialRequest request, Guid materialId)
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            var result = await repository.UpdateMaterial(request, materialId, Guid.Parse(userId));
            return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
        }

        /// <summary>
        /// Deletes a specific material by its ID.
        /// </summary>
        /// <param name="materialId">The ID of the material to be deleted.</param>
        /// <returns>Returns a success or failure result.</returns>
        [HttpDelete("{materialId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> DeleteMaterial(Guid materialId)
        {
            var userId = (string)HttpContext.Items["Sub"];
            if (userId == null) return TypedResults.Unauthorized();

            var result = await repository.DeleteMaterial(materialId, Guid.Parse(userId));
            return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
        }
    }
}
