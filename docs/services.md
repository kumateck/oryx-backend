# Service contracts

## Controlled STP document boundary (2026-09-07)

- `/api/v1/stp-documents` accepts only existing `MaterialStandardTestProcedure` and
  `ProductStandardTestProcedure` owners and persists immutable, SHA-256-addressed versions.
- Draft submission, independent review, independent approval, rejection, and approved-document
  revision are explicit endpoints; approval cannot bypass a reviewer signature.
- User downloads require bearer authentication. ONLYOFFICE file/callback routes use independent,
  expiring tokens bound to the document/version, and callback downloads must share the configured
  `ONLYOFFICE_INTERNAL_URL` origin and remain within the 25 MB limit.
- Product and material document reads/writes are checked server-side against their existing STP
  view/edit permissions. A signed-in user without the relevant domain permission cannot acquire
  an editor lock, stream a version, or invoke a lifecycle transition directly through the API.
- Editor-session creation verifies the selected version exists in object storage before acquiring
  a Draft lock. Missing objects fail with `StpDocument.StoredFileUnavailable` and are recovered
  only through the governed replacement-version workflow.
- Submit, review, and approval endpoints recheck the same object before recording their transition
  or e-signature, preventing unavailable evidence from becoming reviewed or effective.
- Uploading content whose SHA-256 matches the current Draft is an idempotent success. The service
  returns the existing document without writing another version; Approved revisions are excluded
  because beginning a governed replacement remains a deliberate, reasoned action.
- A save callback is acknowledged only after the Word package is stored and its immutable version
  row is committed. Invalid actors, untrusted URLs, oversized payloads, and storage failures return
  a retry response; the service never reports a failed controlled-document save as successful.
- Runtime settings: `ONLYOFFICE_JWT_SECRET`, `ONLYOFFICE_PUBLIC_URL`,
  `ONLYOFFICE_INTERNAL_URL`, and `API_INTERNAL_BASE_URL`.
- The API and Document Server must receive the same effective JWT secret, and
  `API_INTERNAL_BASE_URL` must be reachable from the Document Server network. Deployment
  health checks must verify an actual signed editor session and file fetch, not only the
  Document Server `/healthcheck` endpoint.

## Formula dependency and deployment boundary (2026-09-07)

- `QuestionValue`, `QuestionTableColumnStat`, and `FormulaResult` bindings resolve only against the
  same response and approved form revision. Formula-result resolution requires the latest valid
  authoritative upstream execution and re-resolves its current inputs to reject stale evidence.
- Submission sorts snapshot execution by the declared formula-result dependency graph. Unknown
  nodes, self-dependencies, and cycles return a controlled configuration failure before business
  status mutation.
- A formula-result source is accepted only when its percent-decoded
  `placement/{placement}/result/{result}` reference equals its single declared dependency. The
  same parser drives topological ordering and value resolution, preventing split-brain source
  identity. Malformed encoding or disagreement fails closed.
- Cross-question table statistics use deterministic decimal arithmetic, including decimal
  square-root for standard deviation and midpoint-away-from-zero rounding. One malformed,
  non-finite, or over-scale cell invalidates the governed statistic rather than being discarded.
- The production API overlay declares the internal calculation service and waits for its health
  check. Deployment requires `FORMULA_SERVICE_IMAGE`, `FORMULA_SERVICE_ENGINE_BUILD_HASH`, and
  `FORMULA_SERVICE_AUTH_SECRET_FILE_HOST`; AutoMapper is pinned to the open-source 14.0.0 release
  and does not require `AUTOMAPPER_LICENSE_KEY`.
  Secrets are mounted as files and are never committed or returned by runtime endpoints.

## Formula v1 governed runtime boundary (2026-09-05)

The formula v1 boundary now includes authenticated application services and HTTP endpoints:

- `POST|PUT /api/v1/formula-questions` saves the legacy-compatible Question and its governed
  formula draft in one serializable transaction.
- `/api/v1/formula-definitions/**` exposes revision reads, validation, review, and approval.
- `/api/v1/form-revisions/**` captures immutable template field snapshots and formula placement
  configurations. Drafts may be incomplete and refreshed; submit-for-review requires one verified
  approved formula revision for every formula field and an acyclic dependency graph.
- `/api/v1/formula-runtime/responses/**` creates immutable response snapshots and requests
  authoritative evaluation from values already stored for that response. It never trusts a
  client-supplied result as authoritative.
- Existing final response submission now creates a hash-bound formula submission set before the
  business status transition. Response approval rounds bind to that set.
- `POST /api/v1/form/responses/draft` preserves mixed legacy-form compatibility: an approved form
  revision is required only when a formula question is linked through
  `QuestionFormulaDefinitions` or the form has governed placement history. A legacy JSON formula
  alone does not make unrelated scalar fields depend on a governed configuration; governed and
  retired placements remain fail-closed.
- Form-response reads attach `FormulaGoverned`, `FormulaResultFinalized`, `FormulaExecutionId`, and
  `FormulaDisplayResultsJson`. A finalized projection is selected from the latest immutable
  submission set; a pre-submission projection may use the latest valid authoritative execution.
  The backend never exposes a client-computed value as authoritative.

Formula and template review/approval retain separate permission keys and three-person segregation
of duties. The calculation service and runtime cutover remain disabled by default; enabling them
requires deployed migrations, service authentication, an approved numeric policy/corpus, and
approved formula/template revisions.

Replacement approvals run in serializable transactions, retire the prior effective revision, and
append a `Superseded` audit row. Migration
`20260907163852_EnforceSingleEffectiveFormulaAndFormRevision` adds filtered unique indexes for one
non-deleted Approved revision per formula definition and per form. A uniqueness or serialization
race returns a controlled conflict instead of creating ambiguous effective content.

The API deployment overlay is `docker-compose.formula.yml`. It mounts the same Docker secret used
by the calculation service, uses the service-only `sail` network address, and leaves
`FORMULA_RUNTIME_ENABLED=false` unless a controlled release explicitly enables it.

## Formula v1 persistence boundary (2026-09-05)

No new HTTP endpoint is enabled in this increment. Existing question, template, response,
ARD, print, COA, and approval contracts continue to use the legacy behavior.

The backend now has an additive persistence model for future formula-definition revisions,
versioned template placements, immutable response snapshots, append-only execution attempts,
authoritative submission sets, preserved legacy source artifacts/key mappings, and migration
reconciliation evidence. The three links added to existing response entities are nullable, so
existing clients and rows remain compatible.

Future formula endpoints must write through application services that enforce authorization,
separation of author/reviewer/approver duties, idempotency, canonical hashes, and the approved
numeric-policy version. Direct CRUD over formula revisions, snapshots, executions, submission
sets, legacy evidence, or reconciliation records is not an acceptable API contract.

The internal `IFormulaMigrationInventoryService` exposes `DryRunAsync`; it returns hashed,
deterministic inventory and readiness controls and performs no writes. The separate authenticated
`IFormulaMigrationEvidenceService` can atomically record that report in the new append-only legacy
artifact, migration-item, and reconciliation tables. It never changes legacy formula or response
rows. `IFormulaMigrationApplyService` accepts a hash-verified canonical manifest plus the digest
and location of its external signed approval report, rechecks the live source fingerprint and
approved dry-run ledger, enforces active three-person separation of duties,
and atomically imports approved canonical definitions/revisions, question links, key mappings,
status audits, and reconciliation evidence. It is idempotent for the identical signed release and
fails closed on any provenance or identity conflict.

None of these services is exposed through HTTP. `TOOLS/FormulaMigration` is the sole operator
entry point in this increment. Its offline commands seal and validate a canonical package; its
database commands require the expected database name and server endpoint, and its write commands derive the actor
from a signed, non-expired environment-matched application JWT and require the dedicated
`CanApplyFormulaMigration` permission plus an exact fingerprint/manifest confirmation token.
Connection strings are accepted only through `ORYX_FORMULA_DB_CONNECTION`. It does not
migrate form placements, response snapshots, executions, or switch runtime readers to formula v1.

See `docs/formula-v1-persistence-foundation.md` for deployment and integrity requirements and
`docs/formula-migration-operator-tool.md` for the controlled command workflow.

## Shift scheduling and working hours policy (2026-09-04)

`ShiftCategoryController` (`/api/v1/shift-category`) and `WorkingHoursPolicyController`
(`/api/v1/working-hours-policy`) are new; both follow the existing standard-CRUD
controller/repository split (`ShiftCategoryRepository`, `WorkingHoursPolicyRepository`).
`WorkingHoursPolicyController` exposes `POST`/`GET`/`DELETE` only — no `PUT` — because
the policy is effective-dated: a change is a new row, not an edit of an existing one,
the same contract shape as `PayeTaxBand`/`SsnitRate`.

`ShiftType.StartTime`/`EndTime` are `TimeOnly` on the entity but stay `string`
(`"hh:mm tt"`) on `CreateShiftTypeRequest`/`ShiftTypeDto`/`MinimalShiftTypeDto` —
the wire contract is unchanged, so this is not a breaking change for any existing
consumer of those DTOs.

`POST /api/v1/shift-schedules/assign`, `POST /api/v1/shift-schedules/swap`, and the
Excel-import endpoint can now return `Error.Validation("Employee.WorkingHoursPolicy", ...)`
in addition to their existing overlap/leave-conflict validation errors; the Excel
import instead adds the affected row to its existing per-row `skipped` response
list with a reason, never failing the whole upload for one row's policy breach.

## IT support tickets

All ticket routes require authentication. `POST /api/v1/tickets` is available
to every authenticated staff user and always records the authenticated user as
the reporter; clients cannot report on behalf of another user. The legacy
`CanCreateTicket` permission remains registered for role compatibility but is
not required by this endpoint.

`GET /api/v1/tickets` returns only tickets reported by or assigned to the
caller unless the caller has `CanViewAllTickets`. The optional `reportedById`
filter lets the My IT Issues page request only the caller's reported tickets,
including when that caller can view all tickets. Ticket detail and activity
endpoints return not found when the caller is neither reporter nor assignee and
lacks `CanViewAllTickets`, preventing ticket-number enumeration.

IT workflow permissions remain required for viewing every ticket, assigning,
closing, and commenting. Reopening is limited to the reporter or a user with
close permission.

## Product ARD and COA responses

`GET /api/v1/form/response` resolves a product response by the exact `batchManufacturingRecordId` and `productionActivityStepId`. A pair with no response returns `200` with `null`; it is not a validation failure. Response detail includes `approved`, `rejected`, and `hasPendingApproval`.

`POST /api/v1/form/responses/draft` creates the response container on the first field save and returns its ID. Subsequent fields submit that ID and must retain the same form, BMR, and production-step context. A different step under the same BMR creates a different response container.

Draft and final form submission require every field to belong to the response form. Existing response updates must retain the original form, batch, and production-step context. Product COA generation verifies the BMR/step ATR pair and refuses mixed-form responses. A pending or completed approval round produces a conflict; generating a COA never silently starts a replacement approval round.


## Cashflow and payments

All routes use the existing authenticated API version prefix. Mutating requests
are captured by the existing request audit middleware.

| Method and route | Permission | Contract |
| --- | --- | --- |
| `POST /api/v1/payments` | `CanRecordPayment` | Records a payment and returns its ID; it is pending when approval stages exist and auto-approved when none are configured. |
| `PUT /api/v1/procurement/billing-sheet/charge` | `CanRecordPayment` | Atomically records one full payment per selected billing-sheet charge; returns approved and pending charge IDs. |
| `GET /api/v1/payments/{paymentId}` | `CanViewPayments` | Returns payment detail and approval stages. |
| `POST /api/v1/payments/{paymentId}/review` | `CanApprovePayment` | Approves or rejects the caller's active approval stage. |
| `GET /api/v1/payments/ap-aging?asOf=` | `CanViewCashflowReports` | Supplier aging, grouped by supplier and converted to base currency. |
| `GET /api/v1/payments/ar-aging?asOf=` | `CanViewCashflowReports` | Customer aging, grouped by customer and converted to base currency. |
| `GET /api/v1/payments/cashflow-summary?asOf=` | `CanViewCashflowReports` | Projected approved-payment-adjusted inflows and outflows. |
| `PUT /api/v1/payments/base-currency/{currencyId}` | `CanRecordPayment` | Selects the single reporting base currency. |
| `POST /api/v1/payments/exchange-rates` | `CanRecordPayment` | Adds an effective-dated rate to the base currency. |
| `GET /api/v1/payments/exchange-rates/{currencyId}?asOf=` | `CanViewCashflowReports` | Returns the latest rate effective on or before `asOf`. |

`RecordPaymentRequest` requires a positive `amount`, `currencyId`,
`paymentDate`, `method`, unique payable-scoped `reference`, `payableType`, and
`payableId`. A payment must use a currency present on the payable and cannot
exceed that currency's outstanding balance. Valid numeric enums are:

- `PaymentMethod`: `0` bank transfer, `1` cheque, `2` cash, `3` credit card,
  `4` other.
- `PayableType`: `0` billing sheet, `1` shipment invoice, `2` purchase-order
  invoice, `3` customer invoice.

`MarkBillingSheetChargePaymentsRequest` requires a payment date, a valid
payment method, and at least one charge entry. Every entry supplies a
`billingSheetChargeId`, payable-scoped unique `reference`, and optional notes.
All selected charges must exist on one billing sheet, be unpaid, have positive
amounts and currencies, and have no pending or approved linked payment. The
operation is all-or-nothing. Its response separates `paidChargeIds` from
`pendingChargeIds`; a configured approval workflow keeps the charge unpaid
until final payment approval.

Reports require one base currency. Missing due dates, frozen customer-invoice
amounts, supplier/customer associations, currencies, or effective exchange
rates are returned in `dataQualityWarnings`; data is never silently guessed.

## Existing invoice contract additions

Invoice, shipment-invoice, purchase-order-invoice, and billing-sheet responses
now include `dueDate` and per-currency `balances`. Only approved payments appear
in those balances. `CreateInvoice` accepts optional `termsOfPaymentId` and
`amounts[]` (`currencyId`, positive `amount`) so new customer invoices retain an
immutable monetary snapshot for AR reporting.

## Supplier relationship management

SRM routes use `/api/v1/suppliers`. Existing supplier CRUD remains under the
Procurement controller for backward compatibility; the separate Vendor subsystem
is unchanged.

| Route group | Permission | Behavior |
| --- | --- | --- |
| `/{supplierId}/certifications` | `CanViewSupplierCertifications` / `CanManageSupplierCertifications` | Lists and manages GMP, ISO 9001, ISO 13485, WHO-PQ, and other certificates. |
| `/certifications/expiring?withinDays=&asOf=` | `CanViewSupplierCertifications` | Lists certificates expiring in the inclusive window. |
| `/requalification-due?withinDays=&asOf=` | `CanViewSupplierCertifications` | Lists overdue and upcoming approved-supplier requalifications. |
| `/{supplierId}/contacts` | `CanManageSupplierContracts` | Manages multiple supplier contacts and one primary contact. |
| `/{supplierId}/bank-details` | `CanManageSupplierContracts` | Manages supplier accounts by currency. |
| `/{supplierId}/pricing-agreements` | `CanManageSupplierContracts` | Manages standing material/UoM prices and resolves the active agreement as of a date. |
| `/{supplierId}/performance/compute` | `CanViewSupplierPerformance` | Computes delivery and quality performance without persistence. |
| `/{supplierId}/performance` | `CanViewSupplierPerformance` | Lists preserved scorecards or preserves a newly computed period. |
| `/{supplierId}/spend-summary?from=&to=` | `CanViewSupplierSpend` | Summarizes approved payments by month and original currency, with base-currency totals. |

Supplier approval requests now accept `requalificationIntervalDays` from 1 to
3650, defaulting to 365 for existing clients. Performance weights must each be
between 0 and 1 and total exactly 1. Pricing agreements require positive prices,
valid referenced entities, coherent dates, and no overlapping active window for
the same supplier/material/UoM.

## Customer relationship management

CRM extends the existing `/api/v1/customers` API and preserves the current
Customer, Invoice, ProformaInvoice, and ProductionOrder contracts.

| Method and route | Permission | Contract |
| --- | --- | --- |
| `GET /{customerId}/credit-status?additionalOrderValue=` | `ViewCustomerCreditStatus` | Returns approved-invoice outstanding balances, available credit, and a prospective limit check. |
| `GET/POST/PUT/DELETE /{customerId}/contacts` | `ManageCustomerContracts` | Manages multiple contacts with at most one active primary contact. |
| `GET/POST/PUT/DELETE /{customerId}/pricing-agreements` | `ManageCustomerContracts` | Manages standing product/UoM prices and resolves the inclusive active window. |
| `GET /{customerId}/quotations` | `ViewCustomerQuotations` | Returns non-expired, non-rejected, unconverted quotations with pagination. |
| `POST /{customerId}/quotations` | `CreateCustomerQuotation` | Creates a draft, defaulting omitted prices from an active agreement or existing product price. |
| `POST /quotations/{id}/send` | `CreateCustomerQuotation` | Freezes the draft for review and instantiates configured approval stages. |
| `POST /quotations/{id}/approval` | `ApproveCustomerQuotation` | Approves or rejects the caller's active maker-checker stage. |
| `POST /quotations/{id}/convert` | `ConvertCustomerQuotation` | Atomically creates one linked ProductionOrder from an accepted quotation. |
| `GET /{customerId}/order-history` | `CanViewCustomers` | Returns paginated ProductionOrder history, negotiated value, and delivery dates. |
| `GET /{customerId}/summary` | `ViewCustomerCreditStatus` | Returns lifetime order metrics, comparable-order on-time rate, and current outstanding balance. |

Customer create/update accepts nullable `creditLimit`, `termsOfPaymentId`,
numeric `type`, `currencyId`, `billingAddress`, and `shippingAddress`. Null
credit limit means unlimited. A limited customer must have a preferred currency.
Existing clients that omit new optional fields do not clear stored CRM values.
The update contract distinguishes omission from an explicit JSON `null`: omitted
CRM fields retain their stored values, while an explicit `null` clears a nullable
field (with the existing currency/credit-limit consistency validation applied).

Quotation item quantity must be positive, discount must be from 0 through 100,
and an omitted unit price is only defaulted from an unambiguous active agreement
or the current product price. The customer preferred currency is captured on the
quotation so monetary values are never currency-less.

## Cross-origin API access

The API applies its global CORS policy after routing and before authentication
and authorization. This ordering allows browser `OPTIONS` preflight requests to
complete without weakening authorization on the requested endpoint.

## Approval progression contract (2026-09-08)

- All configurable document entities implement `IRequireApproval`; downstream repositories call the shared fail-closed guard before business or inventory mutation.
- The guard covers requisition issue/sourcing, purchase-order and proforma sends, billing-sheet payment, shipment distribution, production order/allocation/loading/delivery, transfer-note acceptance, extra-material issue, stock-adjustment application, job execution, and R&D project/formulation/trial progression.
- An unapproved document returns `Approval.Required` without mutation. Stock issue retains the domain-specific `Requisition.ApprovalRequired` code and checks before warehouses, stock, reservations, bin cards, or movement records are resolved.
- Missing or zero-stage configurations set the same approval flag through an audited automatic decision; payroll runs are included in that path.
- `POST /api/v1/requisition/{requisitionId}/issue` is retained for contract compatibility but returns `Requisition.ApprovalWorkflowRequired`. Only `POST /api/v1/approval/approve/{modelType}/{modelId}` may authorize a configured stage, subject to assigned-user/role checks.
