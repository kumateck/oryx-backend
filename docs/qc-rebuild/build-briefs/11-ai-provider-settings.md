# Build brief 11 — AI provider selection and key management

Extends build brief 10. Today the AI fallback extractor is hard-wired to Anthropic, with
the key read from `Anthropic:ApiKey` config/env, requiring a redeploy to change the
provider or rotate a key. This brief adds **OpenAI as a second provider** and an
in-app, admin-managed settings screen: pick the active provider, its model, and paste
its key, without a redeploy.

## Locked decisions (2026-09-28)

1. **Two providers: Anthropic and OpenAI.** Both extractors are always registered; only
   one is *active* at a time, chosen by the stored setting. No other provider is in
   scope.
2. **Keys are stored encrypted in the database**, not only in environment config. An
   admin sets them from a Settings screen. A key is **write-only after saving** — no
   endpoint ever returns the plaintext key back, only a masked preview
   (`sk-...ab12`, last 4 characters) plus who/when set it.
3. **Encryption via ASP.NET Core Data Protection** (`IDataProtector`), not a hand-rolled
   cipher — this app has an unused hand-rolled AES helper
   (`APP/Extensions/StringExtensions.cs`) that is deliberately **not** reused here;
   Data Protection is the framework-native mechanism and manages key rotation itself.
   **Data Protection's own key ring must be persisted to the database** (via
   `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, a new package reference,
   pointed at the existing `ApplicationDbContext`) — not the default local-filesystem
   keys, which would make an encrypted API key undecryptable after a restart or on any
   second instance behind a load balancer. This is a deployment-correctness requirement,
   not an optional hardening step.
4. **Everything else from brief 10 is unchanged**: redaction before any network call,
   `Confidence` forced to `Low` and flag `AiExtracted` added in code regardless of
   provider response, an ungrounded `sourceQuote` refused, no partial accept, the AI path
   only ever runs for a file matching no built recognizer, gated by
   `CanUseAiWorksheetExtraction`. None of that is provider-specific and none of it moves.
5. **A separate permission key for configuring this**, `CanManageAiWorksheetExtractionSettings`
   — distinct from `CanUseAiWorksheetExtraction` (using the feature) and from
   `CanImportQcWorksheetTemplates` (importing at all). Choosing which external vendor
   receives redacted worksheet text, and holding the only access to its key, is a
   different authority from using the feature day to day.
6. **No key configured for the active provider → `AiExtractionUnavailable`**, exactly the
   existing brief 10 error, not a silent fallback to a different provider.

## Data model

```csharp
public enum AiExtractionProvider { Anthropic = 0, OpenAi = 1 }

public sealed class AiExtractionSettings : BaseEntity
{
    public AiExtractionProvider Provider { get; set; }
    public string Model { get; set; }
    /// <summary>Data-Protection-encrypted; never serialized to any DTO.</summary>
    public string EncryptedApiKey { get; set; }
    /// <summary>Last 4 characters only, for the masked preview. Not sensitive alone.</summary>
    public string KeyPreview { get; set; }
    public Guid UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Single active row per provider (two rows total, one per `AiExtractionProvider` value) —
not one row that's overwritten, so switching back to a previously-configured provider
doesn't require re-entering its key. A third `ActiveProvider` row/flag (or a single
`AiExtractionActiveProvider` singleton row) records which one is live.

Migration: three new tables/columns — keep it additive only, matching every prior QC
migration in this series.

## `IAiExtractionSettingsService`

```csharp
public interface IAiExtractionSettingsService
{
    Task<Result<AiExtractionSettingsDto>> GetAsync(CancellationToken ct);
    Task<Result<AiExtractionSettingsDto>> SetActiveProviderAsync(
        AiExtractionProvider provider, Guid actorId, CancellationToken ct);
    Task<Result<AiExtractionSettingsDto>> SaveProviderKeyAsync(
        AiExtractionProvider provider, string model, string apiKey, Guid actorId,
        CancellationToken ct);
    /// <summary>Decrypts and returns the active provider's key. Internal use only — never
    /// exposed through a controller.</summary>
    Task<Result<(AiExtractionProvider Provider, string Model, string ApiKey)>>
        ResolveActiveAsync(CancellationToken ct);
}

public sealed record AiExtractionSettingsDto(
    AiExtractionProvider ActiveProvider,
    IReadOnlyList<AiExtractionProviderStatusDto> Providers);

public sealed record AiExtractionProviderStatusDto(
    AiExtractionProvider Provider, string Model, bool HasKey, string KeyPreview,
    Guid? UpdatedById, string UpdatedByName, DateTime? UpdatedAt);
```

`SaveProviderKeyAsync` encrypts with `IDataProtector` (a purpose-named protector, e.g.
`dataProtectionProvider.CreateProtector("QcAiExtraction.ApiKey")`) and stores
`KeyPreview` as the input key's last 4 characters, computed before encryption.

## Extractor routing

`WorksheetDocxImportService` no longer depends on `IAiWorksheetExtractor` directly.
Introduce `AiWorksheetExtractorRouter : IAiWorksheetExtractor` that:

1. Calls `ResolveActiveAsync`. No key for the active provider → `Result.Failure` with
   `AiExtractionUnavailable`, exactly as brief 10 already returns when config is empty.
2. Dispatches to the matching concrete extractor — `AnthropicWorksheetExtractor` or the
   new `OpenAiWorksheetExtractor` — both still registered as named `HttpClient`s in DI
   (`"AnthropicClient"`, `"OpenAiClient"`), both still receiving the already-redacted
   `RedactedDocument` from the caller (routing happens **after** redaction, not before —
   redaction must never depend on which provider ends up receiving the text).
3. Every enforcement from brief 10 (`Confidence = Low`, `AiExtracted` flag, grounding
   check) stays exactly where it already lives and applies identically regardless of
   which concrete extractor produced the raw result.

## `OpenAiWorksheetExtractor`

Same contract as `AnthropicWorksheetExtractor` (`IAiWorksheetExtractor`, one
`ExtractAsync(RedactedDocument, CancellationToken)`). Calls OpenAI's Chat Completions API
with **structured outputs** (`response_format: { type: "json_schema", json_schema: {...},
strict: true }`) using the identical JSON schema already built for the Anthropic tool-use
call — one shared schema-building helper, not two copies — so both providers are held to
exactly the same shape, including the required `sourceQuote` per field. Same grounding
check, same forced `Confidence = Low`/`AiExtracted`, same refuse-on-malformed,
same one-retry-on-transient/no-retry-on-4xx-or-schema-failure rule as brief 10. Default
model configurable via the settings row, not hardcoded (unlike brief 10's
`AnthropicSettings.Model` default, which stays as the fallback only when no DB row
exists yet for Anthropic).

## Endpoints (`QcAiExtractionSettingsController`, `api/v{version}/qc/ai-extraction-settings`)

| Verb | Route | Permission | Body / Notes |
|---|---|---|---|
| GET | `/` | `CanManageAiWorksheetExtractionSettings` | Returns `AiExtractionSettingsDto` — never a key |
| PUT | `/{provider}/key` | `CanManageAiWorksheetExtractionSettings` | `{ model, apiKey }`. Rejects an empty/whitespace key. |
| PUT | `/active` | `CanManageAiWorksheetExtractionSettings` | `{ provider }`. Refuses if that provider has no key saved yet (`AiExtractionUnavailable`) — you can't activate a provider you haven't configured. |

## Tests

- Settings service: save then resolve round-trips the key (decrypts to the same value);
  `KeyPreview` never contains more than 4 characters of the real key; `GetAsync` never
  includes ciphertext or plaintext in its DTO.
- Switching `ActiveProvider` without a key for that provider is refused.
- `AiWorksheetExtractorRouter`: dispatches to the correct concrete extractor per active
  provider (fake `IAiWorksheetExtractor` stand-ins for both, asserting only the active
  one is invoked); missing key → `AiExtractionUnavailable`, extractor never invoked.
- `OpenAiWorksheetExtractor`: identical fake-`HttpMessageHandler` coverage to brief 10's
  Anthropic tests — forced Low/`AiExtracted` regardless of response, ungrounded quote
  refused, malformed response refused, no test ever touches the network.
- Confirm the shared schema-building helper produces byte-identical JSON schema content
  for both providers' requests (a single source of truth, not two hand-maintained
  copies that could drift).
- Permission tests for all three new endpoints, and the existing
  `CanUseAiWorksheetExtraction` gate in `WorksheetDocxImportService` remains
  behaviorally unchanged (still tested against the router now, not the concrete class).

## Frontend

- New **Settings → AI Extraction** screen (permission `CanManageAiWorksheetExtractionSettings`,
  new key registered in `permission-keys/qc-worksheets.ts`), showing both providers as
  cards: masked key preview or "Not configured", model, last updated by/at, an
  **Activate** button (disabled without a key), and a **Set key** form (provider is
  fixed per card; model text input; API key password-style input that is always blank on
  load — never pre-filled — with helper text "leave blank to keep the current key" only
  shown when a key already exists for that card).
- The active provider is visually marked (badge), matching the pattern already used for
  Draft/Effective status elsewhere in this module.
- No changes to the import screen itself — brief 10's `AiExtracted` banner and flag
  handling are already provider-agnostic (they only render on the flag code, never on
  which provider produced it).

## Deployment note (not code, but must be called out)

Adding `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` and persisting the key
ring to the database means the **first deploy of this brief generates a new Data
Protection key ring row on first run** — this only protects data encrypted *after* this
deploy; nothing existing needs migrating, since brief 10 shipped no DB-stored keys. If
this backend ever runs as more than one instance behind a load balancer, they must share
this same database (already true for every other piece of this app's state) — no new
shared-storage requirement is introduced.

## Out of scope

- Any provider beyond Anthropic and OpenAI.
- Per-user or per-company provider settings (this is one company-wide setting, matching
  how `Anthropic:ApiKey` worked in brief 10).
- Automatic key rotation/expiry reminders.
