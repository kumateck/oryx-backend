# Unified pending revision approvals (2026-09-29)

`GET /api/v1/approval/my-pending/revisions` is authenticated and returns
`revisionId`, `resourceId`, `resourceType`, `action`, `label`, `contentHash`,
`createdAt`, and `resourcePath` for currently actionable formula, Full
Procedures, and unconfigured OOS disposition work. It filters by permission,
formula stage assignment, area role grants, and independent actor rules.
The existing generic and QC pending endpoints remain active.
`GET /api/v1/approval/my-pending/document-counts` returns the current actor's
pending counts keyed by ApprovalDocument. List requests specify one document:
generic approvals use `modelType`; QC and revision queues use
`approvalDocument`. The OOS document may query both QC stages and the
no-workflow disposition read model. Decisions use their resource endpoints.
The generic detail routes constrain `modelId` and `approvalId` to GUIDs so
`my-pending/revisions` and `my-pending/document-counts` cannot be interpreted
as detail identifiers. Both new routes require the backend release containing
these controllers; an older deployment returns a `modelId` validation error.

# Service contracts

## Full Procedure Definitions (2026-09-16)

`/api/v1/procedures` provides stable Procedure identities and immutable
revisions. A revision pins one exact Published Workflow revision and content
hash, declares product/site applicability for Trial, Validation, Commercial or
Scale Up batches, and assigns every Activity node to exactly one Manufacturing,
Packaging, Shared or Development record scope. Production-purpose Procedures
cannot use Development scope.

Reads, create, draft edit, validation, submission, review, approval and
retirement use distinct Procedure permission keys plus area assignment. Draft
writes and lifecycle commands are reasoned and hash-fenced. Approval rechecks
the Workflow/hash, complete Activity scope coverage and active Product/Site
dependencies; approving a replacement retires the previous Approved revision
transactionally. Validation is side-effect-free. This service does not create a
BMR/BPR master, release bundle, effective assignment, issued batch or run. See
[`full-procedure-definitions.md`](full-procedure-definitions.md).

## Full Procedures Template Adoption (2026-09-16)

`POST /api/v1/template-adoptions/from-grant/{grantId}` consumes one current
Active sharing grant and creates a new Draft owned by the target area. The
request must map every source dependency exactly to a compatible target
revision; Activity adoption additionally maps performer/checker/approver roles.
Question, Section, Form, Activity and Workflow adoption are supported.

The operation rechecks the exact Published source revision and hash, target
Author assignment and both active areas. In one serializable transaction it
creates the Draft through the existing kind-specific service, increments the
grant version, appends an `Adopted` grant audit and stores immutable lineage.
Source approvals, actors, responses, evidence and consumers are not copied.
`GET /api/v1/template-adoptions/{adoptionId}` is scoped to the target area's view
authority. See
[`full-procedures-template-adoption.md`](full-procedures-template-adoption.md).

## Full Procedures Template Sharing (2026-09-16)

`/api/v1/template-sharing` governs the approval record for cross-area adoption
of one exact Published
Question, Section, Form, Activity or Workflow revision. A source Publisher or
target Author may request access; a different actor assigned to the other area
must approve or reject it. Approval rechecks the revision hash, Published state,
and target purpose/subject support. Revocation is version-checked and requires a
Publisher in either participating area.

Area listings and usage-impact reads require global view permission plus area
assignment. Usage returns direct definition-reference and active-grant counts
only; it does not query or expose completed responses, evidence, signatures, or
subject records. An Active grant does not itself authorize a direct cross-area
reference; the separate adoption command creates a target-owned Draft. See
[`full-procedures-template-sharing.md`](full-procedures-template-sharing.md).

## Full Procedures Template Workflows (2026-09-16)

`/api/v1/template-workflows` provides an immutable, versioned graph of typed
nodes (Start, End, Activity, Branch, Fork/Join, Wait/IPC, Hold/Resume,
Rework) and edges. Reads use `CanViewQuestionTemplates`; drafting/submission
uses `CanManageWorkflowTemplateRevision`; review and publication use separate
keys. Area assignment and active state are always rechecked.

Activity nodes bind an exact Published Activity revision in the same
area/purpose/subject context. The graph must have exactly one Start, at least
one End, every node reachable, fork/join and hold/resume keys paired
one-to-one, and no cycle outside the explicit, attempt-capped Rework
back-reference. Node layout (position) is persisted separately from this
governed content and does not affect its hash. Node labels and node/edge order
are governed semantic content and do affect the hash. Non-finite layout
coordinates are rejected. See
[`full-procedures-workflow-revisions.md`](full-procedures-workflow-revisions.md).

## Full Procedures Template Activities (2026-09-16)

`/api/v1/template-activities` provides immutable Activity definitions. Reads use
`CanViewQuestionTemplates`; drafting/submission uses
`CanManageActivityTemplateRevision`; review and publication use separate keys.
Area assignment and active state are always rechecked.

Requests bind exact Published Forms, ordered typed actions and role sets,
configured resource capabilities, typed data inputs/outputs and completion
rules. The service validates segregation, action/capability compatibility and
rule coverage, then rechecks references at publication. It does not dispatch an
action. See [`full-procedures-activity-revisions.md`](full-procedures-activity-revisions.md).

## Full Procedures Template Sections and Forms (2026-09-16)

`/api/v1/template-sections` and `/api/v1/template-forms` provide the next two
immutable definition layers. Reads require `CanViewQuestionTemplates`. Draft
create/change and submission use the kind-specific manage permission; review
and publication use separate review/publish permissions. Every command also
checks the active area and the actor's area grant.

Sections order exact Published Question revisions and contain only local,
backward-looking conditions. Forms order exact Published Section revisions,
carry required/evidence/signature declarations, and can condition a later
Section on an exact Question revision in an earlier pinned Section. Both APIs
reject unpublished, mismatched, cross-context and missing dependencies,
hash-fence changes, and revalidate dependencies before publication. See
[`full-procedures-section-revisions.md`](full-procedures-section-revisions.md)
and [`full-procedures-form-revisions.md`](full-procedures-form-revisions.md).

## Full Procedures Template Questions (2026-09-16)

`/api/v1/template-questions` now provides area-scoped stable questions with
immutable revisions. List/detail require `CanViewQuestionTemplates`; draft
create/change and submission require `CanManageQuestionRevision`; review and
publication require their separate review/publish keys. The service additionally
checks the actor's exact area grant and active area state. Expected SHA-256
content hashes fence draft edits and every lifecycle transition.

Calculation inputs are resolved server-side to both a stable question ID and
the exact Published source revision ID in the same area. Published content is
never updated in place; Reviewers can return In Review content to Draft for
correction, while Publishers can explicitly retire a Published revision. A
replacement starts a new Draft and publication retires the prior Published
revision transactionally. See
[`full-procedures-question-revisions.md`](full-procedures-question-revisions.md).

## Full Procedures Template Areas (2026-09-16)

`/api/v1/template-areas` provides the persistent, deployment-configured area
registry. Catalog/list/detail reads require `CanViewQuestionTemplates`;
create/update/activation require `CanManageTemplateAreas`. The service also
checks owner-role or area-role assignment, so a global permission alone cannot
read or administer an unrelated area. Requests use expected versions and
reasoned changes; 403, 404 and 409 outcomes remain distinct. See
[`full-procedures-template-areas.md`](full-procedures-template-areas.md) for the
endpoint, data, audit and migration contract.

## Full Procedures permission catalog (2026-09-16)

`FullProcedurePermissionKeys` defines the stable capability strings and
`FullProcedurePermissionCatalog` adds each one exactly once to
`PermissionUtils.GeneratePermissions()`. They appear in role administration
with no access types until an administrator explicitly grants them. Catalog
membership is not resource authorization: endpoints must also validate
company/deployment context, area, assignment, qualification, lifecycle state,
and segregation of duties. The Template Area, Question, Section, Form, Activity,
Workflow, Sharing, Adoption and Procedure endpoints now enforce the area
and assignment boundary; other future endpoints must do the same.
`CanManageTemplateAreas` and `CanReviewTemplateRevision` are separate from
authoring and publishing. The Template Question, Section and Form endpoints now enforce these
separate lifecycle capabilities and actor segregation. `TemplateAreaPolicy` validates reviewed descriptor and
owner-group references; `TemplateAreaAuthorization` requires both the operation
key and resource assignment. The DI-registered Template Area service persists
that policy. It does not authorize Procedure execution.

## Service quotation stage and SMTP configuration (2026-09-16)

`GET /api/v1/service-quotations` returns the related numeric `jobOrderStatus` in each `ServiceQuotationDto`. Optional `jobOrderStatus` and `proformaPending` query parameters filter before pagination; the latter includes comparison-ready orders and only the selected quotation on an order awaiting proforma. This is derived from the persisted job order, not a second quotation state. `POST /api/v1/job-orders/select-quotation` accepts comparison-ready orders, returns success for an identical already-selected quotation, and rejects any transition after selection has advanced to proforma or later stages. This prevents rollback of governed state. SMTP send requires explicit `SMTP_HOST`, `SMTP_USERNAME`, and `SMTP_PASSWORD`; optional `SMTP_FROM` defaults to the authenticated username. A 535 authentication error remains a deployment credential/account issue; failed sends do not mark vendor requests as sent.

## Chemical, Microbial, Routine QC, and water coverage (2026-09-15)

The delivered entities, endpoint contracts, migrations, validation rules, and remaining
boundaries are documented in
[`quality-ard-routine-microbiology-2026-09-15.md`](quality-ard-routine-microbiology-2026-09-15.md).
Routine and commercial certificates use explicit immutable endpoints. Water quality
coverage remains separate from the unlimited water inventory batch.

## Full Procedures Phase-0 service boundary (2026-09-15)

`APP.Services.FullProcedures.ProcedureRuntimeSpike` is a side-effect-free proof,
not a DI-registered service or endpoint. It checks graph/content pinning and
transition eligibility but does not validate actor authority, approved QC
receipts, BMR/BPR releases or inventory effects. No API contract has changed.
`ProcedureSqlSpikeStore` and `ProcedureSqlOutboxSpike` are likewise unregistered
and refuse any database name other than `oryx_procedure_spike_test`. Their
disposable PostgreSQL proof writes
state/audit/outbox atomically and deduplicates local effect receipts. A leased
retry test uses a fake idempotent adapter and proves one fake effect after a
forced post-effect failure. A separate test-only worker is killed before or
after its fake effect/local receipt, then a new process reclaims the event;
neither sends real outbox events or executes domain effects. See
`full-procedures-phase-0-backend.md` for the remaining runtime decision.
`ProcedureActionContractSpike` is also unregistered: it snapshots exact action
contract content and interpreter versions at prototype issue, then blocks an
unknown/missing version at interpretation. Its hash is not approval evidence,
and it does not authenticate an actor, select BMR/BPR masters or perform effects.

## Template drafting and ARD readiness boundary (2026-09-08)

- `POST /api/v1/form` permits a form section/test whose `fields` collection is empty so template
  authors can divide test and question entry across authorized editors.
- Product and material ARD creation load the selected form with its active sections and fields and
  apply operational readiness validation before checking the STP or writing the ARD.
- An absent form returns `Form.Invalid`; a form with no sections returns `Form.Section`; any active
  section with no active field returns `Form.Question`. No ARD row is written on these failures.

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

The role-permission read endpoint returns the complete generated permission catalog, including
newly introduced permissions with an empty access-type list. This allows administrators to grant
new view permissions (such as `CanViewReviewFormulaRevision` and
`CanViewApproveFormulaRevision`) without a manual role-claim backfill. Permission responses use a
versioned cache key so older filtered catalogs cannot hide newly registered permissions.

Replacement approvals run in serializable transactions, retire the prior effective revision, and
append a `Superseded` audit row. Migration
`20260907163852_EnforceSingleEffectiveFormulaAndFormRevision` adds filtered unique indexes for one
non-deleted Approved revision per formula definition and per form. A uniqueness or serialization
race returns a controlled conflict instead of creating ambiguous effective content.

The API deployment overlay is `docker-compose.formula.yml`. It mounts the same Docker secret used
by the calculation service, uses the service-only `sail` network address, and leaves
`FORMULA_RUNTIME_ENABLED=false` unless a controlled release explicitly enables it.

The demo GitHub Actions deployment is coordinated across repositories: it waits up to 120 seconds
for the `formula-calculation` container to become healthy, verifies that the container exposes a
non-empty `formula-service-token`, and mounts the same Docker volume (or legacy bind source) into
the API. A timeout fails with the observed container status and recent service logs instead of
silently starting an API that cannot evaluate governed formulas.

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
| `GET /{customerId}/quotations/convertible` | `ViewCustomerQuotations` | Returns approved, unexpired quotations that have no ProductionOrder link. |
| `GET /{customerId}/quotations/resolve-price` | `CreateCustomerQuotation` | Resolves an active agreement price or the product list-price fallback for a product/packing selection. |
| `POST /{customerId}/quotations` | `CreateCustomerQuotation` | Creates a draft, defaulting omitted prices from an active agreement or existing product price. |
| `POST /quotations/{id}/send` | `CreateCustomerQuotation` | Freezes the draft and registers its configured stages with the central approval workflow. |
| `POST /approval/approve\|reject/CustomerQuotation/{id}` | central approval permissions plus stage assignment | Reviews the active quotation stage from My Approvals and records the audit action. |
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
or the current product price. The customer preferred currency is captured when
configured. Otherwise, every line must have one active pricing agreement and
their common agreement currency is captured on the quotation; mixed or missing
agreement currencies are rejected so monetary values are never currency-less.
Quotation item responses expose `packPerShipper`, `shippers`, and `loose` as
deterministic packing previews. Conversion recomputes and persists the same split
from the approved quotation and referenced packing style.

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

## Governed formula question boundary (2026-09-10)

- `POST /api/v1/formula-questions` and `PUT /api/v1/formula-questions/{questionId}` are the only
  authoring endpoints for formula questions. The generic question repository rejects formula
  creates, edits, and attempts to change an existing formula into another type with
  `Form.Question.FormulaGovernanceRequired`.
- A save changes question metadata and a Draft revision only. It does not delete, recreate, or
  overwrite the existing `QuestionOption`, so active legacy forms retain their prior executable data.
- `FormulaRevision.AuthoringPayloadJson` and `AuthoringPayloadHash` preserve the exact editable
  representation separately from the canonical executable definition. Existing rows remain valid
  with both fields null and use the preserved legacy option only as an edit-screen compatibility
  fallback; the first governed save creates populated evidence.
- Validation remains server-authoritative. Draft persistence may succeed while the calculation
  service is unavailable, but submit-for-review revalidates and fails closed. Only Approved formula
  revisions may participate in an approved form revision.
- The API applies a global AutoMapper object-graph `MaxDepth(32)` in addition to its execution-plan
  depth limit. This is the runtime recursion mitigation for CVE-2026-32933 while the project remains
  on the open-source 14.x dependency line; package-audit warnings remain visible in CI.
## QC materials-ready-for-checklist report (2026-09-14)

`GET /api/v1/report/materials-ready-for-checklist` now accepts optional
`departmentId`, `materialKind`, `startDate`, and `endDate`. Production callers are
server-scoped to their own department; non-production and R&D callers default to all
production departments and may select one active production department. The response
is a lean `MaterialReadyForChecklistDto[]` projection rather than a full distributed
material entity graph. See [the report contract](materials-ready-for-checklist-report.md).

## Job request assignment delivery (2026-09-16)

- `POST /api/v1/job-requests/assign-internal` still accepts the existing employee, assigner, request, and notes contract.
- The selected employee must map by email to an active ERP user. A missing or disabled account returns `Employee.UserAccountUnavailable` before assignment records are created.
- A successful assignment creates the job execution and then sends an in-app `JobRequestAssigned` notification directly to that user. The notification uses the existing persisted notification and activity-log service.
- Service quotation selection remains the prerequisite mutation for `POST /api/v1/service-proforma-invoices/request`; no request payload shape changed.

## Assigned approval queue (2026-09-27)

`GET /api/v1/approval/my-pending` is scoped to the authenticated user. A mismatched `userId` returns 403. Only active pending stages are returned, including staff requisitions and production orders. The dedicated `GET /api/v1/approval/my-pending/{modelType}/{modelId}` and generic approval actions check that the caller has the document in their assigned queue. The preexisting generic detail endpoint remains available for historical views. The QC queue at `GET /api/v1/qc/worksheets/approvals/my-pending` now includes worksheet instances and OOS cases with detail routes. QC decisions continue through signed domain actions.

## Full Procedures template scope (2026-09-28)

Question, Section, Form, Activity, and Workflow create endpoints validate the
Area's purpose and subject against the same catalog that defines allowed
template kinds. A purpose/subject pair outside the catalog, or a template kind
not allowed by that purpose, returns the existing invalid-template result
before any draft or audit row is written. The request and response shapes are
unchanged. See `docs/full-procedures-template-scope-2026-09-28.md`.

## Exact calculation input revisions (2026-09-28)

`POST /api/v1/template-questions`, its new-revision endpoint, and its draft
update endpoint accept `calculationReferences` pairs containing `questionId`
and `revisionId`. The server requires each exact revision to be Published in
the same Area; stale and mismatched references fail validation. The existing
`calculationQuestionIds` field remains available to existing callers, but may
not be combined with the exact-pair field. Adoption maps exact target revision
IDs through the same path.
