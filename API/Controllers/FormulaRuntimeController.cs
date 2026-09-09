using System.Text.Json;
using APP.Extensions;
using APP.Services.Formulas;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/formula-runtime")]
[Authorize]
public sealed class FormulaRuntimeController(
    IFormulaCalculationClient calculationClient,
    IFormulaResponseRuntimeService responseRuntime,
    FormulaCalculationSettings settings) : ControllerBase
{
    [HttpGet("status")]
    [Authorize(PermissionKeys.CanViewQuestions)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> Status(CancellationToken cancellationToken)
    {
        var result = await calculationClient.IsReadyAsync(cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(new
            {
                schemaVersion = "oryx-formula-runtime-status-v1",
                ready = result.Value,
                authorityEnabled = settings.Enabled
            })
            : result.ToProblemDetails();
    }

    [HttpPost("definitions/validate")]
    [Authorize(PermissionKeys.CanEditQuestions)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormulaServiceResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> ValidateDefinition(
        [FromBody] JsonElement request,
        CancellationToken cancellationToken)
    {
        var result = await calculationClient.ValidateAsync(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("responses/{responseId:guid}/snapshots")]
    [Authorize(PermissionKeys.CanExecuteFormulaResponse)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> EnsureSnapshots(
        Guid responseId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await responseRuntime.EnsureInitialSnapshotsAsync(
            responseId, actorId, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("responses/{responseId:guid}/placements/{placementKey}/evaluate")]
    [Authorize(PermissionKeys.CanExecuteFormulaResponse)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormulaExecutionDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> EvaluateStoredInputs(
        Guid responseId,
        string placementKey,
        [FromBody] FormulaEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await responseRuntime.EvaluateStoredInputsAsync(
            responseId, placementKey, request.IdempotencyKey, actorId,
            DOMAIN.Entities.Formulas.FormulaExecutionTrigger.DraftEdit, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private bool TryActor(out Guid actorId) =>
        Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);
}
