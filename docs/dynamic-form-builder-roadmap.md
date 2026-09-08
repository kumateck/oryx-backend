# Dynamic form builder: architecture assessment and roadmap

## Purpose

This is an assessment of the `Form` / `FormSection` / `FormField` / `Question` / `Response`
module (UI name: "Template") against how mature dynamic-form and LIMS/COA systems are built,
plus a phased roadmap for closing the gaps. It is a report for prioritization, not an
implementation plan — no schema or code changes are included here.

The module backs QC Specifications, Analytical Raw Data (ARD), and Certificate of Analysis
(COA) generation. The goals driving this review: Sections should formally represent **Specs**
and Fields/Questions should formally represent **Tests** within them; a COA should be
producible after a Response is submitted with a trustworthy, deterministic compliance signal
per test (not a heuristic guess); and the engine should be general enough to configure any
kind of form, not only QC, without forking the schema again.

## Current state

The module is more unified than it might first appear. `ProductSpecification` /
`MaterialSpecification` (`DOMAIN/Entities/ProductSpecifications/ProductSpecification.cs`,
`DOMAIN/Entities/MaterialSpecifications/MaterialSpecification.cs`) and
`MaterialAnalyticalRawData` / `ProductAnalyticalRawData`
(`DOMAIN/Entities/MaterialARD/`, `DOMAIN/Entities/ProductAnalyticalRawData/`) are thin
metadata wrappers around one generic `Form` (`DOMAIN/Entities/Forms/Form.cs`). `FormSection`
doubles as "Test": `Name` is the test name, `Description` carries the spec/acceptable-range
text, and it optionally links to `MaterialSpecification`, `ProductSpecification`, and
`QcEquipment`. `FormField` → `Question` (`DOMAIN/Entities/Forms/Question.cs`) is a reusable
question bank with 15 fixed `QuestionType` values (`ShortAnswer`, `LongAnswer`, `Paragraph`,
`Datepicker`, `SingleChoice`, `Dropdown`, `Checkbox`, `FileUpload`, `Signature`, `Reference`,
`Formula`, `Specification`, `Equipment`, `Table`, `Instrument`). Every answer lands in
`FormResponse.Value` (a bare string) under a `Response` tied to a batch, BMR, or production
step.

COA generation already works end-to-end. `FormRepository.GenerateCertificateOfAnalysis(...)`
(`APP/Repository/FormRepository.cs`, ~line 1188) validates the response, applies
section-level `Complies` overrides submitted as `CertificateOfAnalysisComplies` payloads,
transitions the batch/BMR status, and starts the approval workflow — with an OOS
investigation triggered on COA rejection (`API/Controllers/OosInvestigationController.cs`).
The frontend renders a full printable COA
(`oryx-next/src/components/pages/qa/approvals/details/ard/coa.tsx` and siblings under
`qc/goods-receipt-note/details/ard/` and `production/analytical-test-request/ard/`) purely by
reading `FormSectionDto[]` + `FormResponseDto[]`. There is no separate `Coa` table by design,
and this roadmap does not propose adding one — see Decisions below.

## Gaps versus industry standard

1. **No real answer validation.** `Question.Validation` (`QuestionValidationType`: Number /
   Letter / Alphanumeric / None) is persisted but never read anywhere in
   `CreateQuestion`/`UpdateQuestion` or response submission. A catalog of typed validation
   errors exists (`FormErrors.cs`: `InvalidNumericResponse`, `InvalidDateResponse`,
   `InvalidEmailResponse`, `InvalidOptionResponse`, etc.) but is dead code — nothing enforces
   that a numeric result is numeric, a date is a date, or a choice answer is one of
   `Question.Options`.
2. **Acceptance criteria are unstructured prose.** A test's expected range or result lives in
   `FormSection.Description` as free text. Compliance is inferred client-side by
   regex-parsing that text (`inferCompliance()` in
   `oryx-next/src/components/pages/qc/analytical-raw-data/shared/coa-compliance.ts`) —
   fragile, not auditable, and computed on the client instead of a single backend source of
   truth.
3. **No general versioning or audit trail for ordinary questions.** `Question` is a shared
   bank entity; editing one retroactively changes every `Form` and historical `Response` that
   referenced it. A governed revisioning system (`FormRevision` / `FormFieldRevision` in
   `DOMAIN/Entities/Formulas/FormulaPlacements.cs`) exists, but it was built specifically —
   and very recently (migrations dated 2026-09-05) — to support `Formula`-type computed
   fields, not ordinary questions.
4. **Closed, hand-duplicated question-type enum.** The same 15-value `QuestionType` enum is
   kept in sync by hand across the backend C# enum, a hand-written frontend TS enum
   (`oryx-next/src/lib/enum.ts`), and a generated OpenAPI numeric union with no semantic
   meaning (`0 | 1 | ... | 14`). Three places to update per new type, with real risk of
   drift.
5. **No conditional logic.** Nothing in the model or renderers supports showing or hiding a
   field based on another field's answer.
6. **A second, fully independent implementation exists.** `AuditChecklistTemplate` /
   `AuditChecklistResponse` (`DOMAIN/Entities/QualityAudits/`) reinvents "template + questions
   + responses" with a fixed `ChecklistResponseStatus` enum and no shared `Question` entity
   at all — direct evidence the current engine isn't yet reusable enough to be the default
   choice for a new form-like feature.
7. **The core engine is QC/manufacturing-coupled, not domain-agnostic.** `Form`,
   `FormSection`, `Response`, and `FormAssignee` all carry direct foreign keys to
   `MaterialBatch`, `BatchManufacturingRecord`, `ProductionActivityStep`,
   `MaterialSpecification`, `ProductSpecification`, and `QcEquipment`.

**Industry reference points.** Schema-driven form engines (form.io, SurveyJS,
JSON-Schema-based tools) treat the form definition as a versioned, declarative asset that is
separate from its renderer. FHIR's `Questionnaire` / `QuestionnaireResponse` split cleanly
separates a form's definition (with validation constraints expressed on it) from a filled
instance, linked by a stable `linkId`. In LIMS/pharma specifically, a Certificate of Analysis
is compiled from structured spec parameters (min/max/unit/method) that drive deterministic
pass/fail, and is issued as a controlled release document under GMP / 21 CFR Part 11
expectations.

## Decisions

These were confirmed in discussion and scope the roadmap below:

- **Test/specification criteria move from free text to structured fields** (min/max/unit/
  target value/comparison operator/method reference). This is the top-priority
  recommendation and is what unblocks reliable, deterministic compliance determination.
- **COA stays a computed view.** No new `CertificateOfAnalysis` persistence table. What must
  change is the reliability of its compliance signal (backend-computed from structured
  criteria, not client-side regex) and the robustness of the manual override path so a
  reviewer can confirm or force compliance with an auditable reason, not just a bare boolean.
- **`QualityAudits` stays separate for now.** Not folded into the generic engine as part of
  this effort — revisit after the core engine itself is hardened (see Phase 6).

## Roadmap

### Phase 1 — Structured test criteria and deterministic compliance

Highest priority; directly addresses the COA compliance concern.

- Add structured acceptance-criteria fields at the `FormField` level — placement-specific,
  following the existing pattern where `Required`/`Rank`/`Description` already override
  per-placement rather than living on the shared `Question`: a comparison operator (Equals,
  Between, GreaterThan, LessThan, OneOf, etc.), `MinValue`/`MaxValue`/`TargetValue`, `Unit`,
  `ExpectedText` (for qualitative results like "White crystalline powder" or "Complies"), and
  a test-method reference.
- Add a single backend `ComplianceEvaluationService` that computes `FormResponse.Complies`
  deterministically from `FormResponse.Value` against the `FormField`'s structured criteria.
  This becomes the one source of truth; the frontend's `inferCompliance()` regex parsing is
  replaced with a call to this evaluation, kept only for display.
- Harden `GenerateCertificateOfAnalysis` / `CertificateOfAnalysisComplies`: default to the
  auto-evaluated result, but let an authorized reviewer override per-section with a required
  reason/comment, preserving both the automatic result and the override for audit purposes
  (this also feeds the existing OOS-investigation trigger on rejection).
- Migration shape: additive nullable columns only. Existing free-text `Description` remains
  for display/context. Backfilling structured criteria onto existing Specification-linked
  sections is a best-effort, reviewed data task — not automatic.

### Phase 2 — Real answer validation

- Wire the already-defined `QuestionValidationType` and `FormErrors` catalog into
  `CreateFormResponse` / `SubmitFormResponseFinal`: numeric/date/format checks, and
  option-membership checks for Dropdown/SingleChoice/Checkbox against `Question.Options`.
- Mirror the same rules client-side (today `local-form.tsx` only checks presence for
  `Required` fields) so users get immediate feedback, with the backend as the enforced
  source of truth.

### Phase 3 — General versioning for ordinary forms and questions

- Extend the existing `FormRevision`/`FormFieldRevision` governance pattern — built for
  `Formula` fields — to cover all question types, so editing a shared `Question` doesn't
  silently rewrite history for prior `Response`s.

### Phase 4 — Conditional logic

- Add a lightweight visibility-rule model on `FormField` (e.g. "show Field X only if Field Y
  = value Z"), evaluated both server-side (validation) and client-side (rendering).

### Phase 5 — Question-type extensibility

- Reduce the three-way hand duplication of `QuestionType` to a single generated source of
  truth at minimum; consider a metadata-driven type definition longer-term so new answer
  types don't require a backend rebuild plus edits to the ~4 frontend switch statements that
  branch on it (`local-form.tsx`, `value-renderer.tsx`, `question-preview.tsx`, and the
  FormWizard renderers). Note the codebase already has a working registry pattern for
  ordinary app forms (`oryx-next/src/components/form-inputs/wizard/field-renderer-registry.ts`)
  that was never applied to this dynamic-question rendering path — reuse it here rather than
  inventing a new mechanism.

### Phase 6 — Decouple the core engine from QC-specific foreign keys

- Replace the direct `MaterialBatch`/`BatchManufacturingRecord`/`ProductionActivityStep`
  foreign keys on `Form`/`Response`/`FormAssignee` with a generic polymorphic "subject" link,
  so any domain (HR forms, generic surveys, audits) can attach to the same engine without
  null-FK abuse. This is the natural point to revisit folding `QualityAudits` onto the engine
  instead of leaving it as a permanent fork — deferred until Phases 1–4 are proven out.

## Suggested execution order and rationale

Phases are ordered by dependency and by how directly they address the stated goals: Phase 1
is asked for explicitly and is a prerequisite for a trustworthy COA; Phase 2 is low-risk and
reuses scaffolding that already exists; Phase 3 is what makes the engine safe to trust for
regulated data long-term; Phases 4–6 are genuine extensibility investments best undertaken
once the foundation is solid, and Phase 6 is the natural trigger to reconsider unifying
`QualityAudits`.
