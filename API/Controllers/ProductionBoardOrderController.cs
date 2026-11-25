using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/production-board-order/{departmentId:guid}")]
public sealed class ProductionBoardOrderController(ICollectionRepository repository) : ControllerBase
{
    [HttpGet(Name = "GetDepartmentProductionBoardOrder")]
    [Authorize(PermissionKeys.CanViewDepartmentProductionBoardOrder)]
    [ProducesResponseType(typeof(ProductionBoardOrderDto), StatusCodes.Status200OK)]
    public async Task<IResult> Get(Guid departmentId)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var result = await repository.GetProductionBoardOrder(departmentId, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut(Name = "SaveDepartmentProductionBoardOrder")]
    [Authorize(PermissionKeys.CanReorderDepartmentProductionBoard)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> Save(Guid departmentId, SaveProductionBoardOrder request)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var result = await repository.SaveProductionBoardOrder(departmentId, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
