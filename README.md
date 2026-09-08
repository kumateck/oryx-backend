# Oryx ERP

## Recent Updates

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
- Customer relationship management: added preferred currency and terms, advisory credit control backed by approved invoice payments, multiple contacts, standing prices, maker-checker quotations, safe quotation-to-ProductionOrder conversion, and customer order metrics. The additive CRM migration has a controlled production runbook and remains unapplied.
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

## Documentation

Design notes, roadmaps, and implementation write-ups that don't belong in
the changelog above live in [`docs/`](docs/):

- [Cashflow, SRM & CRM roadmap](docs/cashflow-srm-crm-roadmap.md) — gap
  analysis and the Codex backend prompts for the three modules, in
  implementation order (Cashflow → SRM → CRM).
- [Service contracts](docs/services.md) — cashflow endpoints and permissions.
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

Documentation updated for the formula persistence foundation, controlled definition importer,
and operator workflow: `README.md`, `docs/services.md`, `docs/workflows.md`,
`docs/formula-v1-persistence-foundation.md`, and `docs/formula-migration-operator-tool.md`.

## Contributing

Guidelines for contributing to the project.

## License

Details about the project's license.
