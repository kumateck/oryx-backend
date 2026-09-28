# Build brief 11 — API contract: AI provider settings

Backend implementation for `docs/qc-rebuild/build-briefs/11-ai-provider-settings.md`.
All three endpoints live under `api/v{version}/qc/ai-extraction-settings` and require
the permission key **`CanManageAiWorksheetExtractionSettings`** (distinct from
`CanUseAiWorksheetExtraction` and `CanImportQcWorksheetTemplates`; register it in
`permission-keys/qc-worksheets.ts` on the frontend, filed under the same "QC Worksheet
Templates" submodule as those two).

No endpoint here ever returns a plaintext or ciphertext key — only a masked preview.

## Types

```ts
type AiExtractionProvider = 0 | 1; // 0 = Anthropic, 1 = OpenAi
```

> **Confirmed**: `API/Program.cs` registers no global `JsonStringEnumConverter` /
> `AddJsonOptions` enum-as-string configuration, so `AiExtractionProvider` serializes as
> a **plain integer** everywhere in this contract — `0` for Anthropic, `1` for OpenAi —
> in both responses and the `{provider}` route segment / request bodies below.

```ts
interface AiExtractionProviderStatusDto {
  provider: AiExtractionProvider;       // 0/Anthropic or 1/OpenAi (see note above)
  model: string | null;                 // null if never saved
  hasKey: boolean;
  keyPreview: string | null;            // e.g. "ab12" — last 4 chars only, null if hasKey is false
  updatedById: string | null;           // Guid
  updatedByName: string | null;         // "First Last"
  updatedAt: string | null;             // ISO 8601, null if hasKey is false
}

interface AiExtractionSettingsDto {
  activeProvider: AiExtractionProvider; // NOTE: non-nullable per the brief's locked DTO shape.
                                         // Before any provider has ever been activated, this
                                         // reports the enum default (Anthropic / 0) — check
                                         // providers[].hasKey for that provider to tell "really
                                         // active" from "never configured, defaulted".
  providers: AiExtractionProviderStatusDto[]; // always 2 entries, one per provider, Anthropic then OpenAi
}
```

## `GET /api/v{version}/qc/ai-extraction-settings`

- **Permission**: `CanManageAiWorksheetExtractionSettings`
- **Request**: none
- **Response 200**: `AiExtractionSettingsDto`
- **Errors**: 401 if unauthenticated, 403 if missing the permission.

## `PUT /api/v{version}/qc/ai-extraction-settings/{provider}/key`

- **Permission**: `CanManageAiWorksheetExtractionSettings`
- **Route param**: `provider` — `AiExtractionProvider` as the numeric value (`0` or `1`).
- **Request body**:
  ```json
  { "model": "claude-opus-5-5", "apiKey": "sk-ant-...." }
  ```
  - `model`: free text, stored as-is (no validation beyond non-null).
  - `apiKey`: required, non-empty after trimming whitespace.
- **Response 200**: `AiExtractionSettingsDto` (reflecting the saved provider's new
  `hasKey: true`, `keyPreview`, `updatedById`/`updatedByName`/`updatedAt`; `apiKey` is
  never echoed back).
- **Response 400** (`ProblemDetails`, from `Result.ToProblemDetails()`):
  - `apiKey` empty/whitespace → error code `QcWorksheetTemplate.AiExtractionKeyRequired`,
    message "An API key is required."
- **Behavior**: saving a key does **not** activate the provider — `PUT /active` is a
  separate, explicit step. Saving again for the same provider overwrites its key/model
  (rotation); the other provider's row is untouched, so switching back to it later never
  requires re-entering its key.

## `PUT /api/v{version}/qc/ai-extraction-settings/active`

- **Permission**: `CanManageAiWorksheetExtractionSettings`
- **Request body**:
  ```json
  { "provider": 1 }
  ```
- **Response 200**: `AiExtractionSettingsDto` with `activeProvider` updated.
- **Response 400** (`ProblemDetails`):
  - Target provider has no key saved yet → error code
    `QcWorksheetTemplate.AiExtractionUnavailable`, message "AI extraction is not
    available: no API key is configured for the active provider." — the same error code
    the worksheet import path already surfaces when the active provider's key goes
    missing, so the frontend can share one "AI extraction unavailable" message/banner
    for both contexts if desired.

## Error shape

All 400s are `Microsoft.AspNetCore.Http.HttpResults`/`ProblemDetails` via the existing
`Result.ToProblemDetails()` extension used across the QC module — same shape as every
other QC endpoint's validation error (`type`, `title`, `status`, `detail` carrying the
error description, and an `errors`/`code` field carrying the `Error.Code` string above).
Check an existing QC controller's 400 response in the running API/Swagger UI to confirm
the exact envelope field names before wiring client-side error branching, since that
shape is produced by shared infrastructure this brief didn't change.

## Frontend screen (brief's spec, for reference)

Settings → AI Extraction: two provider cards (Anthropic, OpenAi), each showing masked
key preview or "Not configured", model, last updated by/at, an **Activate** button
(disabled when `hasKey` is false), and a **Set key** form (provider fixed per card,
model text input, password-style API key input always blank on load, "leave blank to
keep the current key" helper text — **note**: the current backend `PUT .../key` request
always requires a non-empty `apiKey` and always overwrites; there is no
partial-update/"leave blank" support on the backend today. If the frontend needs true
"leave blank to keep the current key" semantics, that requires either a backend change
(optional `apiKey` in the request, kept as-is when omitted) or client-side omission of
the PUT call entirely when the key field is left blank and only `model` changed — flag
this back if the product behavior must match the brief's "leave blank" copy literally).
The active provider gets a badge, matching the Draft/Effective pattern elsewhere in the
module. No changes to the import screen itself.
