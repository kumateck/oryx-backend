using APP.Extensions;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;
using Microsoft.EntityFrameworkCore;
using APP.IRepository;
using DOMAIN.Entities.QcWorksheets;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/qc/material-retests")]
public sealed class MaterialRetestController(IMaterialRetestService retests, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialRetestDetailDto>))]
    [Authorize(QcWorksheetPermissionKeys.CanViewMaterialRetests)]
    public async Task<IResult> List([FromQuery] Guid? batchId)
    {
        var result = await retests.ListAsync(batchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialRetestDetailDto))]
    [Authorize(QcWorksheetPermissionKeys.CanViewMaterialRetestDetail)]
    public async Task<IResult> Get(Guid id)
    {
        var result = await retests.GetAsync(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("batches/{batchId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [Authorize(QcWorksheetPermissionKeys.CanRequestMaterialRetest)]
    public async Task<IResult> CreateRequest(Guid batchId, [FromBody] CreateMaterialRetestRequest body)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.RequestAsync(batchId, body?.Reason, actor, false);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(QcWorksheetPermissionKeys.CanApproveMaterialRetest)]
    public async Task<IResult> Approve(Guid id, [FromBody] MaterialRetestDecision body)
        => await Decide(id, body, true);

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(QcWorksheetPermissionKeys.CanRejectMaterialRetest)]
    public async Task<IResult> Reject(Guid id, [FromBody] MaterialRetestDecision body)
        => await Decide(id, body, false);

    [HttpPost("{id:guid}/sample")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(QcWorksheetPermissionKeys.CanRecordMaterialRetestSample)]
    public async Task<IResult> Sample(Guid id, [FromBody] RecordMaterialRetestSample body)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.RecordSampleAsync(id, body, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(QcWorksheetPermissionKeys.CanAcknowledgeMaterialRetest)]
    public async Task<IResult> Acknowledge(Guid id)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.AcknowledgeAsync(id, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/test-request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(QcWorksheetPermissionKeys.CanLinkMaterialRetestTestRequest)]
    public async Task<IResult> LinkTestRequest(Guid id, [FromBody] LinkMaterialRetestTestRequest body)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var result = await retests.LinkTestRequestAsync(id, body.TestRequestId, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/test-request/create")]
    [Authorize(QcWorksheetPermissionKeys.CanLinkMaterialRetestTestRequest)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DOMAIN.Entities.QcWorksheets.TestRequestDetailDto))]
    public async Task<IResult> CreateTesting(Guid id, [FromBody] DOMAIN.Entities.QcWorksheets.CreateTestRequestRequest body)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var grants = await APP.Services.Authorization.CurrentPermissionSet.Load(context, actor);
        if (!grants.Contains(QcWorksheetPermissionKeys.CanLinkMaterialRetestTestRequest) ||
            !grants.Contains(QcWorksheetPermissionKeys.CanCreateUnscheduledQcTestRequest)) return TypedResults.Forbid();
        var result = await retests.CreateTestingAsync(id, body, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/certificates")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcCertificate)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CoaDetailDto>))]
    public async Task<IResult> Certificates(Guid id, [FromServices] ICoaRepository certificates)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var grants = await APP.Services.Authorization.CurrentPermissionSet.Load(context, actor);
        if (!grants.Contains(QcWorksheetPermissionKeys.CanViewQcCertificate) ||
            !grants.Contains(QcWorksheetPermissionKeys.CanViewMaterialRetestDetail)) return TypedResults.Forbid();
        var retest = await context.MaterialRetestRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (retest is null) return TypedResults.NotFound();
        if (!retest.TestRequestId.HasValue) return TypedResults.Ok(new List<CoaDetailDto>());
        var ids = await context.Coas.AsNoTracking().Where(item => item.TestRequestId == retest.TestRequestId)
            .OrderByDescending(item => item.RevisionNumber).Select(item => item.Id).ToListAsync();
        var rows = new List<CoaDetailDto>();
        foreach (var certificateId in ids)
        {
            var result = await certificates.GetCoa(certificateId);
            if (result.IsFailure) return result.ToProblemDetails();
            rows.Add(result.Value);
        }
        return TypedResults.Ok(rows);
    }

    private async Task<IResult> Decide(Guid id, MaterialRetestDecision body, bool approve)
    {
        if (!Actor(out var actor)) return TypedResults.Unauthorized();
        var roles = HttpContext.Items["Roles"] as List<Guid> ?? [];
        var result = await retests.DecideAsync(id, actor, roles, body, approve);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private bool Actor(out Guid actor)
    {
        actor = Guid.Empty;
        return Guid.TryParse(HttpContext.Items["Sub"] as string, out actor);
    }
}
