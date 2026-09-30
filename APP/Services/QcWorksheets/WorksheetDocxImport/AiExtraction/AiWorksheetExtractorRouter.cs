using SHARED;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Build brief 11: <see cref="WorksheetDocxImportService"/> depends on <see cref="IAiWorksheetExtractor"/>
/// exactly as it did in build brief 10 — its constructor is unchanged — but DI now wires this
/// router in as that dependency instead of a concrete extractor directly (see
/// <c>DependencyInjection.AddTransientServices</c>). Resolves the active provider and its key via
/// <see cref="IAiExtractionSettingsService.ResolveActiveAsync"/>, then dispatches to the matching
/// concrete extractor. No key for the active provider (including no provider ever activated) →
/// <see cref="WorksheetImportErrors.AiExtractionUnavailable"/>, exactly the existing brief 10
/// error — the extractor is never constructed or invoked in that case. Routing happens after
/// <see cref="WorksheetDocxImportService"/> has already redacted the document; provider choice
/// never affects what gets redacted.
/// </summary>
public sealed class AiWorksheetExtractorRouter(
    IAiExtractionSettingsService settingsService,
    IAiWorksheetExtractorFactory extractorFactory) : IAiWorksheetExtractor
{
    public async Task<Result<AiExtractionResult>> ExtractAsync(
        RedactedDocument document, CancellationToken cancellationToken)
    {
        var resolved = await settingsService.ResolveActiveAsync(cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<AiExtractionResult>(resolved.Error);

        var extractor = extractorFactory.Create(resolved.Value.Provider, resolved.Value.ApiKey, resolved.Value.Model);
        return await extractor.ExtractAsync(document, cancellationToken);
    }
}
