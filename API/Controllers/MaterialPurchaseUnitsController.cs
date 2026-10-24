using APP.Services.Materials;
using DOMAIN.Entities.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Tags("Material")]
[Route("api/v{version:apiVersion}/material/{materialId:guid}/purchase-units")]
public sealed class MaterialPurchaseUnitsController(MaterialPurchaseUnitsReader reader) : ControllerBase
{
    /// <summary>Return distinct material unit symbols linked in any active department, including zero-stock links.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UnitOfMeasureDto>))]
    public async Task<IResult> Get(Guid materialId, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await reader.Read(materialId, cancellationToken));
    }
}
