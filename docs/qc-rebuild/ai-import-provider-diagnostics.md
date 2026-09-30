# QC worksheet AI import provider diagnostics (2026-09-29)

`POST /api/v1/qc/worksheets/templates/import` remains a read-only proposal operation.
Unknown ARD families reach AI only when the caller has
`CanUseAiWorksheetExtraction`. The response stays HTTP 200 with one proposal per
file and a flag for any AI failure; it does not save or approve a template.

The settings response's `hasKey: true` means encrypted key material exists. It
does not prove that the stored key can be decrypted, that the provider accepts
it, or that the provider request succeeds. The import now distinguishes:

The `LUFART DRY Powder.docx` screenshot shows `UnknownFamily` plus
`AiExtractionUnavailable`: this finished-product chemical layout has no
deterministic recognizer, so import reaches the AI fallback. With an active
provider and `hasKey: true`, an unreadable stored ciphertext is the likely
cause on a deployment still using the older generic error. The focused
key-ring regression test covers the exact `hasKey: true` / decrypt-failure
state and confirms that saving the key again restores extraction routing.

| Flag code | Meaning | Action |
| --- | --- | --- |
| `QcWorksheetTemplate.AiExtractionUnavailable` | No active provider or no stored key | Save a key and activate that provider. |
| `QcWorksheetTemplate.AiKeyUnreadable` | Stored ciphertext cannot be decrypted | Resave the provider key; check the persisted Data Protection key ring. |
| `QcWorksheetTemplate.AiProviderRejectedRequest` | Provider returned a non-rate-limit 4xx | Check provider status in backend logs, configured model, key and account access. |
| `QcWorksheetTemplate.AiProviderRateLimited` | Provider returned 429 | Retry after the provider's limit clears. |
| `QcWorksheetTemplate.AiProviderUnreachable` | Connection failed twice or provider returned 5xx | Check outbound connectivity and provider availability. |
| `QcWorksheetTemplate.AiResponseUngrounded` | Response could not be parsed or grounded to source text | Review the source and provider output; no partial proposal is accepted. |

The OpenAI request uses the same extraction fields as Anthropic but adapts the
JSON schema for strict structured output: every object rejects extra fields,
every field is required, and semantically optional values may be `null`.
The shared response parser still requires grounded `sourceQuote` values and
forces AI confidence to Low. Provider response bodies and API keys are not
returned in proposal flags or logged.

Documentation updated for this task: this note, `docs/services.md`,
`docs/workflows.md`, and `README.md`.
