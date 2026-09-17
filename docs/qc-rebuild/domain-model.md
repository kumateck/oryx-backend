# Domain Model

Seven core objects carry the whole system. Every trace from a certificate back to an
analyst's raw entry runs through `FieldKey` — a stable, human-assigned identifier that
specifications, cross-worksheet references, and COA rows all resolve against instead
of a field's position on a form.

```
StandardTestProcedure (STP)                // structured document, distinct from both of the below --
                                            // not the field-palette builder (no data is captured),
                                            // not a single free-text blob either
  Code (e.g. QCD/STP/RM/004), Name, Area
  RevisionNo, Supersedes, EffectiveDate, ReviewDate, IssueDate
  Purpose, Scope, Responsibility, Accountability  (four fixed rich-text fields)
  Steps[] -> { Order, Title (optional), Instruction (rich text),
               ReferencedStpId (optional -- a structured link to another STP/SOP,
                   replacing the plain-text "Refer to QCD/SOP/053" pattern seen
                   throughout the real documents with a real, renumbering-safe
                   reference) }
  Status: Draft -> UnderReview -> Approved -> Effective -> Superseded

WorksheetTemplate                          // fully independent entity, not the shared Form/Question engine
  Code, Name, Department
  StpId (optional -- the STP this worksheet is the structured data-capture
      implementation of; a real filled ARD prints Spec No. and STP No. side by side,
      so the worksheet's STP reference flows onto every WorksheetInstance printout)
  Category: Chemical | Microbial | MediaQualification
  Version, EffectiveDate, Status: Draft -> UnderReview -> Approved -> Effective -> Superseded
  Sections[] -> Fields[]
      Field: FieldKey (required, unique within template), Label, Type, Unit
             Mode: Constant | Entry | Calculated
             Analyte (optional -- multi-active products)
      Field type "Table": ColumnDefinitions[] (each a small field def: label, type,
          unit); rows repeat, fixed-count or open-ended; columns are heterogeneous
          (see field-catalog.md for the full precision writeup)
      Field type "ReferencedResult": SourceWorksheetTemplateId + SourceFieldKey +
          a runtime resolution key (e.g. paired with a Reagent-type batch-number
          field, since many instances of the source template exist over time)
      CalculatedValue: formula over any FieldKey in the same WorksheetInstance
          (worksheet-scoped, not section-scoped) and/or a table-column aggregate
          (Sum, Average, Min, Max, %RSD/StdDev)
  ChangeHistory[]

Specification
  Code, Name, AppliesTo: RawMaterial | PackagingMaterial | Product |
      RoutineWater | RoutineEnvironmental  (split Material into RawMaterial/
      PackagingMaterial to match the live system's existing separation --
      quality-control.ts keys rawMaterialSpecifications/packagingMaterialSpecifications
      and rawMaterialStps/packagingMaterialStps separately throughout; a unified
      "Material" would have been a real regression from what's already established.
      Caught late -- see build-briefs/04-oos-cases.md's corrections pattern)
  Stage: Intermediate | Bulk | Finished  (required only when AppliesTo = Product;
      null otherwise -- carries forward AnalyticalTestRequest.Stage and
      ProductSpecification.TestStage from the live system, which this design
      missed entirely until flagged. A product's Intermediate/Bulk/Finished
      Specifications are genuinely different documents with different
      Characteristics, not tiers of one Specification -- e.g. Intermediate often
      skips Dissolution and Microbial entirely, which Finished always requires.

      Confirmed against 8 real BMR/BPR pairs spanning every dosage form actually
      manufactured -- Amoxicillin 250mg Capsules (Beta), Amurox 500mg Tablet
      (Beta), Albendazole 400mg Tablet (Non-Beta), Entrima 2% Cream (Ointment),
      Paracetamol 120mg Syrup (Liquid), Ancigel Suspension (Liquid), Lufart Dry
      Powder for Suspension, and ORS Orange (Sachet): every single one defines
      exactly two QA-sampling/QC-testing gates in its BMR/BPR text, never one and
      never three -- one mid-process gate plus a Finished gate at packaging.
      Which mid-process gate appears is determined by manufacturing method, not
      chosen freely per product: granulation-based solids (capsules, tablets,
      dry powder for suspension, sachets) gate on Intermediate ("Intermediate
      granules Sampling and Release by QA, Testing by QC", right after
      blending/granulation, before fill or compression); liquid-mix or
      semi-solid-mix forms (syrup, suspension, cream/ointment) gate on Bulk
      ("Sampling, Testing and Release of bulk", right after product preparation,
      before filling). Intermediate and Bulk never both appear on the same
      product, and no product in the sample used all three stages or shipped on
      a Finished-only gate. Practical implication: seeding/UX for a Product's
      Specifications should default to offering the Finished + [Intermediate |
      Bulk] pair based on the product's dosage form/manufacturing method, not
      present all three stages as equally likely choices -- the enum itself
      stays 3-valued since a future process could genuinely need all three)
  Version, EffectiveDate, Status  (same lifecycle as WorksheetTemplate)
  RetestPolicy: SameSample | FreshResample  (required, no default -- every
      Specification must declare this explicitly; added in
      build-briefs/04-oos-cases.md after lifecycle-and-governance.md's retest
      policy rule was locked without ever landing on this entity)
  WorksheetLinks[] -> { WorksheetTemplateId, AnalysisType: Chemical | Microbial }
      (1 or 2 rows -- Environmental only ever has Microbial; Water always has both;
       Material/Product varies per MicrobialRequirement)
  Characteristics[] -> { TestName, Analyte (optional), AcceptanceCriteria,
                          AlertLimit (optional, EM/Water two-tier), ActionLimit,
                          SamplingPointGroupId (optional FK, for grouped/tiered
                              limits -- dropdown-selected, never free text, since a
                              typo here silently applies the wrong Alert/Action
                              tier; see build-briefs/02-specifications.md),
                          SourceWorksheetTemplateId, SourceFieldKey,
                          IncludeOnCoa, DisplayOrder, GroupName }
  // The same SourceWorksheetTemplateId/SourceFieldKey pair may appear on more than
  // one Characteristic, differentiated by SamplingPointGroupId -- how one EM test
  // (e.g. Airborne Viables) gets separate Alert/Action tiers per room classification.
  // MediaQualification-category WorksheetTemplates typically have no Specification
  // linking to them at all -- they produce no COA. Their Result fields carry their
  // own inline Constant-mode acceptance criteria instead (see field-catalog.md).

SamplingPointGroup                         // master data: Name, Description
  Introduced ahead of MonitoringProgram (build sequence) because Specification
  needs it first -- see build-briefs/02-specifications.md. MonitoringProgram below
  references the same table once built; this is not a duplicated concept.

SamplingPoint                              // master data: Code, Name, Area,
                                            // Type (Water|Environmental),
                                            // SamplingPointGroupId (FK) --
                                            // introduced in build-briefs/06, since
                                            // MonitoringProgram.SamplingPoint was
                                            // only ever a loose string until then

MonitoringProgram                          // Routine/Water/Environmental scheduling
  SamplingPointId (FK, one point per program), SpecificationId (singular -- the
      linked Specification's own WorksheetLinks determine Chemical/Microbial/both,
      matching TestRequest's design; corrected from an earlier "SpecificationId(s)"
      framing)
  Frequency: Daily | Weekly | Monthly | Quarterly | Custom(interval)
  LeadTimeDays, NextDueDate (advanced at generation time, not completion time --
      see build-briefs/06-monitoring-programs-and-water-quality.md)
  Status: Active | Paused
  // The daily due-date scan groups every program due the same day sharing Type +
  // SpecificationId into ONE TestRequest with one TestRequestSubject per program --
  // "one point per program" is a configuration-granularity decision, distinct from
  // the real one-round-many-points execution pattern real EM/Water documents show

TestRequest (the ARD -- the round)
  Type: RawMaterial | PackagingMaterial | Product | RoutineWater |
      RoutineEnvironmental  (mirrors Specification.AppliesTo)
  SpecificationId + SpecificationVersion (pinned at creation -- shared by every
      Subject in the round, since one round tests against one Specification)
  ScheduleOrigin: Scheduled | Unscheduled(Reason -- mandatory)
  ArNumber, IssueNumber, IssuedAt/By  (round-level; matches existing
      AnalyticalTestRequest fields)
  Status: Draft -> Sampled -> Assigned -> InTesting -> ResultsComplete -> UnderReview -> Released | Rejected
  Subjects[] -> TestRequestSubject   // explicit child entity, not a scalar field --
      see build-briefs/03-test-requests-and-instances.md for why: real EM/Water
      worksheets confirm one round routinely covers 60-90 rooms or 15+ water points,
      which a single SubjectRef on TestRequest itself can't represent. Material/
      Product rounds normally have exactly one Subject; the shape is unified rather
      than forking the model per category.

TestRequestSubject                         // one per batch (Material/Product) or
                                            // one per sampling point (Water/EM)
  TestRequestId
  SubjectRef (batch number, or sampling point code, e.g. "SF-91")
  SubjectLabel (optional human name alongside the code, e.g. "Deblistering-2")
  ArNumber (optional per-point AR sub-number -- confirmed on the real Water
      worksheet, nested under the round's own ArNumber)
  SamplingPointGroupId (optional FK, Water/EM only)
  SamplingPointId (optional FK to SamplingPoint -- Water/EM only; added in
      build-briefs/06-monitoring-programs-and-water-quality.md, auto-populated when
      system-generated by a MonitoringProgram, optional for manually-added
      unscheduled Subjects)
  MaterialBatchId (optional FK to the existing MaterialBatch entity --
      RawMaterial/PackagingMaterial only; added in build-briefs/04-oos-cases.md so
      OOS quarantine has a real batch to act on, not just a SubjectRef string)
  BatchManufacturingRecordId (optional FK to the existing BatchManufacturingRecord
      entity -- Product only, same reason)
  CollectedAt
  RequiredWorksheets[] resolved from Specification.WorksheetLinks at TestRequest
      creation time, materialized as one WorksheetInstance per (Subject x
      WorksheetLink) pair

WorksheetInstance                          // one per required worksheet, per Subject
  TestRequestSubjectId, WorksheetTemplateId + WorksheetTemplateVersion (pinned)
  Header block (rendered, not stored as fields): Batch No, AR No, Spec No +
      Revision, STP No, Issue No, Mfg/Exp/Sampled/Analysis/Issue dates -- derived
      live from TestRequest + Specification + WorksheetTemplate.StpId; every real
      filled ARD reviewed prints exactly this block, and it is never analyst-entered
  AssignedToId, AssignedById, AssignedAt  (see "Assignment and reassignment" below)
  FieldValues[] -> { FieldKey, Value, EnteredBy, EnteredAt,
                      ResolvedFromInstanceId (if ReferencedResult) }
  RetestOfInstanceId (nullable -- links a retest to its original, never overwrites it)
  Status: NotStarted -> InProgress -> Submitted -> Reviewed -> Locked
  Approvals via the shared QcApproval table (EntityType = "WorksheetInstance"),
      reusing the codebase's existing generic Approval engine, wrapped with QC
      re-authentication -- see build-briefs/01-stp-and-worksheet-templates.md and
      lifecycle-and-governance.md

OOSCase                                     // supersedes OosInvestigation -- see note below
  WorksheetInstanceId, FieldKey, Status: Open -> (Phase 1 Investigation) ->
      Retest | Escalated -> QA Disposition: ConfirmedOOS | Invalidated | RetestAccepted -> Closed
  Blocks TestRequest reaching Released and blocks COA issuance while Open
  On Open: linked MaterialBatch/ProductBatch (if any) -> Status = Quarantine
  On Close: batch -> Available (Invalidated | RetestAccepted) | Rejected (ConfirmedOOS)

COA
  TestRequestId it's built from (exactly one -- a Specification's Chemical+Microbial
      WorksheetLinks already resolve into WorksheetInstances under the same
      TestRequest per Subject, per build-briefs/03-test-requests-and-instances.md,
      so a combined certificate never spans two TestRequests; corrected from an
      earlier "1, or 2" framing that predated that design)
  Rows[] resolved from Specification.Characteristics (IncludeOnCoa = true only) x
      WorksheetInstance.FieldValues, per Subject, grouped/ordered by
      GroupName/DisplayOrder
  Status: Draft -> Issued -> (Revised -> new version, old superseded, never overwritten)
```

## Water-specific extension

Water is consumed continuously by production between tests, so it carries a
validity-window concept the other categories don't need. This carries forward,
against the new entity model, the mechanics already validated in
`quality-ard-routine-microbiology-2026-09-15.md`:

```
WaterQualityPeriod
  SamplingPointId (FK), TestRequestSubjectId (the Subject whose Reviewed results
      back this period)
  ValidFrom (= TestRequestSubject.CollectedAt, always), ValidUntil (dynamic -- set
      only on activation, tied to that point's MonitoringProgram.NextDueDate)
  RetrospectiveReason (always required -- ValidFrom is always backdated relative to
      activation time given incubation lag, so there is always something to justify)
  Status: PendingActivation -> Active | Held | Expired  (created at
      PendingActivation when a Water Coa is Issued; explicit /activate action
      required before production can rely on it -- see
      build-briefs/06-monitoring-programs-and-water-quality.md)

WaterUseRecord
  WaterQualityPeriodId, UsedAt, BatchManufacturingRecordId/ProductionActivityStepId
  Status: Recorded | Held (auto-flagged for Quality Impact Assessment when its period is Held)
  // Manual entry only in this design -- automatic capture from production
  // consumption stays deferred (see deferred-and-next-steps.md)
```

## Assignment and reassignment

Every `WorksheetInstance` is assigned to exactly one user, who alone may start it,
enter or edit its `FieldValues`, and submit it — enforced at the permission layer,
not just hidden in the UI (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md#assignment-enforcement)).
This isn't a new mechanism: `FormAssignee`/`FormFieldAssignee` already exist in the
current backend and enforce assignment down to individual field level (a
`FormFieldAssignee.AssigneeId` per field within a `FormAssignee` scoped to a
batch/sample/routine-track context). `WorksheetInstance.AssignedToId` is that same
concept surfaced explicitly at the granularity the new Test Room actually needs — one
assignee per test. The finer-grained per-field pattern stays available underneath for
worksheets that genuinely split work across people (e.g. one tech weighs, another
runs the instrument) without becoming the default.

Reassignment changes `AssignedToId` mid-flight and is a distinct, audited action —
who reassigned, from whom, to whom, when — since it's changing who is accountable for
GxP-relevant data already possibly in progress. It doesn't reset `Status`; work
already entered stays exactly as entered, `EnteredBy` still correctly attributed to
whoever actually typed it, not retroactively changed to the new assignee.

## StandardTestProcedure unifies existing per-Material/per-Product STP entities

The existing backend already has `MaterialStandardTestProcedure` and
`ProductStandardTestProcedure` — the same Material/Product duplication pattern found
everywhere else in the current QC entities (see the "why this exists" context in
[README.md](./README.md)). `StandardTestProcedure` unifies them into one entity,
consistent with how `WorksheetTemplate` and `Specification` are unified rather than
kept as parallel Material/Product copies. An STP is the narrative, formally
version-controlled method document (Purpose/Scope/Responsibility/Accountability/
Procedure) — confirmed against a real STP doc for a raw material (`QCD/STP/RM/004`,
Revision 05, Supersedes 04, Reference BP 2025) whose Procedure steps themselves cite
other SOPs by code (e.g. "Refer to QCD/SOP/053"). It's a different document from the
`WorksheetTemplate` that implements it: the STP is prose describing how to perform
the test; the `WorksheetTemplate` is the structured form an analyst fills in while
following it.

## OOSCase supersedes the live OosInvestigation entity

The existing `OosInvestigation` (migration `20260627101100_AddOosAndSpecificationReference`,
applied — this one is live, not shelved) is per-`AnalyticalTestRequest`/per-`MaterialBatch`
with a binary disposition (`QaApproved` | `PermanentlyRejected`), and retests by
reopening the same ATR to `Testing` rather than creating a linked record. `OOSCase`
supersedes it with per-`FieldKey` granularity and a three-way disposition, because one
`Specification` can carry 9+ `Characteristics` and a single failing one shouldn't read
as "the whole ATR is OOS." Its two concrete mechanics — quarantining the batch on
open, releasing or rejecting it on close — are real gaps the original design missed
and are carried forward explicitly above. Its readiness check before approval
(required analysis must be complete) also carries forward: an `OOSCase` cannot close
`RetestAccepted`/`Invalidated` while other required `WorksheetInstance`s for the same
`TestRequest` are still incomplete.

A retest is a **new, separately linked `WorksheetInstance`** (`RetestOfInstanceId`),
not the same record reopened — a deliberate behavior change from the live pattern,
in favor of never touching an already-submitted result. This is a real workflow
change for QA staff used to the reopen-in-place pattern and should be called out
explicitly in training/rollout, not treated as a silent implementation detail.

## Why field references are the spine

Every place this design needs traceability -- specification -> worksheet, worksheet ->
worksheet, COA -> result -> analyst entry -- resolves through `FieldKey`, never
position. Nothing in the current codebase does cross-worksheet resolution today
(confirmed by search — the only "worksheet" cross-referencing that exists,
`FormulaWorksheetPreprocessor`, is an unrelated spreadsheet-formula engine in the
manufacturing Formulas module), so this is the piece designed most carefully before
anything else is built on top of it. See [field-catalog.md](./field-catalog.md) for
the `ReferencedResult` field type this depends on.
