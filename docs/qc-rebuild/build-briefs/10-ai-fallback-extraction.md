# Build brief 10 — AI fallback extraction for worksheet import

## Locked decisions (2026-09-27)

1. **Fallback only.** The 6 deterministic recognizers (product microbiology, culture
   media, environmental monitoring, purified water, raw-material chemical, raw-material
   specification) stay the primary, always-first path. They are validated against the
   real 63-file corpus and are reproducible — the same file always produces the same
   output, which matters for validation sign-off. Nothing here replaces them.
2. **AI only runs when classification produces no recognizer** — today that's
   `ArdFamily.Unknown` with evidence `"ANALYTICAL WORKSHEET (chemical)"` (a finished-
   product chemical worksheet) or no evidence at all (a genuinely new layout). A file
   that matches a built family always goes through its deterministic recognizer, never
   the AI path, even if the AI path exists.
3. **Hosted API is acceptable**, with mandatory redaction of run data before any text
   leaves the building — see Redaction below. This is not optional even though the API
   is approved for use, because staff names, AR numbers and batch numbers appear
   throughout the corpus and this dictionary already exists for exactly this reason
   (brief 07's `RunDataLabels`).
4. **Nothing AI-derived skips the Draft-plus-mandatory-review rule.** Every AI-derived
   field carries `Confidence = Low` (never Medium or High, regardless of what the model
   reports) and the new flag `AiExtracted`, on top of whatever other flags apply. An
   AI-proposed field with no grounded source location is refused outright rather than
   silently included.

## What triggers the AI path

In `WorksheetDocxImportService`, where the switch on `classification.Family` currently
returns immediately for `ArdFamily.Unknown` with flag `UnknownFamily`:

- If the evidence is `"STANDARD TEST PROCEDURE"`, keep refusing exactly as now — that is
  a different document kind entirely and AI extraction would be the wrong tool.
- Otherwise (a document that looks like an ARD but matches no built family), call the
  new `IAiWorksheetExtractor` instead of returning the refusal, **behind the permission
  key `CanUseAiWorksheetExtraction`** — a separate key from `CanImportQcWorksheetTemplates`,
  because this triggers a paid external API call per file and carries a materially
  different review burden. A caller without it gets today's plain refusal.

## Redaction (runs before any network call)

Reuse `RunDataLabels` exactly as the deterministic recognizers do: any value paired with
a run-data label (Batch No, A.R. No, Issue No/Date, Issued by, Sampled On/By, Mfg/Exp
Date, Date Received/Opened, Medium Batch no, Analysis Start/End) is replaced with a
placeholder (`[BATCH]`, `[NAME]`, `[DATE]`, `[AR_NUMBER]`, ...) before the document text
is sent. This is the same rule that keeps run data out of Constants — it now also keeps
it off the wire. A document whose redaction can't confidently strip a matched label (the
label matches but the value shape is unrecognized) is refused rather than sent
unredacted; this is a hard fail, not a warning.

## `IAiWorksheetExtractor`

```csharp
public interface IAiWorksheetExtractor
{
    Task<Result<AiExtractionResult>> ExtractAsync(
        RedactedDocument document, CancellationToken cancellationToken);
}

public sealed record AiExtractionResult(
    ProposedWorksheetTemplate Template,
    List<SpecificationCharacteristicProposal> SpecificationProposals,
    List<WorksheetImportFlag> Flags,
    int InputTokens,
    int OutputTokens);
```

- Implementation calls the Anthropic Messages API (model configurable, default
  `claude-opus-5-5`) through a named `HttpClient` ("AnthropicClient"), with the API key
  from configuration (`Anthropic:ApiKey`, environment-overridable, never checked in).
- **Structured output, not free text.** The request uses tool-use / a JSON schema
  matching `ProposedWorksheetTemplate` and `SpecificationCharacteristicProposal` exactly
  (the same DTOs the deterministic recognizers already populate) plus a required
  `sourceQuote` string per field — the exact text the field was read from. The response
  is deserialized strictly; a response that doesn't validate against the schema, or
  whose `sourceQuote` doesn't appear verbatim in the redacted document, is refused
  (`AiResponseUngrounded`) rather than partially accepted.
- Every field gets `Confidence = Low` and flag `AiExtracted` unconditionally, regardless
  of any confidence the model itself reports — the brief's own decision 4, enforced in
  code, not trusted from the response.
- A choice-phrase field the model proposes only becomes a Select if its options exactly
  match two mutually exclusive quoted phrases from the document — never invented options.
- Timeout, one retry on transient failure, no retry on a 4xx (bad request/auth) or on a
  schema-validation failure. Token usage is logged per import for cost tracking, but
  never blocks or retries based on cost.

## Provenance and flags

- New flag code `AiExtracted` — a distinct, non-dismissable banner on the review screen,
  not folded into the existing Medium/Low confidence styling.
- New flag code `AiDictionarySuggestion`: when the model encounters a run-data label or
  choice phrase it doesn't recognize but is confident is one, it proposes it as a
  **suggestion attached to the flag's own text**, never applied. A human adds it to
  `RunDataLabels` or the curated choice-phrase list in a follow-up PR, the same as any
  other code change — this is intentionally not a runtime-editable dictionary.
- `ImportFieldProvenance.Location` is populated from the grounded `sourceQuote`, exactly
  like the deterministic path's `ImportSourceLocation`, so "jump to source" behaves
  identically in the review screen regardless of which path produced the field.

## Tests

- A fake `HttpMessageHandler` stands in for the Anthropic API — **no test ever makes a
  real network call**, including in CI.
- Redaction: every run-data label in `RunDataLabels` is stripped from the text sent to
  the fake handler; a corpus file's real batch/AR numbers and staff names never appear
  in the captured outbound request body (asserted against the real corpus files under
  `QC_ARD_CORPUS_DIR`/`QC_RM_CORPUS_DIR`, read-only, never sent anywhere in tests).
- Confidence and flag are forced to Low/`AiExtracted` even when the fake response claims
  High confidence.
- An ungrounded `sourceQuote` (not present in the redacted text) is refused.
- A malformed/non-schema response is refused, not partially accepted.
- The permission gate: without `CanUseAiWorksheetExtraction`, an unrecognized file still
  gets the plain `UnknownFamily` refusal, unchanged from brief 07.
- A file that matches a built family never reaches the AI extractor, even when the key
  is held (assert the fake handler is never called).

## Frontend

- New permission key `CanUseAiWorksheetExtraction`. The import screen's proposal card
  shows a distinct amber "AI-extracted — verify every field against the source before
  saving" banner when any flag is `AiExtracted`, separate from the ordinary flag list.
- `AiDictionarySuggestion` flags are listed under a "Dictionary suggestions for the team"
  heading — informational, never actionable from the screen itself.
- Save is not blocked by these flags (the existing Draft-plus-review rule already governs
  this), but the banner cannot be dismissed for that upload.

## Configuration and rollout

- `Anthropic:ApiKey` must be supplied on staging/production before this path can be
  exercised; without it, `CanUseAiWorksheetExtraction` holders get a clear
  `AiExtractionUnavailable` error, not a silent failure.
- Not covered by this brief: fine-tuning, caching responses across near-duplicate files,
  or extending AI extraction to the 6 already-built families (explicitly ruled out by
  decision 1 above).
