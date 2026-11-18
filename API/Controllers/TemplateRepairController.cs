using APP.Extensions;
using APP.Services.Forms;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

/// <summary>
/// Template repair: fields added to a worksheet template after its tests were assigned are given, on open tests, to analysts
/// already on those tests, so the worksheets show them and they can be filled. Saved answers are never changed.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/template-repairs")]
[Authorize]
public sealed class TemplateRepairController(TemplateRepairService service) : ControllerBase
{
    [HttpGet("forms/{formId:guid}", Name = "GetTemplateRepairContext")]
    [Authorize(PermissionKeys.CanViewTemplateRepair)]
    [ProducesResponseType(typeof(TemplateRepairContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> Context(Guid formId, [FromQuery] Guid? formAssigneeId = null) => Run(() => service.Context(formId, formAssigneeId));

    [HttpPost("forms/{formId:guid}/preview", Name = "PreviewTemplateRepair")]
    [Authorize(PermissionKeys.CanViewTemplateRepair)]
    [ProducesResponseType(typeof(TemplateRepairPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Preview(Guid formId, TemplateRepairCandidate request) => Run(() => service.Preview(formId, request));

    [HttpPost("forms/{formId:guid}/apply", Name = "ApplyTemplateRepair")]
    [Authorize(PermissionKeys.CanApplyTemplateRepair)]
    [ProducesResponseType(typeof(TemplateRepairResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Apply(Guid formId, ApplyTemplateRepairRequest request) => Run(() => service.Apply(formId, request));

    [HttpGet("forms/{formId:guid}/history", Name = "GetTemplateRepairHistory")]
    [Authorize(PermissionKeys.CanViewTemplateRepair)]
    [ProducesResponseType(typeof(List<TemplateRepairHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IResult> History(Guid formId) => Run(() => service.History(formId));

    private static async Task<IResult> Run<T>(Func<Task<Result<T>>> action)
    {
        try { var result = await action(); return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails(); }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505") { return Changed(); }
    }
    private static IResult Changed() => Result.Failure(Error.Conflict("TemplateRepair.ConcurrentChange",
        "The template, its tests or your permissions changed. Reload the repair preview.")).ToProblemDetails();
}
