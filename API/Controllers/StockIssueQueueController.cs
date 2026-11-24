using APP.Extensions;
using APP.IRepository;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace API.Controllers;

public sealed record StockIssueQueueDto(Paginateable<IEnumerable<RequisitionDto>> Requisitions, List<CollectionItemDto> Products);

[ApiController]
[Tags("Requisition")]
[Route("api/v{version:apiVersion}/stock-issue")]
public sealed class StockIssueQueueController(ApplicationDbContext db, IRequisitionRepository repository) : ControllerBase
{
    [HttpGet("raw", Name = "GetRawStockIssueQueue")]
    [Authorize(PermissionKeys.CanViewRawMaterialRequisitions)]
    [ProducesResponseType(typeof(StockIssueQueueDto), StatusCodes.Status200OK)]
    public Task<IResult> Raw([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string searchQuery = null,
        [FromQuery] bool? issued = null, [FromQuery] Guid? productId = null) => Read(MaterialKind.Raw, page, pageSize, searchQuery, issued, productId);

    [HttpGet("packaging", Name = "GetPackagingStockIssueQueue")]
    [Authorize(PermissionKeys.CanViewPackagingMaterialRequisitions)]
    [ProducesResponseType(typeof(StockIssueQueueDto), StatusCodes.Status200OK)]
    public Task<IResult> Packaging([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string searchQuery = null,
        [FromQuery] bool? issued = null, [FromQuery] Guid? productId = null) => Read(MaterialKind.Package, page, pageSize, searchQuery, issued, productId);

    private async Task<IResult> Read(MaterialKind kind, int page, int pageSize, string searchQuery, bool? issued, Guid? productId)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor)) return TypedResults.Unauthorized();
        var key = kind == MaterialKind.Raw ? PermissionKeys.CanViewRawMaterialRequisitions : PermissionKeys.CanViewPackagingMaterialRequisitions;
        if (!(await CurrentPermissionSet.Load(db, actor)).Contains(key)) return TypedResults.Forbid();
        var department = await db.Users.Where(user => user.Id == actor && !user.IsDisabled).Select(user => user.DepartmentId).SingleOrDefaultAsync();
        if (!department.HasValue) return TypedResults.Unauthorized();
        var prefix = kind == MaterialKind.Raw ? "RM" : "PM";
        var products = db.Requisitions.AsNoTracking().Where(request => request.DepartmentId == department && request.RequisitionType == RequisitionType.Stock
            && request.Code.StartsWith(prefix) && request.ProductionScheduleProductId.HasValue);
        if (issued == true) products = products.Where(request => request.Status == RequestStatus.Completed);
        else if (issued == false) products = products.Where(request => request.Status != RequestStatus.Completed && request.Status != RequestStatus.Rejected);
        var choices = await products.Select(request => new { request.ProductionScheduleProduct.Product.Id, request.ProductionScheduleProduct.Product.Name,
            request.ProductionScheduleProduct.Product.Code }).Distinct().OrderBy(product => product.Name).ThenBy(product => product.Code).ToListAsync();
        var result = await repository.GetRequisitions(Math.Max(1, page), Math.Clamp(pageSize, 1, 200), searchQuery, null, RequisitionType.Stock, department, kind, issued, productId, oldestFirst: true);
        return result.IsSuccess ? TypedResults.Ok(new StockIssueQueueDto(result.Value,
            choices.Select(product => new CollectionItemDto { Id = product.Id, Name = product.Name, Code = product.Code }).ToList())) : result.ToProblemDetails();
    }
}
