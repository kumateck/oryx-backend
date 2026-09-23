# Milestone 1: StandardTestProcedure + WorksheetTemplate

Status: not started. No dependency on the two spikes in this folder — can start in
parallel with them (the STP-DOCX-parser spike is still unresolved; the
formula-engine spike is **resolved** — see
[00-formula-engine-spike.md](./00-formula-engine-spike.md)). Formula/CalculatedValue
field *configuration* (storing the formula string, syntax per
[field-catalog.md](../field-catalog.md)) and the actual evaluator (a small local
arithmetic + aggregate-function parser — not `FormulaWorksheetPreprocessor`, ruled
out by the spike) can both be built as part of this milestone; nothing is stubbed.

## Objective

Deliver `StandardTestProcedure` and `WorksheetTemplate` end to end: backend
entities, migration, endpoints, and the two frontend editors — structured-document
editor for STPs, drag-and-drop field-palette builder for WorksheetTemplates — fully
working for Draft authoring through Effective, including the DOCX import path for
STPs (using the design in
[phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md#stp-docx-import),
validated by [00-stp-docx-parser-spike.md](./00-stp-docx-parser-spike.md)).

This milestone does **not** include `Specification`, `TestRequest`, or any
execution/Test Room UI — those are later milestones and depend on what this one
produces (`WorksheetTemplate` must exist before `Specification` can link to it).

## Do-not-touch boundary

Everything in this brief is new, under `DOMAIN.Entities.QcWorksheets`,
`api/v{version}/qc/worksheets/...`, and `qc/worksheets/...`. Do not modify:
`MaterialSpecification`, `ProductSpecification`, `MaterialStandardTestProcedure`,
`ProductStandardTestProcedure`, `OosInvestigation`, any `RoutineQc*`/`Routine*`
entity, any existing controller, any existing migration, or
`quality-control.ts`/`quality-assurance.ts`. See
[README.md](../README.md#the-existing-materialproductpackaging-system-is-mature-not-shelved--and-the-new-module-coexists-with-it)
for why.

**One deliberate exception, read-and-extend, not modify**: this milestone
*subclasses* the existing `ResponsibleApprovalStage` (into `QcApproval`) and
*calls* the existing `IApprovalRepository`/`ApprovalController` machinery
(`Approval`, `ApprovalStage`, `IRequireApproval`) — the same way the Form module's
`ResponseApproval` already does. Do not alter any of those existing types or the
existing generic `ApprovalController`'s behavior; only add new code that consumes
them.

## Correction to phase-2-implementation-architecture.md

That doc said these entities follow `BaseEntity, IVerifiable`. That's wrong —
`IVerifiable` (confirmed by reading `MaterialSpecification`) is a simple single-flag
pattern (`IsVerified`/`VerifiedAt`/`VerifiedById`), not the five-state
Draft→UnderReview→Approved→Effective→Superseded lifecycle these entities need.
**Use `BaseEntity` only, with the entities' own `Status` enum and approval fields
below.** Fix the phase-2 doc's wording to match once this brief is implemented.

## Shared pattern: reuse the existing Approval engine, wrapped with QC re-auth

**Correction (2026-09-17): an earlier version of this brief introduced a custom
`QcESignature` audit table. That was a mistake — the codebase already has a
complete, generic, multi-stage approval engine
(`DOMAIN.Entities.Approvals`: `Approval`, `ApprovalStage`, `ResponsibleApprovalStage`,
`IRequireApproval`) used across the ERP, wired to a unified "My Pending Approvals"
inbox (`GET /api/v{version}/approval/my-pending`) and generic
`POST /api/v{version}/approval/approve/{modelType}/{modelId}` /
`.../reject/{modelType}/{modelId}` endpoints. `Response` (Form module) already
extends it via `ResponseApproval : ResponsibleApprovalStage`. Every QC entity
needing approval follows the exact same pattern — do not build a parallel
audit-log table.** Delete any reference to `QcESignature` from this and later
briefs; this section replaces it.

**The one real gap**: the generic engine's `ApproveItem`/`RejectItem` endpoints only
require `[Authorize]` (a valid session) — no re-authentication. Phase 1 locked full
meaning-of-signature (re-auth required) for QC specifically, as a deliberately
stricter bar than the rest of the company, confirmed when this gap was found. So QC
wraps the generic engine with its own thin re-auth layer rather than calling the
generic endpoints directly from the frontend.

**Centralized, not per-entity**: rather than a `Qc<Entity>Approval` subclass per
approval-bearing entity (which would scatter QC approval records across five
tables), there is **one** shared table covering every QC approval point —
StandardTestProcedure and WorksheetTemplate here; Specification, WorksheetInstance,
and OosCase in later milestones all reuse this same table. This is a deliberate
choice to make QC approvals manageable as one thing, even though it diverges
slightly from the Form module's `ResponseApproval`-per-entity convention — asked
for explicitly, prioritizing centralized manageability over strict pattern-matching:

```
QcApproval : ResponsibleApprovalStage   // subclasses the existing base class, same
                                          // as ResponseApproval does — but ONE
                                          // table for all QC entities, not one per
  Id: Guid
  EntityType: string    // "StandardTestProcedure" | "WorksheetTemplate" |
                         // "Specification" | "WorksheetInstance" | "OosCase" —
                         // free string, not a closed enum, so a later milestone
                         // never needs a migration to add a case
  EntityId: Guid        // no FK constraint (spans multiple tables); query by
                         // (EntityType, EntityId), same reasoning the earlier
                         // QcESignature design had for the same problem
  ApprovalId: Guid (required)     // FK to the Approval chain definition
  Approval: Approval
  ApprovalRound: int (default 1)  // mirrors ResponseApproval.ApprovalRound
  ReauthConfirmedAt: DateTime (required)   // the QC-specific addition beyond the
        // base class; set only after the re-auth wrapper below verifies the
        // caller's credentials, never client-supplied
```

Every approval-bearing entity implements `IRequireApproval` (`Approved: bool`) but
does **not** get its own child collection — approvals are looked up by
`(EntityType, EntityId)` against this one table. Each entity type registers its own
`modelType` string with the generic `Approval` system (e.g.
`"QcStandardTestProcedure"`, `"QcWorksheetTemplate"`) so an admin defines its
approval chain (stages, required approvers by user or role) through the existing
generic `Approval`/`ApprovalStage` configuration — QC doesn't build its own
chain-configuration UI either. A single `qc/worksheets/approvals` screen (see
Frontend, below) lists every pending QC approval across all five entity types in
one place, which is the actual point of centralizing the table this way.

**Re-auth wrapper endpoint shape** (used by every milestone from here on, not
repeated per-entity below):

```
POST api/v{version}/qc/worksheets/<resource>/{id}/approve
  body: { Password (or whatever re-auth credential the existing auth stack uses —
          verify against the actual Identity/SignInManager plumbing before
          implementing, don't assume a shape), Comments }
  1. Verify the re-auth credential against the CURRENT user's own credentials
     (not a generic password check — must be the same authenticated user
     re-proving themselves, matching what "meaning of signature" requires)
  2. On success: call the existing IApprovalRepository.ApproveItem(modelType,
     modelId, userId, roleIds, comments) — the exact same method the generic
     ApprovalController already calls — so all stage-routing/multi-approver logic
     is fully reused, zero duplication
  3. Record a QcApproval row (EntityType/EntityId set to this call's resource) with
     ReauthConfirmedAt set
```

Same shape for `.../reject`, calling `RejectItem` instead. `Comments` on the
generic `ApprovalRequestBody` carries what the earlier `QcESignature` design called
"Meaning"/"ReasonForChange" — no new free-text field needed beyond what
`ResponsibleApprovalStage` already has.

## Entities

```
StandardTestProcedure : BaseEntity, IRequireApproval
  Approved: bool   // from IRequireApproval; approvals themselves live in the
                    // single shared QcApproval table above, looked up by
                    // (EntityType = "StandardTestProcedure", EntityId = this Id)
  Code: string (required, max 100)
  Name: string (required, max 500)
  Area: string (max 200)
  Version: int (required, default 1)             // displayed as "Revision No." in
                                                   // the STP editor UI specifically —
                                                   // same underlying concept as
                                                   // WorksheetTemplate.Version below,
                                                   // named for schema consistency
  SupersedesId: Guid? (FK to StandardTestProcedure, self-referencing)
  EffectiveDate: DateTime?
  ReviewDate: DateTime?
  IssueDate: DateTime?
  Purpose: string (long text)
  Scope: string (long text)
  Responsibility: string (long text)
  Accountability: string (long text)
  Status: enum { Draft, UnderReview, Approved, Effective, Superseded }
  Steps: List<StpStep>

StpStep : BaseEntity
  StandardTestProcedureId: Guid (required)
  Order: int (required)
  Title: string? (max 200)
  Instruction: string (long text, required)
  ReferencedStpId: Guid? (FK to StandardTestProcedure — the structured
      cross-reference; nullable because not every step references another document)

WorksheetTemplate : BaseEntity, IRequireApproval
  Approved: bool   // looked up via QcApproval where EntityType = "WorksheetTemplate"
  Code: string (required, max 100)
  Name: string (required, max 500)
  Department: string (max 200)
  StpId: Guid? (FK to StandardTestProcedure)
  Category: enum { Chemical, Microbial, MediaQualification }
  Version: int (required, default 1)
  SupersedesId: Guid? (FK to WorksheetTemplate, self-referencing)
  EffectiveDate: DateTime?
  Status: enum { Draft, UnderReview, Approved, Effective, Superseded }
  Sections: List<WorksheetSection>

WorksheetSection : BaseEntity
  WorksheetTemplateId: Guid (required)
  Order: int (required)
  Name: string (required, max 200)
  InstrumentId: Guid? (optional linked Instrument, per field-catalog.md's
      Structure > Section entry)
  Fields: List<WorksheetField>

WorksheetField : BaseEntity
  WorksheetSectionId: Guid (required)
  Order: int (required)
  FieldKey: string (required, max 100 — unique within the WorksheetTemplate, not
      just within the section; enforce at the application layer across all
      sections of the same template, since CalculatedValue formulas reference
      FieldKeys worksheet-scoped, not section-scoped)
  Label: string (required, max 500)
  Type: enum { ShortText, LongText, Number, Date, Time, Select, MultiSelect,
      Checkbox, Table, Instructions, Heading, Measurement, CalculatedValue, Result,
      Instrument, Reagent, ReferenceStandard, Organism, Dilution,
      IncubationTemperature, IncubationPeriod, GrowthObservation, ColonyCount,
      CFUCalculation, ReferencedResult }
  Mode: enum { Constant, Entry, Calculated }
  Unit: string? (max 50)
  Analyte: string? (max 200)                      // optional multi-active tag
  ConstantValue: string?                           // populated only when Mode =
                                                     // Constant — the fixed method
                                                     // parameter text/value
  FormulaExpression: string?                        // populated only when
                                                     // Type = CalculatedValue or
                                                     // CFUCalculation; syntax
                                                     // resolved in field-catalog.md
                                                     // ({field_key}, AVG(...)/SUM(...)
                                                     // /MIN(...)/MAX(...)/RSD(...)
                                                     // over table_key.column_key) —
                                                     // parse and evaluate with a new
                                                     // small local evaluator, not
                                                     // FormulaWorksheetPreprocessor
  ColumnDefinitions: string?                        // populated only when
                                                     // Type = Table; JSON array of
                                                     // { label, type, unit } — see
                                                     // field-catalog.md's Table
                                                     // column-definition model
  ReferencedResultSourceTemplateId: Guid?           // populated only when
                                                     // Type = ReferencedResult
  ReferencedResultSourceFieldKey: string?
  ReferencedResultResolutionFieldKey: string?       // the FieldKey (elsewhere in
                                                     // this same template, typically
                                                     // a Reagent field) whose entered
                                                     // value is the runtime lookup
                                                     // key against the source
                                                     // template's instances
  Revisions: List<WorksheetFieldRevision>

WorksheetFieldRevision : BaseEntity
  WorksheetFieldId: Guid (required)
  // Same shape as WorksheetField's configurable properties (Label, Type, Mode,
  // Unit, ConstantValue, FormulaExpression, etc.), snapshotted whenever a field's
  // configuration changes within a Draft edit cycle — mirrors the existing
  // FormFieldRevision pattern already used by Form/FormField (see domain-model.md).
  // Populate this table on every save while Status = Draft/UnderReview; it is what
  // lets an author see field-level change history within one version, separate
  // from the WorksheetTemplate-level Version/SupersedesId chain which tracks
  // whole-document versions.
```

## Migration

`AddQcWorksheetTemplates` — creates `QcApproval`, `StandardTestProcedure`,
`StpStep`, `WorksheetTemplate`, `WorksheetSection`, `WorksheetField`,
`WorksheetFieldRevision`, in that dependency order. Matches
[phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md#migrations)'s
migration 1, expanded here to include the single shared `QcApproval` table (not
`QcESignature` — see the correction above). Does not create or modify `Approval`/
`ApprovalStage`/`ResponsibleApprovalStage` — those already exist.

## Backend permission keys

Add to `APP/Utils/QcWorksheetPermissionKeys.cs` (new file):

```
CanViewQcStps, CanCreateQcStp, CanEditQcStp, CanApproveQcStp, CanSupersedeQcStp,
CanImportQcStp
CanViewWorksheetTemplates, CanCreateWorksheetTemplate, CanEditWorksheetTemplate,
CanApproveWorksheetTemplate, CanSupersedeWorksheetTemplate
```

Exact same string values as [permissions.md](../permissions.md)'s `CanXxx`
translation table — do not invent new names.

## Backend endpoints

`StandardTestProcedureController` at `api/v{version}/qc/worksheets/stps`:

| Verb | Route | Permission | Request | Response | Notes |
|---|---|---|---|---|---|
| GET | `/` | `CanViewQcStps` | query: page, pageSize, search, status | paginated `StpSummaryDto[]` | list |
| GET | `/{id}` | `CanViewQcStps` | — | `StpDetailDto` (includes `Steps[]`) | |
| POST | `/` | `CanCreateQcStp` | `CreateStpRequest` (Code, Name, Area, Purpose, Scope, Responsibility, Accountability, Steps[]) | `StpDetailDto` | creates `Status = Draft`, `Version = 1` |
| PUT | `/{id}` | `CanEditQcStp` | `UpdateStpRequest` (same shape as create) | `StpDetailDto` | **only valid when `Status` is `Draft` or `UnderReview`** — see versioning below for the Effective case; if `UnderReview`, also resets `Status` to `Draft` |
| POST | `/{id}/create-new-version` | `CanEditQcStp` | — | `StpDetailDto` (the new Draft) | **only valid when `Status = Effective`**; clones content into a new `Version = current+1`, `Status = Draft`, `SupersedesId = {id}`; the source record is untouched |
| POST | `/{id}/submit-for-review` | `CanEditQcStp` | — | `StpDetailDto` | `Draft -> UnderReview`; calls `IApprovalRepository.CreateApproval` for `modelType = "QcStandardTestProcedure"`, `modelId = {id}` — this is what puts it into the approver(s)' pending-approvals queue |
| POST | `/{id}/approve` | `CanApproveQcStp` | `ApproveRequest` (re-auth credential, Comments) | `StpDetailDto` | the QC re-auth wrapper described above: verifies re-auth, then calls `ApproveItem("QcStandardTestProcedure", {id}, ...)`, then records a `QcApproval` row; `UnderReview -> Approved` |
| POST | `/{id}/make-effective` | `CanApproveQcStp` | — | `StpDetailDto` | `Approved -> Effective`; if `SupersedesId` is set, the superseded record transitions `Effective -> Superseded` in the same transaction |
| POST | `/{id}/supersede` | `CanSupersedeQcStp` | `SupersedeRequest` (re-auth, Comments/ReasonForChange required) | `StpDetailDto` | manual supersession outside the normal new-version flow (e.g. retiring a method entirely); same re-auth wrapper pattern, records a `QcApproval` row |
| POST | `/import` | `CanImportQcStp` | multipart: one or more `.docx` files | `StpImportResultDto[]` (per file: created `Draft` id, or parse failure reason, or list of flagged-for-review fields) | see [phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md#stp-docx-import); depends on [00-stp-docx-parser-spike.md](./00-stp-docx-parser-spike.md) |

`WorksheetTemplateController` at `api/v{version}/qc/worksheets/templates` — same
verb/lifecycle-endpoint shape as above (`GET /`, `GET /{id}`, `POST /`, `PUT /{id}`,
`POST /{id}/create-new-version`, `POST /{id}/submit-for-review`,
`POST /{id}/approve`, `POST /{id}/make-effective`, `POST /{id}/supersede`), permission
keys `CanViewWorksheetTemplates`/`CanCreateWorksheetTemplate`/
`CanEditWorksheetTemplate`/`CanApproveWorksheetTemplate`/
`CanSupersedeWorksheetTemplate` respectively, `modelType = "QcWorksheetTemplate"`
against the same shared `QcApproval` table, request/response DTOs carrying
`Sections[] -> Fields[]` instead of `Steps[]`. No import endpoint — worksheet
templates have no equivalent existing document library to migrate from.

## Frontend

New file `src/lib/permission-keys/qc-worksheets.ts` (partial — this milestone's
slice only; later milestones add to it):

```ts
export const qcWorksheetPermissions = {
  stps: { view, create, edit, approve, supersede, import: import_ },
  worksheetTemplates: { view, create, edit, approve, supersede },
} as const;
```

Routes:

```
qc/worksheets/stps                   list page: table with Code/Name/Area/Version/
                                      Status columns, filter by Status, search
qc/worksheets/stps/create            structured-document form: header metadata
                                      fields, four rich-text fields
                                      (Purpose/Scope/Responsibility/Accountability),
                                      a reorderable Steps list (add/remove/reorder
                                      step, each step: Title, rich-text Instruction,
                                      optional "link to another STP" picker)
qc/worksheets/stps/[id]              detail/edit — same form as create, pre-filled;
                                      read-only with a "Create New Version" button
                                      when Status = Effective, per the
                                      edit-triggers-versioning rule below
qc/worksheets/stps/import            file upload (multi-file), submit, then a
                                      results table (file name -> created Draft link
                                      / flagged fields / failure reason)

qc/worksheets/templates              list page, same column shape as stps
qc/worksheets/templates/create       drag-and-drop builder: left sidebar = field-type
                                      palette grouped exactly as in
                                      field-catalog.md (Structure/Basic/Scientific/
                                      Microbiology/Integration); canvas = the
                                      template's Sections, each a drop target;
                                      dropping a block adds a WorksheetField, opens
                                      an inline configuration panel for that field's
                                      Label/Type-specific settings/Mode/Unit
qc/worksheets/templates/[id]         same builder, pre-filled; read-only with a
                                      "Create New Version" button when
                                      Status = Effective

qc/worksheets/approvals              the centralized QC approvals queue — every
                                      pending QcApproval across every EntityType,
                                      not just STPs/Templates, so this list keeps
                                      growing as later milestones land; re-auth
                                      prompt (Password + Comments) inline on
                                      Approve/Reject, calling the re-auth wrapper
                                      endpoints per row's actual EntityType/route
```

### Edit-triggers-versioning implementation

Per [lifecycle-and-governance.md](../lifecycle-and-governance.md#edit-triggers-versioning):
the `[id]` detail page checks `Status` on load. `Draft`/`UnderReview` render the
form/builder directly, editable, autosaving or explicit-save (match whatever save
pattern `oryx-next`'s other forms already use — check `FormWizard` conventions per
`CLAUDE.md` before inventing a new one). `Effective`/`Superseded` render the same
form/builder in **read-only** mode with a single "Create New Version" button; that
button calls `POST /{id}/create-new-version` and navigates to the resulting new
Draft's `[id]` page. There is no path in the UI that allows typing into an Effective
record's fields directly — the read-only rendering is the enforcement, backed by the
`PUT /{id}` endpoint rejecting the call server-side if `Status = Effective` (never
trust client-side read-only alone).

## Acceptance criteria

Each scenario names the rule it proves:

1. **Version pinning / edit-triggers-versioning**: Create an STP, submit, approve,
   make effective. Click Edit — verify the form is read-only and a "Create New
   Version" button is present, not an editable form. Click it — verify a new Draft
   (Version 2) is created, `SupersedesId` points to Version 1, and Version 1's
   `Status` is still `Effective` (not yet `Superseded` — that only happens when
   Version 2 itself becomes Effective).
2. **Draft edits are free**: Create an STP (Draft). Edit it three times. Verify no
   new version is created each time — `Version` stays 1 throughout.
3. **UnderReview edit resets to Draft**: Submit an STP for review. Edit it. Verify
   `Status` is back to `Draft`, not still `UnderReview`.
4. **FieldKey uniqueness is worksheet-scoped**: Attempt to create two
   `WorksheetField`s with the same `FieldKey` in different `WorksheetSection`s of the
   same `WorksheetTemplate`. Verify the second is rejected.
5. **E-signature captured on approve**: Approve an STP. Verify a `QcApproval` row
   exists with `EntityType = "StandardTestProcedure"`, the correct `EntityId`,
   `ApprovedById`, `ApprovalTime`, and `ReauthConfirmedAt` — and that the endpoint
   rejects the call if the re-auth credential is missing or wrong, even when the
   caller's session token is otherwise valid. Also verify the underlying generic
   `Approval`/`ResponsibleApprovalStage` records were created via the real
   `IApprovalRepository`, not a bespoke parallel path.
6. **STP Steps cross-reference**: Create STP A. Create STP B with a step whose
   `ReferencedStpId` points to STP A. Verify the STP B detail view renders that step
   as a real link to STP A, not plain text.
7. **Coexistence**: After this milestone is built, verify every existing `qc/*`
   page (`qc/material-specification`, `qc/material-stp`, etc.) still loads and
   functions exactly as before — no shared code path was modified.
