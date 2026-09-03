# Oryx ERP

## Recent Updates

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

Documentation updated for the billing-sheet payment task: `README.md`,
`docs/services.md`, `docs/workflows.md`, and
`docs/cashflow-production-migration.md`.

## Contributing

Guidelines for contributing to the project.

## License

Details about the project's license.
