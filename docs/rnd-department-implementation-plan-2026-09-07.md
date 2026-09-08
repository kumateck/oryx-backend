# Research & Development (R&D) Department — Findings & Implementation Plan

## Context

R&D does not exist in Oryx today — not as a seeded `Department`, not as a
module, not as a single entity. This doc grounds a from-scratch R&D module in
(a) actual pharma industry standards for how an R&D function operates and
what it must produce, and (b) a precise map of what Oryx already has that R&D
should reuse versus what's a genuine gap. It follows the same shape as
`cashflow-srm-crm-roadmap.md`: findings first, proposed entities/phasing
second, open decisions called out explicitly rather than guessed at.

This is a planning document, not a set of instructions to execute yet — the
"Open questions" section at the end needs answers before Phase 1 gets turned
into an implementation prompt.

## Part 0 — Industry standards this module should be built against

- **WHO TRS 1044, Annex 6 (2022) — Good practices for research and
  development facilities of pharmaceutical products.** The most directly
  relevant standard: it's GxP written specifically for a pharma R&D function.
  Covers knowledge management and risk assessment (quality attributes,
  critical quality attributes, critical process parameters must be defined
  and documented), self-inspection with tracked CAPA, and the expectation
  that programs/procedures/protocols/specifications/process design
  *developed* in R&D get formally *transferred* to commercial manufacturing
  and QC sites. This should be the backbone the module's workflow gates are
  built around.
- **ICH Q8(R2) — Pharmaceutical Development (Quality by Design).** Defines
  the QbD sequence: Quality Target Product Profile (QTPP) → Critical Quality
  Attributes (CQA) → design space / critical process parameters. This is the
  intellectual structure a formulation-development record should follow, and
  it's what CTD Module 3.2.P.2 expects to see.
- **ICH Q9(R1) / Q10 / Q11** — quality risk management, pharmaceutical
  quality system, and drug-substance (API) development respectively; the
  framework QbD sits inside. Not modules of their own, but their risk/CAPA
  vocabulary should match what the existing `OosInvestigation`
  (RootCauseAnalysis/CorrectiveActions/PreventiveActions) already uses, for
  consistency.
- **ICH Q1A(R2) / Q1E — stability testing.** Long-term / intermediate /
  accelerated storage conditions by climatic zone, testing frequency, and the
  statistical basis for extrapolating shelf-life from partial real-time data.
  Note: a consolidated ICH Q1 (merging Q1A–E and Q5C) is in draft as of mid-
  2025 with final adoption expected in 2026 — the data model below is
  designed to hold condition/zone as configurable values, not hardcoded
  enums, so it survives that revision.
- **WHO TRS 1044, Annex 4 (2022) — technology transfer in pharmaceutical
  manufacturing** (supersedes the older TRS 961 Annex 7). Structures a
  transfer as: due diligence/gap analysis → organization & management →
  documentation → equipment/instrument qualification → life-cycle approach.
  This maps directly onto the "R&D → Production" handoff Oryx currently has
  no gate for at all (see Part 1).
- **ALCOA+ / 21 CFR Part 11** — data integrity expectations (Attributable,
  Legible, Contemporaneous, Original, Accurate + Complete, Consistent,
  Enduring, Available) for any electronic record, including lab data. Oryx
  already has two conventions that satisfy most of this — `IVerifiable`
  (maker-checker on specs/ARD) and `ActivityLogMiddleware` (audit trail on
  every mutating call) — so R&D mostly needs to *use* those consistently
  rather than invent a new integrity model.
- **CTD Module 3.2.P.2 (Pharmaceutical Development)** — the actual
  regulatory-facing deliverable R&D produces. A Product/Pharmaceutical
  Development Report aggregating formulation rationale, process development,
  and stability data. This should be the natural "final output" the module
  generates, the same way `FormRepository.GenerateCertificateOfAnalysis`
  already generates a QC deliverable from structured data.

## Part 1 — Current state in Oryx (verified against source, not assumed)

### What R&D should reuse as-is

- **Dynamic `Form`/`FormSection`/`Response` module.** `MaterialSpecification`
  and `ProductSpecification` don't hold their own field lists — they
  reference a `Form` whose sections carry the actual criteria. `Material/
  ProductAnalyticalRawData` do the same for raw test data. This is the right
  reuse target for DOE protocols, pre-formulation compatibility matrices, and
  stability test result capture — **not** `Checklist`/`PreSampleChecklist`,
  which are hard-coded, GRN-receipt-specific entities with fixed fields tied
  to `Grn`/`Supplier`/`Manufacturer`.
- **`IVerifiable`** (`DOMAIN/Entities/Base/IVerifiable.cs`: `IsVerified`,
  `VerifiedAt`, `VerifiedById`) — the single-step maker-checker pattern
  already used on `MaterialSpecification`, `ProductSpecification`, and both
  ARD entities. Good fit for lab-data sign-off.
- **`IRequireApproval` / `ResponsibleApprovalStage`**
  (`DOMAIN/Entities/Approvals/Approval.cs`) — the multi-stage maker-checker
  pattern used by `ProductionOrder` and others (`Approved` bool on the
  entity, an ordered `ApprovalStage` template, per-instance
  `ResponsibleApprovalStage` records with `Pending/Approved/Rejected`). This
  is the right fit for stage gates that need more than one sign-off:
  formulation-trial release, technology-transfer approval, development-report
  sign-off.
- **`BillOfMaterial` / `BillOfMaterialItem` / `ProductBillOfMaterial`.**
  `BillOfMaterial` is already versioned (`Version int`, `IsActive bool`) and
  attaches to `Product` via a join entity carrying its own
  `EffectiveDate`/`IsActive`. `Product` also already carries
  `MasterFormulaNumber`, `DocumentNumber`, `RevisionNumber` (currently
  free-text, populated by hand). **This is the exact landing spot for an
  R&D-approved formulation** — the tech-transfer step should end by writing
  a new `BillOfMaterial`+items here, not by inventing a parallel BOM concept.
- **The `Draft → InReview → Approved → Retired` revision lifecycle** used by
  `FormulaRevisionStatus`/`FormRevisionStatus` (in the calculation-engine
  module — see caveat below) is a clean status shape worth copying verbatim
  for formulation and analytical-method versioning, for naming consistency
  across the codebase.

### Caveat: "Formula" is already a taken name

`DOMAIN/Entities/Formulas/*` is **not** a pharmaceutical recipe. It's a
calculation-engine subsystem (`FormulaDefinition`/`FormulaRevision`/
`FormulaExecution`) that powers auto-calculated fields inside dynamic QC/QA
forms — no ingredients, no quantities, no link to `Product`/
`BillOfMaterial`. Any new R&D master-formula entity needs a different name
(proposed: `RndFormulation`) to avoid collision and confusion.

### What's genuinely missing (the real gaps)

1. **No lab-scale/trial/pilot batch concept anywhere.** Grepped for
   `TrialBatch`/`PilotBatch` — zero matches. The only "batch" entities are
   `MaterialBatch` (raw-material inventory lot, tied to a real GRN) and
   `BatchManufacturingRecord` (tied to a live `ProductionScheduleProduct`/
   `ProductionActivityStep`). Both assume commercial-production plumbing
   that a lab-scale run doesn't have. This is the central gap — R&D needs
   its own lightweight batch entity.
2. **No R&D project/study tracking at all** — no intake, no stage/status,
   no QTPP capture.
3. **No stability-study data model** — no chambers, no storage conditions,
   no pull-point scheduling, nothing resembling ICH climatic zones.
4. **No instrument/equipment calibration tracking, anywhere.**
   `Instrument` (`Code`, `Name` — that's the whole entity) and the separate,
   overlapping `QcEquipment` (`Name`, `SerialNumber`, `Make`, `Model`,
   category) both lack calibration due-dates or qualification status. WHO
   Annex 6 explicitly expects equipment qualification records — this is a
   cross-cutting gap R&D surfaces but doesn't own alone (QC hits it too).
5. **No technology-transfer record** — no gap-analysis protocol, no formal
   R&D→Production handoff gate. Today, promoting a formulation to
   `BillOfMaterial` would have to happen by hand with no audit trail tying
   it back to the development work that produced it.
6. **No development-report generation** — nothing analogous to CTD Module
   3.2.P.2, though the existing `GenerateCertificateOfAnalysis[ForProduct]`
   pattern shows how Oryx already does "assemble structured data into a
   formal document."
7. **STP entities are unversioned** — `MaterialStandardTestProcedure`/
   `ProductStandardTestProcedure` are just `StpNumber` + `Description`, no
   status, no revision history. Analytical *method development* (as opposed
   to routine testing) needs a draft/validate/transfer lifecycle these don't
   have.

### Conventions to follow (unchanged from prior modules)

`DOMAIN/Entities/<Module>/<Entity>.cs` (entity + DTOs) →
`APP/IRepository/I<Module>Repository.cs` →
`APP/Repository/<Module>Repository.cs` →
`API/Controllers/<Module>Controller.cs` → one EF Core migration per logical
change. Tests in `tests/APP.Tests/Repository/`, in-memory EF Core provider,
`MaterialPipelineQueryTests.cs`-style seeding helpers. Money fields (none
expected here) would follow the decimal+CurrencyId-FK convention regardless.

`Department` already supports what's needed structurally
(`ParentDepartmentId`, `DepartmentType.NonProduction`) — R&D just needs to
actually be seeded. The existing `DepartmentSeeder` is dead code (its whole
body is commented out, and only seeds four *production* departments), so
seeding R&D means either reviving that seeder properly or adding a fresh,
live one — not assuming R&D already exists as a row anywhere.

## Part 2 — Proposed data model & phasing

### Phase 1 — Foundations
- Seed **Research & Development** as a live, active `Department`
  (`DepartmentType.NonProduction`).
- `RndProject` — the intake/NPD record: `Code`, `ProductId` (nullable — a
  brand-new product may not exist yet), `Title`, `Objective`,
  `RequestedById`, `Status` (`Intake/FeasibilityReview/InDevelopment/
  TechnologyTransfer/Completed/OnHold/Cancelled`), `TargetLaunchDate`,
  `QtppFormId` (QTPP captured via the existing dynamic `Form` module, per
  ICH Q8), implements `IRequireApproval` for the intake→development gate.
- Instrument/equipment calibration retrofit (benefits QC too, not R&D-only):
  add `CalibrationDueDate`, `LastCalibratedAt`,
  `CalibrationCertificateAttachmentId`, `QualificationStatus` to whichever
  of `Instrument`/`QcEquipment` ends up canonical — see open question below,
  don't force a merge in this phase.

### Phase 2 — Formulation development
- `RndFormulation` — the master-formula-in-development. `RndProjectId`,
  `Version (int)`, `Status` (`Draft/InReview/Approved/Superseded` — mirrors
  `FormulaRevisionStatus` naming for consistency), `Items:
  List<RndFormulationItem>` shaped deliberately like `BillOfMaterialItem`
  (`MaterialId`, `Percentage`/`Quantity`, `BaseUoMId`, `IsSubstitutable`) so
  promotion to a real `BillOfMaterial` in Phase 5 is closer to a field copy
  than a rewrite.
- `RndTrialBatch` — the missing lab-scale batch entity. `RndProjectId`,
  `RndFormulationId`, `BatchCode`, `ScaleType`
  (`LabScale/PilotScale/ExhibitScale`), `BatchSize`, `ManufacturingDate`,
  `PerformedById`, `ProtocolFormId` (DOE run parameters via `Form`),
  `Status` (`Planned/InProgress/Completed/Aborted`), `Observations`.
- Pre-formulation / drug-excipient compatibility studies — no new entity
  needed, just `Form`/`FormSection` instances scoped to an `RndProjectId`.

### Phase 3 — Analytical method development
- `RndAnalyticalMethod` — development-stage method record:
  `RndProjectId`, `MaterialId`/`ProductId`, `Status`
  (`Draft/UnderValidation/Validated/Transferred`), `ValidationProtocolFormId`
  (AMV parameters — linearity/accuracy/precision/specificity/robustness —
  captured via `Form`). On `Transferred`, a repository method creates the
  corresponding `MaterialStandardTestProcedure`/`ProductStandardTestProcedure`
  row — the same "promote on approval" shape as Phase 5's BOM promotion.

### Phase 4 — Stability studies (ICH Q1A(R2)/Q1E)
- `StabilityChamber` — `Code`, `Name`, `TargetTemperature`,
  `TargetHumidity`, `ConditionType` (kept as a lookup/string, not a hardcoded
  enum, so the 2026 ICH Q1 consolidation doesn't force a migration),
  `CalibrationDueDate` (ties into Phase 1's calibration fields).
- `StabilityStudy` — `RndProjectId`, a polymorphic batch reference
  (`RndTrialBatchId` for development-phase studies, or
  `BatchManufacturingRecordId` for post-launch commercial stability —
  two nullable FKs resolved at the repository layer, same shape as the
  `Payment.PayableType/PayableId` polymorphism proposed for Cashflow),
  `StudyType`, `StartDate`, `ProtocolFormId`.
- `StabilityPullPoint` — `StabilityStudyId`, `TimePointMonths`, `DueDate`,
  `PulledAt`, `ResponseId` (results captured via the existing `Form`/
  `Response` pair), `Status`
  (`Scheduled/Pulled/Overdue/Tested/Reported`).

### Phase 5 — Technology transfer (WHO TRS 1044 Annex 4)
- `RndTechnologyTransfer` — `RndProjectId`, `GapAnalysisFormId` (checklist
  structured around Annex 4's chapters: org & management, documentation,
  equipment/instrument qualification, life-cycle), `Status`
  (`DueDiligence/GapAnalysis/ProtocolApproved/ExecutionInProgress/
  Completed`), implements `IRequireApproval` as the formal release gate.
- On approval: a repository method `PromoteToProduction(rndProjectId)` that
  creates a new versioned `BillOfMaterial` + `BillOfMaterialItem`s (copied
  from the latest `Approved` `RndFormulation`), attaches it via
  `ProductBillOfMaterial`, and stamps `Product.MasterFormulaNumber`/
  `DocumentNumber`/`RevisionNumber`. This is the concrete handoff the
  codebase has no gate for today.

### Phase 6 — Development report (CTD Module 3.2.P.2)
- A generator (mirroring `FormRepository.GenerateCertificateOfAnalysis`'s
  pattern) that assembles `RndProject` + `RndFormulation` history +
  `StabilityStudy` summary + `RndTechnologyTransfer` into one exportable
  report, structured along CTD 3.2.P.2 section lines. This is the actual
  regulatory-facing artifact the whole module builds toward.

**Suggested sequencing**: Phase 1 unblocks everything. Phases 2 and 3
overlap naturally (formulation and method development happen in parallel in
real R&D work). Phase 4 depends on having approved trial batches to pull
stability samples from. Phase 5 depends on an approved formulation existing.
Phase 6 is a pure aggregation layer over 1–5.

## Open questions (need your call before Phase 1 becomes a build prompt)

1. **Naming**: confirm `RndFormulation`/`RndProject`/`RndTrialBatch` as the
   prefix, given `Formula` is already taken by the calculation engine.
2. **Instrument vs. `QcEquipment`**: two overlapping, both-uncalibrated
   entities exist today. Retrofit both separately with calibration fields
   (safer, mirrors how the existing roadmap deliberately left
   `Vendor`/`Supplier` unmerged), or take this as the moment to unify them?
   Recommend: retrofit both, defer unification.
3. **Should R&D lab data reuse `AnalyticalTestRequest`/ARD, or get its own
   entity?** Those are hard-wired to `BatchManufacturingRecord`/
   `ProductionActivityStep` (commercial workflow). Recommend a separate,
   lighter `RndAnalyticalMethod`/protocol-driven approach (Phase 3) rather
   than bending the production-scoped ATR flow to also cover lab-scale runs.
4. **Material consumption for trial batches**: draw from real
   `MaterialBatch` stock (traceable, but pulls in GRN/warehouse ceremony) or
   a lightweight `RndMaterialConsumption` note that references
   `MaterialBatch` for traceability without the full requisition flow?
   Recommend the latter.

## Next step

Once the above is confirmed, Phase 1 (Department seed + `RndProject` +
calibration fields) can be written as a self-contained backend
implementation prompt in the same format as the Cashflow/SRM/CRM prompts in
`cashflow-srm-crm-roadmap.md` — entity + migration + repository + controller
+ tests, nothing else touched.
