using APP.Extensions;
using APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

/// <summary>
/// Build brief 11: provider selection and key management for the AI fallback worksheet
/// extractor (build brief 10). Every endpoint here is gated by
/// <see cref="QcWorksheetPermissionKeys.CanManageAiWorksheetExtractionSettings"/>, deliberately
/// separate from <see cref="QcWorksheetPermissionKeys.CanUseAiWorksheetExtraction"/> (using the
/// feature day to day) — choosing which external vendor receives redacted worksheet text and
/// holding the only access to its key is a different authority. No endpoint here ever returns a
/// plaintext or ciphertext key; see <see cref="IAiExtractionSettingsService"/>.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/ai-extraction-settings")]
[Authorize]
public class QcAiExtractionSettingsController(IAiExtractionSettingsService settingsService) : ControllerBase
{
    /// <summary>Current provider statuses (masked key preview only) and the active provider.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanManageAiWorksheetExtractionSettings)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AiExtractionSettingsDto))]
    public async Task<IResult> GetSettings(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetAsync(cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Saves (or rotates) a provider's model and key. An empty/whitespace <c>apiKey</c> keeps
    /// the provider's current key and updates only the model — a key is write-only, so the
    /// Settings screen's key field is always blank; leaving it blank on an existing provider
    /// changes only the model. Rejects an empty/whitespace key when that provider has none yet.
    /// </summary>
    [HttpPut("{provider}/key")]
    [Authorize(QcWorksheetPermissionKeys.CanManageAiWorksheetExtractionSettings)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AiExtractionSettingsDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SaveProviderKey(
        [FromRoute] AiExtractionProvider provider,
        [FromBody] SaveAiExtractionProviderKeyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await settingsService.SaveProviderKeyAsync(
            provider, request.Model, request.ApiKey, Guid.Parse(userId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Activates a provider. Refused with <c>AiExtractionUnavailable</c> when that provider has
    /// no key saved yet — you can't activate a provider you haven't configured.
    /// </summary>
    [HttpPut("active")]
    [Authorize(QcWorksheetPermissionKeys.CanManageAiWorksheetExtractionSettings)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AiExtractionSettingsDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SetActiveProvider(
        [FromBody] SetActiveAiExtractionProviderRequest request, CancellationToken cancellationToken)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await settingsService.SetActiveProviderAsync(
            request.Provider, Guid.Parse(userId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record SaveAiExtractionProviderKeyRequest(string Model, string ApiKey);

public sealed record SetActiveAiExtractionProviderRequest(AiExtractionProvider Provider);
