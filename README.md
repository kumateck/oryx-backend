# Oryx ERP

QC worksheet AI import fix (2026-09-29): OpenAI extraction now sends a schema
valid for strict structured output and reports provider, rate-limit, network,
and unreadable-key failures separately from an unconfigured key. The importer
still returns reviewable proposals without saving or approving them. See
`docs/qc-rebuild/ai-import-provider-diagnostics.md`.

Documentation updated for this task: `README.md`, `docs/services.md`,
`docs/workflows.md`, and `docs/qc-rebuild/ai-import-provider-diagnostics.md`.

Full Procedures template scope update (2026-09-28): all five template creation
services now enforce the catalog's allowed kind and purpose/subject pairing.
See `docs/full-procedures-template-scope-2026-09-28.md`.

## Recent Updates

- Chemical/Microbial and Routine QC (2026-09-15): typed commercial ARDs,
  microbial applicability, separate worksheets, aggregate release readiness, immutable
  combined/single certificates, scheduled/event-triggered Water and Environmental
  routines, water quality coverage/use records, and linked R&D completion gates are
  implemented. Four migrations are generated and not applied. See
  `docs/quality-ard-routine-microbiology-2026-09-15.md`.

- Full Procedures Phase-0 backend proof (2026-09-15): an isolated .NET graph/run
  transition model and six focused tests cover version/content pinning, QC waits,
  parallel joins, IPC due time, abort, optimistic versioning, retry and serialized
  restart. A separate disposable PostgreSQL spike proves transactional state,
  audit and outbox rollback, fresh-connection recovery, competing writers and
  local effect-receipt deduplication. A test-only leased outbox worker retries
  after a forced post-effect failure using a fake idempotent adapter. A separate
  test worker is killed at three points around the fake effect/receipt, and a
  new process resumes after lease expiry; all 15 focused tests pass with a
  disposable PostgreSQL URL. Neither spike is registered in an API or live
  batch path. Database restart, durable timer and real-effect recovery,
  real outbox delivery, QC authority and material effects remain unproved; see
  `docs/full-procedures-phase-0-backend.md`.
  A separate action-contract issue snapshot now pins exact action/interpreter
  versions and blocks unknown versions. One shared no-database focused run
  passed 13 tests and skipped eight PostgreSQL tests; a later combined database
  run could not build during unrelated concurrent Routine QC edits. Re-run it
  when that build is stable. No new Procedure API or live batch path was added.
  The reviewed Full Procedures capability keys are now emitted by the backend
  permission catalog under dedicated submodules. This is fail-closed: no role is
  granted a key automatically, and no Procedure endpoint or resource authority
  is created by catalog registration.
  Area management and template review have distinct keys. The Template Area
  registry now validates and persists dynamic area descriptors, owner roles,
  role grants, expected versions and hashed audit snapshots behind scoped API
  guards. Migration `20260916064646_AddFullProcedureTemplateAreas` is additive;
  it was SQL-reviewed but not applied automatically. The first immutable
  Template Question revision backend is now also implemented: Draft → In Review
  → reviewed → Published governance, expected-hash concurrency, exact published
  calculation-source revision pins, append-only audit snapshots and automatic
  retirement of the prior Published revision, plus controlled return-for-change
  and explicit retirement commands. Migration
  `20260916072607_AddFullProcedureQuestionRevisions` contains only its five
  additive tables and both lifecycle uniqueness indexes. It was SQL-reviewed
  and applied to the configured local development database; it was not deployed
  to production. Legacy Questions, Routes and
  Procedure execution remain unchanged. See
  `docs/full-procedures-question-revisions.md`.
  Immutable Template Section and Form revisions now extend that hierarchy.
  Sections pin exact Published Question revisions; Forms pin exact Published
  Section revisions and validate earlier-section conditions against exact source
  Question revisions. Migrations
  `20260916074547_AddFullProcedureSectionRevisions` and
  `20260916123348_AddFullProcedureFormRevisions` were SQL-reviewed and applied
  only to the configured local development database. The focused suites pass
  9/9 and 6/6; the complete Full Procedures run reports 54 passed, 10
  intentionally gated skips and zero failures. This governs definitions, not
  response access or Procedure execution. See
  `docs/full-procedures-section-revisions.md` and
  `docs/full-procedures-form-revisions.md`.
  Immutable Activity revisions now add exact Published Form pins, typed action
  occurrences, independent role assignments, controlled resource capabilities,
  typed inputs/outputs and explicit completion rules. The focused Activity suite
  passes 6/6. Its migration remains isolated from the separately generated
  `20260916132115_AddAnalysisTypeToFormSection` migration, and EF reports no
  pending model changes. The local database was unavailable for an apply check.
  See `docs/full-procedures-activity-revisions.md`.
  Immutable Workflow revisions now compose exact Published Activity revisions
  into validated Start/End, Branch, Fork/Join, Wait/IPC, Hold/Resume and bounded
  Rework graphs. React Flow layout is stored separately from semantic content;
  governed labels and ordering are hash-protected while layout-only moves are
  not. Workflow tests pass 12/12 and the aggregate Full Procedures run is
  72 passed, 10 gated skips, zero failures. See
  `docs/full-procedures-workflow-revisions.md`.
  Governed cross-area definition sharing now pins one exact Published Question,
  Section, Form, Activity or Workflow revision. Two-area approval, actor
  segregation, optimistic versions, reasoned hashed audit snapshots, aggregate
  definition-only usage counts and revocation are API-wired. Migration
  `20260916174624_AddFullProcedureTemplateSharing` is model-clean and unapplied;
  six focused tests pass. Sharing exposes no responses or evidence. Target-owned
  adoption is now API-wired for all five template kinds through migration
  `20260916234752_AddFullProcedureTemplateAdoption`; it creates a new Draft with
  immutable lineage and explicit dependency/role translations without copying
  source approvals or responses. Seven adoption tests pass and the aggregate run
  after the Procedure-definition slice is 91 passed, 10 gated skips, zero
  failures. The sharing and adoption migrations remain unapplied.
  See `docs/full-procedures-template-sharing.md` and
  `docs/full-procedures-template-adoption.md`.
  Immutable Procedure definitions now pin one exact Published Workflow and hash,
  declare product/site applicability for Trial, Validation, Commercial or Scale
  Up batches, and map every Activity node to Manufacturing, Packaging, Shared or
  Development record scope. Hash-fenced review/approval, dependency revalidation,
  actor segregation and transactional supersession are API-wired through migration
  `20260916234859_AddFullProcedureDefinitions`; six focused tests pass. This is a
  governed definition only: it creates no BMR/BPR master, release bundle, batch
  issue or runtime execution. See `docs/full-procedure-definitions.md`.

- Collaborative template drafting (2026-09-08): forms may persist tests before questions are
  available. Product and material ARD creation remains fail-closed and returns `Form.Question` if
  any selected-template test has no question. See `docs/services.md` and `docs/workflows.md`.
- Formula response compatibility (2026-09-08): creating a response now requires an approved form
  revision only when the form contains a formula linked to the governed definition model. Preserved
  legacy formula JSON no longer blocks Short Answer or other ordinary fields in mixed legacy forms;
  governed formula placements remain fail-closed until their form revision is approved.
- Governed STP documents (2026-09-07): material/product STPs now support versioned
  ONLYOFFICE `.docx` content with Draft → In Review → Reviewed → Approved controls,
  distinct author/reviewer/approver identities, password-confirmed signatures, authenticated
  and permission-scoped downloads/editing, document-bound editor tokens, trusted callback origins,
  retry-safe save callbacks, validated Word packages, database evidence constraints, and an
  end-to-end deployment check that verifies shared JWT configuration, object availability, plus
  API reachability from the document-server network. Retrying the same file against the current
  Draft is idempotent, preventing duplicate immutable versions after partial multi-owner uploads.
  See `docs/services.md` and `docs/workflows.md`.
- Production formula source integration (2026-09-07): form-scoped variables now resolve from
  same-response scalar fields, table-column statistics, or authoritative upstream formula results.
  Upstream formulas are evaluated in deterministic topological order; unknown/self/cyclic sources,
  incompatible or stale result evidence, reference/dependency disagreement, and input-hash drift
  fail closed. Table statistics use deterministic decimal arithmetic and reject invalid cells
  without silently changing the sample. The deployment overlay
  now starts the released calculation image, waits for its health check, shares a Docker secret,
  and requires the exact engine digest. AutoMapper remains on the open-source 14.0.0 release;
  production does not require an AutoMapper license key. All 348 backend tests pass, including
  94 formula-focused tests.
  Replacement formula/template approvals now run serializably, append supersession evidence, and
  are protected by filtered unique indexes so only one revision can be effective at a time.
- Formula migration rehearsal (2026-09-07): a disposable clone
  `oryx_formula_rehearsal_20260907` was rolled back across the formula migration chain, migrated
  forward, and migrated forward again. All 93 formula questions and 118 formula options retained
  the same `ac647c9ae8d8c6eba2583010cd914116` evidence digest; the second forward run was a no-op.
- Governed formula runtime hardening (2026-09-06): response reads now project stored
  authoritative formula display results, final submission runs inside a serializable transaction,
  and versioned drafts become immutable after their submission set exists. Print/COA clients can
  distinguish governed/finalized evidence without recalculation. `docker-compose.formula.yml`
  securely connects the API to the internal calculation service and remains disabled by default.
- The additive schema was applied to the reachable local PostgreSQL `entrancedb` only after a
  custom-format backup. Post-migration verification retained 97 active legacy formula questions
  and 156 option rows; no formula data migration or runtime enablement was performed.

- Formula v1 governed integration: additive revision, placement, immutable response-snapshot,
  append-only execution/submission, legacy-evidence, and migration-reconciliation tables are
  defined with EF and PostgreSQL integrity guards. Controller-free services now compute a source
  fingerprint, validate an approval manifest, assign deterministic migration IDs, and optionally
  record an authenticated immutable dry-run ledger without changing legacy rows. A controlled,
  controller-free Apply service can import only hash-verified, externally approved canonical definitions and audit
  evidence after rechecking source drift, reconciliation, active identities, and three-person
  separation of duties. A controller-free operator CLI now seals and validates review packages,
  exports read-only dry runs, and gates evidence recording/Apply behind a validated application
  token, dedicated permission, database-name check, and exact confirmation token. No legacy formula is rewritten, no placement is migrated, and no formula
  API/runtime cutover is enabled; see
  `docs/formula-v1-persistence-foundation.md`. Authenticated definition, template-revision,
  response-snapshot/evaluation, and finalization services are implemented with runtime flags off
  by default. The migration chain passed apply, rollback, reapply, trigger inspection, and model-
  drift checks on a disposable local `entrancedb` clone.
- Shift Scheduling gap fixes and Ghana Labour Act compliance: `ShiftType.StartTime`/`EndTime` moved from `string` to a native `TimeOnly` column (migration rewritten with explicit raw-SQL `to_timestamp(...)::time` conversion since the default EF scaffold cannot implicitly cast `"06:00 AM"`-style text to `time`), backing one shared, overnight-aware overlap helper (`APP/Utils/ShiftTimeHelper`) that replaced three separately-implemented, inconsistent comparisons in `AssignEmployeesToShift`, `SwapShift`, and the Excel import (one of which could throw on `TimeSpan.Parse` of an AM/PM string). Added the missing `ShiftCategoryController` (standard CRUD), explicit per-action `[Authorize]` on `ShiftScheduleController`/`ShiftTypeController`, and wired the previously-unused `NotificationType.ShiftAssigned` to fire on assign/swap/import. A duplicated permission-key set (singular vs. plural shift-schedule keys, which had let the frontend's nav guard and page guard disagree) was consolidated to one canonical set. New: an effective-dated `WorkingHoursPolicy` entity, seeded with the Labour Act, 2003 (Act 651) baseline (8h/day, 40h/week, 12h daily rest, 48h weekly rest), is checked at assign/swap/import time via `/api/v1/working-hours-policy`.
- Documentation updated for this task: `README.md`, `docs/services.md`, `docs/workflows.md`; the frontend's `README.md`, `docs/services.md`, `docs/workflows.md`, and in-app user manual were also updated.
- Personal IT issue reporting: every authenticated staff user can create an IT
  ticket and access tickets they reported through the dedicated My IT Issues
  page. Ticket detail and activity access is restricted to the reporter,
  assignee, or a user with `CanViewAllTickets`.
- HR Payroll & Performance Management: new `/api/v1/payroll/*` and `/api/v1/performance/*` endpoints. Payroll covers effective-dated employee compensation (basic salary, allowances, bank/TIN), pay grades, effective-dated PAYE tax bands and SSNIT rates (pre-seeded for 2026 Ghana: graduated GRA bands, 5.5%/13% SSNIT with Tier 2 carved from the employer share), one-off/recurring deductions and additions (bonus/commission/overtime), and annual GRA tax reliefs, all consumed by `PayrollCalculationService` to generate one Payslip per employee on `POST /api/v1/payroll/runs/{id}/submit`. Performance covers Cycles, weighted Goals, and Reviews (self-assessment → manager review). Both `PayrollRun` and `PerformanceReview` implement the existing generic `IRequireApproval` and are approved/rejected through the existing `/api/v1/approval/approve|reject/{modelType}/{modelId}` endpoint — no new approval infrastructure was added, only new `case` branches in `ApprovalRepository` for the two model types. New permission keys were added under the existing `Human Resources` module in `PermissionUtils.cs`. See `docs/hr-payroll-production-migration.md` for the table-naming collision this surfaced against the legacy `PayrollRuns` schema, and its fix.
- HR payroll migration safety: the newer HR payroll workflow now persists to
  `HrPayrollRuns`, preserving the unrelated legacy `PayrollRuns` table and its
  payroll-company, period, and pay-group relationships. The production runbook
  includes explicit collision preflight and post-migration verification.
- Billing-sheet charge payments: the Pay Charges action now records auditable `Payment` rows instead of directly flipping a boolean. Charge batches are validated and committed atomically, require `CanRecordPayment`, preserve configured maker-checker approval, and mark a charge paid only after its payment is approved. Migration `20260903121205_LinkBillingSheetChargePayments` adds the nullable payment-to-charge traceability link.
- Customer relationship management: added preferred currency and terms, advisory credit control backed by approved invoice payments, multiple contacts, standing prices, central My Approvals-backed customer quotations, agreed-price and packing previews, and safe approved-quotation-to-ProductionOrder conversion with preserved shipper/loose quantities. The additive CRM migration has a controlled production runbook and remains unapplied.
- Supplier relationship management: added pharmaceutical compliance certificates, multiple contacts and banking details, standing pricing agreements, configurable AVL requalification, computed supplier scorecards, and approved-payment spend analytics. The additive SRM migration has a controlled production runbook and is not applied automatically.
- Cashflow foundation: added maker-checker payments, approved-payment balances, AP/AR aging, cashflow projections, effective-dated exchange rates, and nullable due-date snapshots. The production migration is additive and automatic startup migration is disabled; use the documented controlled rollout.
- Product ATR response integrity: stage responses are scoped by BMR and production step, COA submission is single-round, and the uniqueness migration must be applied independently to every application database.
- Material stock pipeline: `GET /api/v1/material/{materialId}/stock/pipeline` exposes active inbound material quantities from purchase requisition through sourcing, quotation, purchase order, shipment milestones, receiving, QC, and GRN. The read model reconciles each quantity into one stage, uses actual shipment received quantity, groups duplicate lines before subtraction, and excludes final shelf-distributed stock. Focused builder and EF-backed query tests cover partial deliveries, duplicate lines, and completed distribution.
- Warehouse shelf-list performance: `GET /api/v1/warehouse/shelf` is now a no-tracking, auto-include-free list query that loads only shelf/rack/location/warehouse metadata. List rows intentionally omit material batches; `GET /api/v1/warehouse/shelf/{shelfId}` remains the full detail contract. Search now covers shelf code/name/description, rack, location, and warehouse. Repository coverage verifies the lean payload and code search.
- Analytics/report integrity: implemented the staff gender-ratio report, corrected checklist user identity and logistics OpenAPI metadata, and removed anonymous access from supplier/vendor summary reports.
- Warehouse expiry risk now excludes the intentionally unlimited water sentinel and labels shelf quantities with shelf UOM when available.
- Warehouse KPI reports now apply warehouse type/division filters inside authenticated department scope. `GET /api/v1/report/warehouse-kpi/freshness` exposes persisted source watermarks and row counts without mutating operational data.
- Partial shelf movements preserve total stock, retain source UOM, and reject empty, non-positive, or cross-UOM requests. Regression coverage is included in `tests/APP.Tests`.
- Supplier quotation request lookup now remains readable after it is sent. For a supplier with a new request, the lookup selects that unsent request first; otherwise it returns the latest sent request. Only a supplier with no quotation request at all receives `404 Supplier.QuotationRequest.NotFound`, never `Error.NullValue`. Added repository coverage for completed and next-request selection.
- Supplier quotation detail and receipt endpoints now return `404 Supplier.Quotation.NotFound` for a genuinely missing quotation instead of producing `Error.NullValue` or a null-reference response. The controller contract and repository regression coverage include this behavior.
- Migration pipeline reliability: fixed a migration left with an empty `Up()`/`Down()` body that the EF model snapshot was masking as "no pending changes," and moved migration application out of `ApplyMigrationsOnStartup` (which stays disabled) into a dedicated, idempotent CI pipeline step run before each deploy.
- Alternative-batches performance and correctness: `GET /api/v1/requisition/{id}/alternative-batches` no longer re-offers a batch already committed to a pending warehouse swap, and its response time is fixed at the root cause — a circular `MaterialBatch`/`ShelfMaterialBatches` auto-include plus an unbatched AutoMapper resolver N+1 — down from minutes to seconds.
- Production activity board live sync: a new `ProductionActivityStepStatusChangedEvent`, published over the existing RabbitMQ bus from every one of the nine places a production step can complete (board action, BMR/BPR issue, final packing, FGTN distribution, stock requisition issue/approval, ATR release, QA/QC sign-off), drives a scoped live refresh on the frontend board without creating a persistent notification record.
- Approval auto-approval policy, full coverage: `StaffRequisition` and `ProductionOrder` previously had no approval-creation wiring at all, and recording a payment or sending a customer quotation used to hard-fail with a validation error whenever no approval workflow was configured. All four now follow the same missing-workflow auto-approve policy already used elsewhere, with a system `ApprovalActionLog` audit entry.
- Approval auto-approval policy, verified for every document type: added regression tests proving both the missing-config auto-approve path and the configured, staged manual-approval path work correctly for all 17 approval document types (`LeaveRequest`, `OvertimeRequest`, `ProductionOrder`, `BillingSheet`, `PurchaseOrder`, `Response`, `ProformaInvoice`, `ShipmentDocument`, `JobRequest`, `ProductionExtraPacking`, `FinishedGoodsTransferNote`, and `StockAdjustment` newly covered). No production code changes were required — every previously-unproven path already worked.

## Introduction

Brief introduction about your project.

## Features

List of features.

## Installation

Instructions for installing and running the project.

## Usage

Instructions for using the project.

## Docker Setup

For instructions on how to set up and run the application using Docker, please refer to the [Docker Setup Instructions](DOCKER.md).

The production Docker restore layer copies every project manifest referenced by
`Oryx.sln`, including `TOOLS/FormulaMigration`, before running `dotnet restore`.

## Documentation

Design notes, roadmaps, and implementation write-ups that don't belong in
the changelog above live in [`docs/`](docs/):

- [Cashflow, SRM & CRM roadmap](docs/cashflow-srm-crm-roadmap.md) — gap
  analysis and the Codex backend prompts for the three modules, in
  implementation order (Cashflow → SRM → CRM).
- [Service contracts](docs/services.md) — cashflow endpoints and permissions.
- [Full Procedures Template Areas](docs/full-procedures-template-areas.md) —
  dynamic area configuration, role scope, audit/version rules and migration boundary.
- [Full Procedures Section revisions](docs/full-procedures-section-revisions.md) —
  immutable exact Question-revision composition and governance.
- [Full Procedures Form revisions](docs/full-procedures-form-revisions.md) —
  immutable exact Section-revision composition and conditional-display governance.
- [Full Procedures Activity revisions](docs/full-procedures-activity-revisions.md) —
  exact Form pins, typed actions, roles, resources, data and completion governance.
- [Workflow behavior](docs/workflows.md) — payment approval, balance, due-date,
  and exchange-rate rules.
- [Cashflow production migration](docs/cashflow-production-migration.md) —
  zero-data-loss rollout, validation, and rollback guidance.
- [SRM production migration](docs/srm-production-migration.md) — additive supplier
  compliance and analytics rollout guidance.
- [CRM production migration](docs/crm-production-migration.md) — additive customer,
  credit, quotation, and production-order rollout guidance.
- [Product ATR response migration](docs/product-atr-response-migration.md) —
  per-database preflight, migration, verification, and release order.
- [HR payroll and performance migration](docs/hr-payroll-production-migration.md)
  — preserves the legacy payroll schema while deploying the new HR workflow.
- [Formula v1 persistence foundation](docs/formula-v1-persistence-foundation.md)
  — additive schema, integrity rules, verification evidence, rollout gates, and rollback boundary.
- [Formula migration operator tool](docs/formula-migration-operator-tool.md)
  — package sealing, offline verification, authenticated execution, and no-go controls.
- [Materials ready for checklist report](docs/materials-ready-for-checklist-report.md)
  — QC/production department scope, lean API contract, and rollout verification.

Documentation updated for the checklist-report endpoint: `README.md`,
`docs/services.md`, `docs/workflows.md`, and
`docs/materials-ready-for-checklist-report.md`.

Documentation updated for the formula persistence foundation, controlled definition importer,
and operator workflow: `README.md`, `docs/services.md`, `docs/workflows.md`,
`docs/formula-v1-persistence-foundation.md`, and `docs/formula-migration-operator-tool.md`.

## Global approval progression enforcement (2026-09-08)

All configurable approval documents now share a fail-closed progression contract. Procurement, requisition, inventory, production, logistics, maintenance, and R&D operations recheck persisted approval before downstream mutation; configured documents remain locked until the assigned user or role completes every required stage. Missing or zero-stage configurations use the existing audited automatic-approval policy, now including payroll runs. The legacy requisition approval route cannot set approval state outside the central approval service. Regression tests cover the shared contract, automatic approval, pending rejection, and bypass attempts.

Documentation updated for this task: `README.md`, `docs/services.md`, and `docs/workflows.md`.

## Governed formula authoring completion (2026-09-10)

Formula authoring isolates mutable Draft payloads from legacy `QuestionOption` data, rejects
generic API bypasses, persists payload hashes through an additive migration, and keeps server
validation plus maker-checker approval as the only path to an effective revision. The demo deploy
enables the API client only after the isolated formula worker is healthy. Documentation updated:
`README.md`, `docs/services.md`, and `docs/workflows.md`.

## Demo deployment coordination (2026-09-10)

The demo API deployment waits up to two minutes for the independently deployed governed
calculation service, verifies its health and token mount, and reuses that service's Docker secret
volume. This removes the frontend/backend workflow race that could otherwise fail deployment
before the API container starts.

The API also enforces a global AutoMapper runtime object-graph depth of 32 as the documented
recursion/availability mitigation for the advisory reported against the retained 14.x package.

## Contributing

## Job request assignment delivery and proforma prerequisite (2026-09-16)

Internal job assignment now requires the selected employee to have an active ERP user account and sends that user an auditable in-app assignment notification after the job execution is created. The frontend explicitly persists an already chosen service quotation before requesting its proforma invoice, repairing legacy job orders whose quotation flag and workflow status were out of sync.

Documentation updated for this task: `README.md`, `docs/services.md`, and `docs/workflows.md`.

Guidelines for contributing to the project.

## License

Details about the project's license.
# SMTP delivery configuration

Email delivery now requires `SMTP_HOST`, `SMTP_USERNAME`, and `SMTP_PASSWORD` in the deployed environment. Set optional `SMTP_PORT` (default 587) and `SMTP_FROM` (default SMTP_USERNAME) for the authorized sender. A 535 authentication failure must be resolved by verifying the deployed account and secret with the mail provider; do not log the password or mark a vendor request as sent after failure.

The unified My Pending Approvals interface uses the self-scoped generic and QC queues. Generic detail and action endpoints require an active assigned stage; staff requisitions and production orders now support audited decisions. See [approval workflow documentation](docs/workflows.md).

Full Procedures Question authoring accepts exact `calculationReferences`
(`questionId`, `revisionId`) and rejects a stale or mismatched Published source.
Legacy `calculationQuestionIds` callers remain supported. Documentation updated:
this README, `docs/services.md`, and `docs/workflows.md`.
