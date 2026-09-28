using DOMAIN.Entities.QcWorksheets;
using SHARED;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Build brief 11: stores which AI provider (<see cref="AiExtractionProvider"/>) the fallback
/// extractor (build brief 10) is routed to, and its API key. A key is write-only after saving —
/// <see cref="GetAsync"/> never returns it, only a masked <see cref="AiExtractionProviderStatusDto.KeyPreview"/>.
/// Only <see cref="ResolveActiveAsync"/> decrypts a key, and only for <see cref="AiWorksheetExtractorRouter"/>'s
/// internal use — it is never exposed through a controller or DTO.
/// </summary>
public interface IAiExtractionSettingsService
{
    Task<Result<AiExtractionSettingsDto>> GetAsync(CancellationToken cancellationToken);

    /// <summary>Refuses with <see cref="WorksheetImportErrors.AiExtractionUnavailable"/> when the target provider has no key saved yet.</summary>
    Task<Result<AiExtractionSettingsDto>> SetActiveProviderAsync(
        AiExtractionProvider provider, Guid actorId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a provider's model and key. An empty/whitespace <paramref name="apiKey"/> keeps
    /// that provider's current key unchanged (only the model is updated) when it already has
    /// one — the Settings screen never re-displays a saved key, so this is how "leave blank to
    /// keep the current key" is honored without asking the reviewer to re-enter it. Refuses
    /// with <see cref="WorksheetImportErrors.AiExtractionKeyRequired"/> for an empty/whitespace
    /// key on a provider with no key saved yet.
    /// </summary>
    Task<Result<AiExtractionSettingsDto>> SaveProviderKeyAsync(
        AiExtractionProvider provider, string model, string apiKey, Guid actorId, CancellationToken cancellationToken);

    /// <summary>
    /// Decrypts and returns the active provider's key. Internal use only — never exposed
    /// through a controller. Refuses with <see cref="WorksheetImportErrors.AiExtractionUnavailable"/>
    /// when no key is configured for the active provider (including when no provider has ever
    /// been activated).
    /// </summary>
    Task<Result<(AiExtractionProvider Provider, string Model, string ApiKey)>> ResolveActiveAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// <paramref name="ActiveProvider"/> matches the brief's locked signature (non-nullable); before
/// any provider has ever been activated it reports the enum default (<see cref="AiExtractionProvider.Anthropic"/>)
/// with that provider's <see cref="AiExtractionProviderStatusDto.HasKey"/> false — the same "not
/// configured" signal the UI already renders per card, so nothing is misrepresented as live.
/// </summary>
public sealed record AiExtractionSettingsDto(
    AiExtractionProvider ActiveProvider,
    IReadOnlyList<AiExtractionProviderStatusDto> Providers);

public sealed record AiExtractionProviderStatusDto(
    AiExtractionProvider Provider,
    string Model,
    bool HasKey,
    string KeyPreview,
    Guid? UpdatedById,
    string UpdatedByName,
    DateTime? UpdatedAt);
