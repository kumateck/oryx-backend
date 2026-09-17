# Phase 2 — Implementation Architecture

Date: 2026-09-17. Status: designed, not built. Purely additive — see
[README.md](./README.md#the-existing-materialproductpackaging-system-is-mature-not-shelved--and-the-new-module-coexists-with-it)
for why nothing here touches the existing Material/Product/Packaging system,
`OosInvestigation`, or the shelved routine/water implementation.

This maps [domain-model.md](./domain-model.md) onto real `oryx-backend`/`oryx-next`
conventions, confirmed against the existing codebase rather than invented — the
permission-key style, controller shape, and route structure below all match patterns
already in production elsewhere in the app.

## Namespacing — how collision is avoided

Everything new lives under one clearly-named root, distinct from every existing QC
route:

| Layer | Existing (untouched) | New module |
|---|---|---|
| Backend namespace | `DOMAIN.Entities.MaterialSpecifications`, `.ProductSpecifications`, `.OosInvestigations`, `.QualityRoutines` | `DOMAIN.Entities.QcWorksheets` (single namespace for all seven core entities) |
| API routes | `api/v{version}/material-specifications`, `api/v{version}/qa/oos-investigations`, `api/v{version}/qc/routines` | `api/v{version}/qc/worksheets/...` |
| Frontend routes | `qc/material-specification`, `qc/product-specification`, `qc/material-stp`, `qc/analytical-raw-data`, `qc/routines/*` | `qc/worksheets/...` |
| Frontend permission keys | `src/lib/permission-keys/quality-control.ts`, `quality-assurance.ts` | `src/lib/permission-keys/qc-worksheets.ts` (new file) |
| Backend permission keys | `APP/Utils/PermissionUtils.cs` (role names), ad hoc keys per controller | `APP/Utils/QcWorksheetPermissionKeys.cs` (new file, following the `FullProcedurePermissionKeys.cs` pattern exactly) |

No existing controller, entity, migration, route, or permission key is modified.

## Backend entities

One EF Core class per object in [domain-model.md](./domain-model.md), all under
`DOMAIN.Entities.QcWorksheets`, using `BaseEntity` throughout. **Correction
(2026-09-17, caught while writing the Milestone 1 build brief): these entities are
NOT `BaseEntity, IVerifiable`** — `IVerifiable` (confirmed by reading
`MaterialSpecification`) is a simple single-flag pattern
(`IsVerified`/`VerifiedAt`/`VerifiedById`), not the five-state
Draft→UnderReview→Approved→Effective→Superseded lifecycle these entities need. Each
carries its own `Status` enum, implements the codebase's existing
`IRequireApproval` interface, and reuses the existing generic `Approval`/
`ApprovalStage`/`ResponsibleApprovalStage` engine for sign-off — **not** a bespoke
audit table (an earlier version of this doc and of Milestone 1 introduced one,
`QcESignature`; that was a mistake, corrected once the existing engine was found —
see
[01-stp-and-worksheet-templates.md](./build-briefs/01-stp-and-worksheet-templates.md)
for the full reasoning and exact shape):

```
QcApproval : ResponsibleApprovalStage   // ONE shared table across every QC entity
                                          // below, not one per entity — centralized
                                          // deliberately, so all QC approvals are
                                          // manageable from a single queue.
                                          // Subclasses the existing base class the
                                          // same way Response module's
                                          // ResponseApproval already does.
  EntityType, EntityId, ApprovalId, ApprovalRound, ReauthConfirmedAt
  // Approved via a QC-specific re-auth wrapper in front of the existing generic
  // IApprovalRepository.ApproveItem/RejectItem — the workflow engine (stages,
  // routing, the "my pending approvals" inbox) is 100% reused, not rebuilt

StandardTestProcedure : BaseEntity, IRequireApproval
  StpStep  (child entity -- Order, Title, Instruction, ReferencedStpId)
WorksheetTemplate : BaseEntity, IRequireApproval
  WorksheetSection, WorksheetField, WorksheetFieldRevision  (child entities)
Specification : BaseEntity, IRequireApproval
  SpecificationWorksheetLink, SpecificationCharacteristic  (child entities)
SamplingPoint : BaseEntity           // master data, added in Milestone 6
MonitoringProgram : BaseEntity
TestRequest : BaseEntity
  TestRequestSubject  (child entity)
WorksheetInstance : BaseEntity, IRequireApproval
  WorksheetFieldValue  (child entity)
  WorksheetInstanceReassignment  (plain audit record, NOT routed through Approval —
      reassignment is administrative, not a sign-off)
OosCase : BaseEntity, IRequireApproval    // note the casing match to OosInvestigation for consistency
Coa : BaseEntity
  CoaRow  (child entity, snapshotted — never live-joined)
WaterQualityPeriod : BaseEntity
WaterUseRecord : BaseEntity
```

`WorksheetFieldRevision` mirrors the existing `FormFieldRevision` pattern already
used for `Form`/`FormField` — fields already have revisioning precedent in this
codebase, so `WorksheetField` reuses that shape rather than inventing a new one.

## Migrations

One migration per logical group, matching the existing convention
(`<timestamp>_<Description>.cs`) and the grouping style already used for the routine
implementation (`AddRoutineQcAnalysis`, `FinalizeRoutineWorksheet`,
`AddCommercialMicrobialQualityAnalysis`, `AddWaterQualityCoverage`):

1. `AddQcWorksheetTemplates` — `QcApproval` (the single shared approval table,
   not per-entity), `StandardTestProcedure`, `StpStep`, `WorksheetTemplate`,
   `WorksheetSection`, `WorksheetField`, `WorksheetFieldRevision`.
2. `AddQcSpecifications` — `Specification`, `SpecificationWorksheetLink`,
   `SpecificationCharacteristic`.
3. `AddQcMonitoringPrograms` — `MonitoringProgram`.
4. `AddQcTestRequestsAndInstances` — `TestRequest`, `TestRequestSubject`,
   `WorksheetInstance`, `WorksheetFieldValue`, `WorksheetInstanceReassignment`.
5. `AddQcOosCases` — `OosCase`.
6. `AddQcCertificates` — `Coa`, `CoaRow`.
7. `AddQcWaterQualityCoverage` — `SamplingPoint`, `WaterQualityPeriod`,
   `WaterUseRecord`, plus the `TestRequestSubject.SamplingPointId` alteration
   (reusing the name from the shelved implementation's equivalent migration
   deliberately, since the mechanics were validated correct there — see
   [domain-model.md](./domain-model.md#water-specific-extension)).

Sequenced this way because each migration's entities only reference entities from
earlier migrations — no forward references, no need to apply out of order.

## Backend permission keys (`QcWorksheetPermissionKeys.cs`)

Translating [permissions.md](./permissions.md)'s dotted-notation keys into the
codebase's real `CanXxx` convention (`nameof()` constants, exactly like
`FullProcedurePermissionKeys`):

```
CanViewQcStps, CanCreateQcStp, CanEditQcStp, CanApproveQcStp, CanSupersedeQcStp,
CanImportQcStp

CanViewWorksheetTemplates, CanCreateWorksheetTemplate, CanEditWorksheetTemplate,
CanApproveWorksheetTemplate, CanSupersedeWorksheetTemplate

CanViewQcSpecifications, CanCreateQcSpecification, CanEditQcSpecification,
CanApproveQcSpecification, CanSupersedeQcSpecification

CanViewMonitoringPrograms, CanCreateMonitoringProgram, CanEditMonitoringProgram,
CanPauseMonitoringProgram

CanViewQcWaterQualityPeriods, CanActivateQcWaterQualityPeriod, CanHoldQcWaterQualityPeriod,
CanRecordQcWaterUse

CanViewQcTestRequests, CanCreateScheduledQcTestRequest, CanCreateUnscheduledQcTestRequest,
CanRecordQcSample, CanAssignQcTestRequest

CanAssignWorksheet, CanReassignWorksheet
CanStartChemicalWorksheet, CanStartMicrobialWorksheet
CanEnterChemicalWorksheetResult, CanEnterMicrobialWorksheetResult
CanSubmitChemicalWorksheet, CanSubmitMicrobialWorksheet
CanReviewChemicalWorksheet, CanReviewMicrobialWorksheet
CanReturnWorksheetForCorrection

CanInvestigateQcOosCase, CanAuthorizeQcOosRetest, CanDispositionQcOosCase

CanIssueQcCertificate, CanReviseQcCertificate, CanViewQcCertificate
```

`QcSpecification`/`QcTestRequests`/`QcOosCase`/`QcCertificate` are prefixed `Qc`
throughout specifically to avoid any name collision with the existing
`RawMaterialSpecification`-style and `OosInvestigation`-style keys already declared
in `quality-control.ts`/`quality-assurance.ts` — two keys with the same short name in
different files would still be distinct strings, but the prefix makes the two
systems visually unambiguous in code review and in the permissions admin UI.

## Backend controllers

One controller per major resource, `[Authorize]` at class level plus
`[Authorize(QcWorksheetPermissionKeys.CanXxx)]` per action — same shape as
`ProcedureController`/`RoutineQcController`:

```
WorksheetTemplateController      api/v{version}/qc/worksheets/templates
SpecificationController          api/v{version}/qc/worksheets/specifications
MonitoringProgramController      api/v{version}/qc/worksheets/monitoring-programs
TestRequestController            api/v{version}/qc/worksheets/test-requests
WorksheetInstanceController      api/v{version}/qc/worksheets/instances
OosCaseController                api/v{version}/qc/worksheets/oos-cases
CoaController                    api/v{version}/qc/worksheets/certificates
WaterQualityController           api/v{version}/qc/worksheets/water-quality
StandardTestProcedureController  api/v{version}/qc/worksheets/stps
```

Business logic sits in an `APP/Services/QcWorksheets/` layer (partial classes per
concern, matching the `TemplateFormService.Reads.cs`/`.Writes.cs`/`.Lifecycle.cs`
split already used for Full Procedures) rather than directly in an
`APP/Repository/QcWorksheetRepository.cs` — the state machines here (OOS flow,
version pinning, COA combination gating) are complex enough to warrant that split,
unlike the simpler CRUD-shaped `RoutineQcController` → `IRoutineQcRepository` that
was sufficient for the shelved implementation.

`StandardTestProcedureController` additionally exposes
`POST api/v{version}/qc/worksheets/stps/import` (batch — accepts multiple `.docx`
files in one call, `[Authorize(QcWorksheetPermissionKeys.CanImportQcStp)]`), backed
by a new `StpDocxImportService`. This is a migration tool, not a data source: it
parses `.docx` files, never reads the old `MaterialStandardTestProcedure`/
`ProductStandardTestProcedure` tables, and produces `Draft`-status
`StandardTestProcedure`/`StpStep` rows for human review — it never auto-approves.

## STP DOCX import — spike run, validated against 8 real files

**Spike resolved 2026-09-17: go** (see
[build-briefs/00-stp-docx-parser-spike.md](./build-briefs/00-stp-docx-parser-spike.md)
for the full results table). 8/8 real files parsed cleanly — 6 raw-material STPs
and 2 finished-product STPs, both `STP No.` prefixes (`QCD/STP/RM/xxx`,
`QCD/STP/FP/xxx`) covered. Three real corrections to the design below came out of
debugging the first pass, which failed on every file before these were found:

1. **The header metadata table lives in a Word running header part
   (`word/headerN.xml`), not the document body.** Every file tested had it in
   `header2.xml` specifically, with `header1.xml`/`header3.xml` present but empty
   (first-page/even-page headers, unused) — `StpDocxImportService` must check all
   `word/header*.xml` parts in the zip and use whichever has content, not assume a
   fixed part name or that it's in `word/document.xml` at all.
2. **No space between the section number and heading word** in the real text —
   `"1.0Purpose :"`, not `"1.0 Purpose:"`. Confirmed identically across all 8 files.
3. **The document body is effectively one large table with long concatenated cell
   text, not discrete paragraphs.** Matching must search within the full joined
   text, not line-by-line.

`StpDocxImportService`:

1. Reads the header part (per correction 1) into `Code`/`Name`/`Area`/`RevisionNo`/
   `Reference`/dates. `Code` extraction needs a pattern anchored to the confirmed
   format (`QCD/STP/(RM|FP)/\d{3}`), not a greedy character class — a naive one
   over-captures into the adjacent product name text, since there's no separator
   there either (same root cause as correction 2).
2. Splits the body by numbered heading (per corrections 2–3) into the four fixed
   rich-text fields.
3. Splits `5.0 Procedure` into `StpStep` rows by sub-numbering.
4. Pattern-matches "Refer to `<code>`" text (confirmed working across all 8 files,
   real `QCD/SOP/xxx` codes extracted correctly) and attempts to resolve it against
   existing STP/SOP codes, populating `ReferencedStpId` where a confident match is
   found.
5. Anything it can't confidently place — an unresolved cross-reference, an
   ambiguous section boundary, an unusual layout — is flagged inline on the created
   Draft rather than silently guessed at or dropped. Not yet tested against
   Environmental/Microbial-category STPs or the full numbered library beyond the
   8-file sample — expect some documents to need this fallback in practice.

Every imported STP still goes through the exact same
Draft→UnderReview→Approved→Effective lifecycle and e-signature requirements as one
authored from scratch (see [lifecycle-and-governance.md](./lifecycle-and-governance.md))
— import only saves retyping, it never shortcuts review. Batch upload matters here
specifically because of the volume already on hand: the numbered raw-material
library alone runs past 20 entries, and every dosage-form folder has its own set.

## Frontend

New permission-keys file, same nested-object shape as `research-development.ts`:

```ts
// src/lib/permission-keys/qc-worksheets.ts
export const qcWorksheetPermissions = {
  stps: { view, create, edit, approve, supersede, import },
  worksheetTemplates: { view, create, edit, approve, supersede },
  specifications: { view, create, edit, approve, supersede },
  monitoringPrograms: { view, create, edit, pause },
  waterQuality: { view, activatePeriod, holdPeriod, recordUse },
  testRequests: { view, createScheduled, createUnscheduled, recordSample, assign },
  worksheets: {
    assign, reassign,
    startChemical, startMicrobial,
    enterChemicalResult, enterMicrobialResult,
    submitChemical, submitMicrobial,
    reviewChemical, reviewMicrobial,
    returnForCorrection,
  },
  oosCases: { investigate, authorizeRetest, disposition },
  certificates: { issue, revise, view },
} as const;
```

New route tree under `src/app/(private)/qc/worksheets/` — complete coverage of every
entity in [domain-model.md](./domain-model.md), none omitted:

```
qc/worksheets/stps                   (list + structured-document editor, [id]/edit —
                                       four fixed rich-text fields plus a reorderable
                                       Steps list, each step with an optional structured
                                       cross-reference to another STP/SOP; NOT the
                                       field-palette builder — see field-catalog.md)
qc/worksheets/stps/import            (batch .docx upload; shows created Drafts with
                                       parse-confidence flags, each linking into the
                                       normal editor above for review/correction —
                                       the migration path for the existing STP library)
qc/worksheets/templates              (list + field-palette builder, [id]/edit)
qc/worksheets/specifications         (list + builder, [id]/edit — links WorksheetTemplates
                                       and, for STP-backed templates, surfaces the linked STP)
qc/worksheets/monitoring-programs    (calendar view from test-room-ux.md)
qc/worksheets/water-quality          (period list, activate/hold, use-record log —
                                       carries forward the live system's WaterQualityPeriod/
                                       WaterUseRecord screen shape, against the new model)
qc/worksheets/test-requests          (ARD list/detail, create scheduled+unscheduled,
                                       assign — the QC Manager configuration/ops surface
                                       the roles matrix already assumes exists)
qc/worksheets/test-room              (My Work, tabbed Chemical/Microbial — test-room-ux.md)
qc/worksheets/review                 (Awaiting My Review queue)
qc/worksheets/oos-cases              ([id] detail = investigation + disposition)
qc/worksheets/certificates           ([id] = COA viewer)
```

Every screen here is genuinely new UI, not a retrofit of an existing QC page — the
existing `qc/*` pages are untouched and keep serving the old system throughout.
Distinct editor patterns cover the module, not one generic one: `stps` uses a
structured-document editor (four fixed rich-text fields, plus a reorderable Steps
list — each step rich text with an optional structured cross-reference to another
STP/SOP, closing the untraceable "Refer to QCD/SOP/053" plain-text pattern found
throughout the real documents); `templates` uses the field-palette builder described
in the worksheet-building discussion above. Conflating the two would be the wrong
call — an STP's content is prose with a version history and no captured values, not
a set of typed data-capture fields.

## Formula/calculation engine — resolved

**Spike run, decision made** (see
[build-briefs/00-formula-engine-spike.md](./build-briefs/00-formula-engine-spike.md)):
build a new, small local evaluator for `CalculatedValue`/table-aggregate fields —
do not reuse `APP/Services/Formulas/FormulaWorksheetPreprocessor`. Reading it in
full showed it's `internal` (not accessible outside its own namespace without
modifying existing code, which the coexistence rule forbids) and isn't a local
evaluation core at all — it's a thin adapter shipping the actual expression
evaluation to an external formula microservice with its own versioned DSL and
hash-locked definitions, tuned for manufacturing batch-weight rounding. Taking a
network dependency on that service for every QC field calculation is the wrong
tradeoff for something that should feel instant in the Test Room. No existing local
expression-evaluation library is already a dependency of the backend either. See
field-catalog.md for the resolved formula syntax
(`{field_key}`, `AVG({table_key.column_key})`, etc.) that Milestone 1's
`WorksheetField.FormulaExpression` stores.

## Suggested build sequence within Phase 2

Not a hard dependency chain, but the natural order given what depends on what:

1. `StandardTestProcedure` + `WorksheetTemplate` (configuration, no execution yet)
2. `Specification` (needs WorksheetTemplate to link against)
3. `TestRequest` + `WorksheetInstance` (execution — the Test Room becomes usable)
4. `OosCase` (needs WorksheetInstance field values to react to)
5. `Coa` (needs Specification + released TestRequests)
6. `MonitoringProgram` + `WaterQualityPeriod`/`WaterUseRecord` (Routine/Water —
   deliberately last, since it's the category with the most novel mechanics and the
   least existing live system to reconcile against, matching its position first in
   the Phase 3 proving sequence for a different reason: lowest risk to prove there,
   but soundest to build last here since it depends on 1–5 all existing first)
