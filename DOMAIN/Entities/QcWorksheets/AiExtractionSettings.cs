using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

// Build brief 11: provider selection and key management for the AI fallback extractor
// (build brief 10). Additive to the brief 10 data model — nothing here touches the
// existing worksheet import tables.

/// <summary>The AI provider the fallback extractor can be routed to. No other provider is in scope.</summary>
public enum AiExtractionProvider
{
    Anthropic = 0,
    OpenAi = 1
}

/// <summary>
/// One row per <see cref="AiExtractionProvider"/> (brief 11 decision 2 — a key is stored
/// encrypted, never in plaintext outside the request that set it). Switching the active
/// provider back and forth never requires re-entering a key, since both rows persist.
/// </summary>
public sealed class AiExtractionSettings : BaseEntity
{
    public AiExtractionProvider Provider { get; set; }
    public string Model { get; set; }

    /// <summary>Data-Protection-encrypted (<c>QcAiExtraction.ApiKey</c> purpose); never serialized to any DTO.</summary>
    public string EncryptedApiKey { get; set; }

    /// <summary>Last 4 characters only, computed before encryption. Not sensitive alone.</summary>
    public string KeyPreview { get; set; }

    public Guid UpdatedById { get; set; }

    // Deliberately non-nullable, unlike BaseEntity.UpdatedAt: this row is only ever created and
    // saved together, so "who/when set it" is always known, per the brief's data model.
    public new DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Singleton row recording which <see cref="AiExtractionProvider"/> is live. Kept separate from
/// <see cref="AiExtractionSettings"/> rather than a boolean flag on it, so "no provider chosen
/// yet" and "provider X is active" are both representable without a nullable enum column.
/// </summary>
public sealed class AiExtractionActiveProvider : BaseEntity
{
    public AiExtractionProvider Provider { get; set; }
    public Guid UpdatedById { get; set; }
    public new DateTime UpdatedAt { get; set; }
}
