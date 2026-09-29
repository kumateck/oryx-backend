# Approval fallback and review (2026-09-29)

A shared approval with configured stages remains manual. Formula revisions
require exactly two stages (review, approval); another nonzero stage count
returns a configuration error. With no formula stages, a validated Draft is
automatically approved and a `SystemAutoApproved` formula audit is written.
Full Procedures area policy counts as configured, preserving independent
review and publish/approve gates.

With no QC approval stages, validated STP, worksheet template, specification,
and worksheet-instance submissions use the generic automatic approval audit.
QC document supersession and COA revision write explicit system approval
action logs. QC submission wraps the status write, approval creation or automatic
completion, and worksheet OOS detection in one database transaction. The
generic automatic audit reason includes workflow ID, stage count, initiating
actor when supplied, and an event correlation ID. An OOS case remains pending until an authorized actor selects a
valid outcome; then the system approval log and batch disposition are committed
together. Configured QC workflows retain signed actions.

# Workflow behavior

## QC worksheet choice options and fixed columns (2026-09-24)

Worksheet fields gain a nullable `OptionsJson` (a JSON string array) on the field and on its
revisions (brief 07, Phase A).

- **Template create and update:** Select, MultiSelect and GrowthObservation fields must have
  two or more distinct options, and other types must have none. A table's `fixedValues`
  columns must all be the same length. `rowHeader` needs `fixedValues`.
- **`SaveValues` and submit:** both refuse a choice that is not an option
  (`ValueNotAnOption`) before anything is written.
- **Existing templates:** a template with no options is not checked at runtime, so existing
  Effective templates keep working. An existing Draft must add options the next time it is
  saved.
- **Unchanged:** approval, instance status and every other workflow.

- **Calculated columns:** a table column with `"mode": "Calculated"` is computed per row at
  submit and stored like an entered cell. An unevaluatable row blocks submit with a
  cell-targeted `CalculatedFieldUnevaluatable`. `SaveValues` refuses writes to these columns.

See `docs/qc-rebuild/field-catalog.md` for the column contract.

## QC worksheet table row headers (2026-09-22)

A template table may define one row-header column with fixed labels. These are
template content, not analyst entries: `SaveValues` rejects a value addressed
to that column, and formula validation excludes it from table aggregates.
Other table columns remain normal result inputs. The frontend uses the number
of configured labels as the fixed row count; tables without headers retain
open-ended rows. No approval or instance status changes are implied.

## QC worksheet template constants (2026-09-22)

Creating or updating a worksheet template validates that each Constant-mode
field has a nonblank `ConstantValue`. Heading fields are exempt because their
label is the displayed heading. The template stores the fixed value, includes
it in the detail and worksheet instance DTOs, and worksheet execution renders
it as read-only method text. This validation happens before a template write,
so rejected drafts create no template or field revisions.

## Full Procedures Phase-0 backend semantics (2026-09-15)

The isolated `APP/Services/FullProcedures/ProcedureRuntimeSpike.cs` proves graph
transition gates in tests only. It does not change existing Route activity status,
approval, QC, inventory or dispatch workflows. Its QC receipt is not verified,
JSON restore is not durable recovery, and Abort has no disposition/closeout
adapter. See `full-procedures-phase-0-backend.md` before connecting any live flow.
The disposable PostgreSQL spike proves rollback and retry of state/audit/outbox
without changing any existing approval, stock or release workflow. A test-only
leased outbox worker survives a simulated failure after a fake idempotent
adapter effect: two attempts produce one fake effect. A separate test worker is
also killed at three windows around the fake effect and local receipt; a new
process resumes the lease-expired event while restoring the committed run. It
has no production dispatcher or
real domain adapter; this cannot prevent duplicate physical work.
The isolated action-contract proof retains issue-time action content rather than
consulting newer catalog drafts. An unsupported runtime/action version yields a
blocked code; it does not silently substitute another version or change any
existing operator workflow.

## Collaborative template drafting and ARD creation (2026-09-08)

1. A template author may save a named test with zero questions as an incomplete draft.
2. Another authorized editor may add questions later through the incremental form-field endpoint.
3. Product and material ARD creation validate every active test in the selected template.
4. If any test has zero active questions, creation returns `Form.Question` before the ARD is
   persisted. This keeps incomplete drafting state out of regulated execution records.

## STP document lifecycle (2026-09-07)

1. A saved material/product STP receives a blank or uploaded `.docx` Draft.
2. The editor stores changed content as a new version with its hash and actor; an active edit lock
   blocks submission until the final save callback completes. The callback actor must be the user
   holding that lock, and failed persistence is retried instead of silently acknowledged.
3. Draft moves to In Review. A different user password-signs the review, producing Reviewed.
4. A third user password-signs approval; only that transition makes the version effective.
5. Rejection from In Review/Reviewed returns to Draft with signature evidence. Upload is prohibited
   while review is active, so review cannot be silently cancelled.
6. Revisions of Approved documents require a reason and do not replace the effective version until
   the new Draft completes the same controlled lifecycle.
7. A retry of the identical current Draft file returns the existing document and creates no extra
   version. This makes partial multi-owner upload recovery safe without weakening append-only
   history or suppressing deliberate Approved-document revisions.

All reads, downloads, editor sessions, and state-changing calls enforce the matching product or
material STP permission at the API boundary. Uploaded files must open as real `.docx` packages;
extension checks or ZIP magic bytes alone are insufficient.

Editor startup requires the same effective JWT secret in the API and ONLYOFFICE runtime and a
container-reachable `API_INTERNAL_BASE_URL`. Release verification opens a signed editor session,
downloads its bound document through ONLYOFFICE, and confirms the callback route is reachable;
an HTTP-only Document Server health response is not sufficient evidence of integration readiness.
The API checks object availability before it takes a Draft lock, so an orphaned version returns an
actionable storage error instead of launching an editor that can only report “Download failed.”
Submission, review, and approval repeat the same check so missing controlled evidence cannot cross
a regulated lifecycle boundary through either the application UI or a direct API call.

## Formula dependency execution and migration rehearsal (2026-09-07)

The demo deployment workflows run independently. The API deploy therefore waits for the
`formula-calculation` service rather than checking it once and racing the frontend deployment.
It verifies the service health and token mount for up to 120 seconds, then reuses the service's
Docker secret volume when starting the API. If the service is still unavailable, the deploy fails
with its status and recent logs before stopping the existing API container.

1. A response formula snapshot freezes typed source bindings from its approved form revision.
2. The input resolver reads same-response source evidence. Table statistics are calculated under
   the frozen table precision; formula-result sources must be authoritative and current by input
   hash.
3. Submission orders all formula snapshots topologically, appends executions, and only then creates
   the immutable submission set. A graph error or unavailable calculation service blocks the
   transition without partial regulated status changes.
4. Schema deployment was rehearsed on a disposable clone by rollback, forward migration, and
   repeated forward migration. Legacy formula question/option counts and their evidence digest were
   unchanged. The clone is retained for inspection; this rehearsal does not authorize legacy
   corrective migration or fabricate scientific approval.

## Formula v1 persistence lifecycle (2026-09-05)

The implemented authoring/runtime sequence is:

1. Save a formula Question and `FormulaRevision` draft atomically. The existing option payload is
   retained for compatibility and migration evidence.
2. Validate, submit, review, and approve the formula revision. Author, reviewer, and approver must
   be distinct active users with the required permissions.
3. Edit the template, create or refresh a `FormRevision` draft, and bind each formula field to an
   approved formula revision. Incomplete drafts are allowed; review submission is fail-closed until
   all formula fields are configured, hashes verify through the authoritative service, and the
   cross-formula graph is acyclic.
4. Review and approve the template revision using separate author/reviewer/approver identities.
   Approval retires the prior approved revision with audit evidence.
5. A new Response selects the approved template revision. Evaluation snapshots its executable
   definition and bindings, resolves stored response inputs server-side, and appends execution
   evidence. Recalculation never overwrites an older execution.
   During the compatibility window, a form containing only preserved legacy JSON formulas may
   still create an unversioned response and save ordinary fields. The presence of a formula linked
   to the governed definition model, or any governed placement history for the form, instead
   requires an approved form revision for the response as a whole. This preserves the immutable
   placement/snapshot boundary and prevents retirement from reopening legacy execution.
6. Final submission appends a fresh authoritative execution for every placement and creates an
   immutable submission set. Any missing, invalid, provisional, stale, or unverified result blocks
   submission; later approval references the same set.

Print and COA views consume stored results only and must not invoke a calculation engine.

1. Formula drafts may be edited. Entering review freezes executable content and evidence;
   every later status change requires a matching append-only audit row in the same transaction.
2. Approved formula revisions are referenced by versioned template placements. A response
   eventually captures an immutable snapshot per placement; later rebases or historical
   corrections add an approved snapshot generation and never replace the original.
3. Each evaluation adds a `FormulaExecution`. Recalculation links to a prior execution instead
   of updating it. Only valid authoritative final-submission executions may enter the immutable
   response submission set used by approval.
4. Legacy formula payloads are not altered by the schema migration. Migration tooling must
   preserve source evidence and classify each item as exact, normalized, corrective, or
   unrecoverable before creating new revisions.
5. This increment supplies persistence, database guards, dry-run inventory, immutable dry-run
   evidence recording, and a controller-free controlled definition importer. Until placement
   migration, the authoritative evaluator, and feature flags are enabled,
   existing ARD calculation/finalization behavior remains unchanged.
6. The internal migration inventory dry run hashes source payloads, validates one decision per
   artifact, and creates deterministic proposed IDs. An authenticated internal recorder may store
   that report and exact source evidence in the new append-only ledger; it never changes a legacy
   row. A fingerprint mismatch, missing approval, unknown decision, or active unrecoverable
   placement keeps Apply readiness false.
7. Apply requires the exact signed manifest, a completed reconciled dry run, unchanged live source,
   active importer/reviewer/approver accounts, and three-person separation of duties. Corrective
   items require individual approval; exact/normalized items may use batch approval. The importer
   adds approved revisions and audit evidence in one serializable transaction, never changing the
   legacy option payload. An identical signed replay is idempotent; a conflicting replay stops.
8. The controller-free operator tool separates non-writing `seal`, `validate`, and `dry-run`
   commands from `record-dry-run` and `apply`. Write commands authenticate a non-expired,
   environment-matched application token, require `CanApplyFormulaMigration`, confirm the exact
   target database name and server endpoint, and require an exact source-fingerprint or manifest-hash token. The
   signed approval report file is rehashed before Apply.

The controlled deployment and rollback boundary are documented in
`docs/formula-v1-persistence-foundation.md`; operator execution is documented in
`docs/formula-migration-operator-tool.md`.

## Shift scheduling gap fixes and compliance (2026-09-04)

1. `ShiftType.StartTime`/`EndTime` are a native `TimeOnly` column. The wire contract on `CreateShiftTypeRequest`/`ShiftTypeDto`/`MinimalShiftTypeDto` is unchanged (`string`, `"hh:mm tt"`); AutoMapper does the `TimeOnly <-> string` conversion at the boundary.
2. `AssignEmployeesToShift`, `SwapShift`, and the Excel import all call the same `ShiftTimeHelper.HasOverlap(TimeOnly, TimeOnly, TimeOnly, TimeOnly)` — two shift-type ranges are compared as if both apply to the same calendar date (matching how every call site actually uses it: assignments being placed on one `ScheduleDate`). This correctly catches two overlapping overnight shifts (e.g. 20:00-04:00 vs 22:00-06:00) but does not detect a conflict that spans two different `ShiftAssignment` rows on adjacent calendar dates — a known, pre-existing limitation, not new behavior.
3. Every `ShiftScheduleController`/`ShiftTypeController` action has an explicit `[Authorize(PermissionKeys.X)]` matched to its actual effect: Assign and the Excel import require the Create permission, Swap and Update require Edit, reads require View, Delete requires Delete.
4. A successful `AssignEmployeesToShift`, `SwapShift`, or Excel-import row enqueues a `NotificationType.ShiftAssigned` notification via the existing `IBackgroundWorkerService.EnqueueNotification` — the enum value existed and was previously never fired.
5. `ShiftCategoryController` (new) provides CRUD at `/api/v1/shift-category`, mirroring `ShiftTypeController`.
6. A `WorkingHoursPolicy` row (`MaxHoursPerDay`, `MaxHoursPerWeek`, `MinDailyRestHours`, `MinWeeklyRestHours`, effective-dated) is looked up for the assignment date before `AssignEmployeesToShift`, `SwapShift`, or an Excel-import row commits. A breach of the weekly-hours cap or the minimum rest gap since the employee's nearest adjacent shift rejects the change with `Error.Validation("Employee.WorkingHoursPolicy", ...)` (Excel import instead adds the row to its existing per-row `skipped` list with a reason, never failing the whole upload). `WorkingHoursPolicySeeder` pre-seeds the Ghana Labour Act, 2003 (Act 651) baseline — 8h/day, 40h/week, 12h daily rest, 48h weekly rest — effective 2026-01-01. There is no update endpoint; a policy change adds a new effective-dated row, matching `PayeTaxBand`/`SsnitRate`.
7. The duplicate plural permission-key set (`CanViewShiftSchedules` etc., `PermissionSubmodules.Schedules`) was removed; the singular set (`CanViewShiftSchedule`, `PermissionSubmodules.ShiftsSchedule`) is canonical and is what both the frontend nav guard and page guard now reference.

## Personal IT issue reporting

1. Every authenticated staff user can report an issue. The server supplies the
   reporter and department from the authentication context and records the
   initial activity entry.
2. My IT Issues filters the paginated list by the authenticated reporter. Staff
   can follow status, assignment, due date, and activity without access to the
   IT team's all-ticket board.
3. A reporter or assigned IT agent can read the ticket and its activity. Users
   with `CanViewAllTickets` can read all tickets for triage. Other authenticated
   users receive a not-found response for ticket details and activity.
4. IT workflow permissions continue to control assignment, closure, and
   commenting. The existing reporter-or-supervisor rule controls reopening.

## Product ATR release

Each reached Intermediate, Bulk, or Finished test stage owns one response for its exact BMR and production step. COA generation starts the first approval round only. Pending, approved, rejected, and completed rounds cannot be regenerated through the COA endpoint; rejected results require an explicit audited revision capability.

Before the first field is answered, an exact response lookup succeeds with an empty value. Saving that field creates and returns the response ID; every later field for the same BMR/step reuses it. Moving to another stage/step ignores the earlier response and creates a separate container.

On final approval, the backend resolves the ATR using both the response BMR and production-step IDs, releases that ATR, and completes that step. It does not require a future stage to exist before production reaches the stage's configured step. A database uniqueness constraint prevents concurrent creation of multiple product responses for one BMR/step; its migration stops for manual reconciliation if historical duplicates are detected.

Migration `20260902215903_AddUniqueProductResponseStep` is part of the application schema and must be applied independently to every development, demo/staging, and production database. A successful run on one database does not cover another database. Each target performs its own duplicate preflight before the unique constraint is created.


## Payment maker-checker

1. A user with `CanRecordPayment` records a positive payment against one
   supported payable. The backend verifies the payable, currency, reference,
   and remaining approved-payment-adjusted balance.
2. When approval stages are configured, the payment starts as `Pending` and
   recording it does not reduce the operational balance or any cashflow report.
   With no configured stages, the existing audited auto-approval policy applies.
3. Configured approval stages are copied from the workflow. A reviewer
   must be the assigned user or hold the assigned role, cannot review their own
   payment, and can act only on the active stage in order.
4. Final approval re-checks the outstanding balance before setting `Approved`.
   Rejection sets `Rejected`. Every review retains reviewer, time, stage, status,
   and comments.
5. Only `Approved = true` payments reduce AP/AR balances. Pending and rejected
   payments remain traceable but have no financial effect.

Payment recording, final balance re-checks, and base-currency switching use
serializable database transactions on relational providers so concurrent
requests cannot silently create an overpayment or multiple base currencies.

Payment rows are not updateable or deleteable through the API. Corrections must
use a future auditable reversal workflow rather than rewriting history.

### Billing-sheet charge payment entry

1. Pay Charges requires `CanRecordPayment`, payment date and method, and a
   unique reference for every selected charge.
2. The server records one full `Payment` per charge with
   `PayableType.BillingSheet`, the parent billing-sheet ID, and a nullable
   traceability link to the exact charge. A serializable outer transaction
   makes a multi-charge submission all-or-nothing.
3. With no configured payment workflow, each payment follows the existing
   audited auto-approval policy and its charge is marked paid immediately.
4. With configured stages, the payment remains pending and the charge remains
   unpaid. Final approval updates the charge's paid cache and audit metadata;
   rejection leaves it available for a new, separately referenced payment.
5. Shipment progression continues to check the charge's paid flag, so a
   pending or rejected payment cannot satisfy the clearance gate.

## Due dates and invoice snapshots

`TermsOfPayment.DueDays` is the explicit rule for new documents. When terms are
available, creation captures a nullable `DueDate`; the date is a snapshot and is
not recalculated when terms later change. Customer invoices also freeze totals
per currency in `InvoiceAmounts`.

The migration only derives `DueDays` for unambiguous legacy names (`Net N`, due
on receipt, due upon receipt, cash on delivery). It only derives legacy invoice
due dates when every linked source agrees. Unknown or ambiguous values remain
null and are placed in the `DueDateUnknown` report bucket.

## Currency and reporting

Exactly one currency is selected as the reporting base currency through the
repository operation. Exchange rates are append-only, effective-dated records;
the latest rate on or before the requested report date is used. Base currency
always resolves to a rate of `1`.

AP aging covers billing sheets, shipment invoices, and purchase-order invoices.
AR aging covers customer invoices with frozen amounts. Aging boundaries are
current, 0–30, 31–60, 61–90, over 90, and unknown due date. Cashflow projections
use overdue, next 7 days, 8–30, 31–60, 61–90, beyond 90, and unknown due date.
Missing data is reported as warnings instead of being silently converted or
discarded.

## Supplier qualification and monitoring

Supplier approval records `ApprovedAt` and derives `RequalificationDueDate` from
the interval supplied by the approver. Existing approved suppliers are not
assigned fabricated historical dates during migration; they remain visible for
controlled data remediation.

Certificates retain their issuer, number, validity period, type, and optional
attachment. Expiry queries use inclusive date boundaries. Contacts and commercial
records are soft-deleted, preserving audit history; a database-filtered unique
index prevents more than one active primary contact per supplier.

Standing pricing agreements cannot overlap for a supplier/material/UoM. Active
resolution is inclusive of `EffectiveFrom` and `EffectiveTo`. If legacy or
externally inserted data yields multiple matches, the repository logs and returns
an ambiguity error instead of choosing a price.

Supplier performance is computed from existing operational evidence:

- on-time delivery compares shipment arrival with the linked purchase order's
  expected delivery date;
- quality reject rate follows supplier-linked checklists to material batches and
  counts rejected batches;
- caller-supplied delivery and quality weights must total 1;
- persisted scorecards are immutable period snapshots, so later source changes do
  not rewrite historical evaluations.

Spend summaries use approved Cashflow payments only. Pending or rejected payments
have no spend effect. Missing base currency or exchange-rate data is returned in
data-quality warnings rather than silently guessed.

## Customer credit control

Credit exposure includes approved customer invoices and subtracts approved
Cashflow payments only. Outstanding amounts remain grouped by original currency,
then convert through the latest effective rate into the customer's preferred
currency. Missing required rates return an error instead of an assumed value.
Null credit limit means unlimited; available credit may be negative.
Legacy customer updates that omit CRM fields preserve the stored credit profile.
An explicit `null` credit limit opts the customer into unlimited credit; currency
remains required whenever the effective credit limit is non-null.

`IsWithinCreditLimit` and the credit-status endpoint are advisory. They are not
wired into existing ProductionOrder creation, so this release does not silently
change the current sales-commitment process.

## Customer quotation maker-checker

1. A quotation starts as `Draft` with a captured currency and one or more
   product/UoM lines. The preferred customer currency is used when configured;
   otherwise the currency is inferred only when every line has one active
   agreement in the same currency. Standing prices are inclusive at both date
   boundaries and only provide defaults; an explicit quotation price may
   override.
2. Sending a valid, unexpired draft delegates stage creation to the central
   approval system. The active stage appears in the assigned user's **My
   Approvals** queue and is reviewed through the generic approval endpoints.
3. The creator cannot approve it. Only the assigned user or assigned-role member
   can act on the active stage. Every action retains reviewer, time, decision,
   and comments.
4. Rejection moves the document to `Rejected`; final approval moves it to
   `Accepted`. Approval transitions use serializable transactions.
5. Expiry is derived when reading a past-due draft or sent quotation; the backend
   does not silently rewrite stored status.
6. The convertible-quotation read model returns only accepted, approved,
   unexpired quotations without an existing order link. It is the source for the
   Production Orders page's customer → quotation picker.
7. Conversion accepts one of those quotations and atomically creates one
   ProductionOrder. Negotiated price, discount, total quantity, packing style,
   and the derived full-shipper/loose split are copied, and the source is marked
   `ConvertedToOrder`.

The resulting ProductionOrder remains in its existing pending fulfillment and
approval flow. Existing orders keep nullable negotiated-price fields and retain
the legacy Product price fallback.

## Customer contacts and performance

Contacts and pricing agreements use soft deletion. A filtered unique index
prevents multiple active primary contacts. Pricing windows cannot overlap for
the same customer/product/UoM, and ambiguous legacy data returns a conflict.

Order history reads the existing ProductionOrder flow. On-time rate includes
only orders with both promised and actual delivery dates; when no comparable
orders exist the rate is null rather than a fabricated zero.
## Governed formula response finalization (2026-09-06)

1. A versioned response resolves stored inputs only against its approved form-revision snapshot.
2. Final submission performs authoritative evaluation, rechecks input fingerprints, writes an
   immutable submission set, and updates the business status inside one serializable transaction.
3. Once that set exists, draft mutation returns `Response.Finalized`; correction must create an
   audited revision rather than alter the submitted record.
4. Approval rounds bind to the latest submission set. Form-response reads project the linked
   stored result so print and COA do not recalculate historical evidence.
5. Unversioned legacy responses remain compatible. A governed field with absent evidence fails
   closed and cannot be represented as an authoritative result.
6. Before ordering or resolving formula-result dependencies, the backend verifies that the
   encoded source reference and declared graph edge identify the same placement and result.
   Disagreement stops the submission before any business status mutation.
7. Table statistics retain every governed observation and calculate with decimal arithmetic.
   Invalid or excessive-scale cells stop evaluation instead of being silently excluded.
8. A replacement formula/template becomes effective only while atomically retiring the prior
   Approved revision. Both changes are audited, and filtered unique indexes are the concurrency
   backstop against multiple effective revisions.

## Global approval progression gate (2026-09-08)

1. Each configurable approval document implements the shared approval contract and starts with `Approved=false` when one or more configured stages exist.
2. The active responsible stage appears in My Pending for its assigned user or role. Until every required stage is authorized, downstream repository operations fail before mutation.
3. Guards cover procurement sends and payments, requisition sourcing/issue, inventory adjustments and transfers, production allocation/dispatch/delivery, shipment distribution, job execution, and R&D status/formulation/trial work.
4. Missing or zero-stage configurations auto-approve and record the system decision; this is the only non-manual authorization path and includes payroll runs.
5. Final approval is performed by `ApprovalRepository`, which enforces responsible-user/role assignment and records approval actions. Domain-specific or legacy endpoints cannot grant approval directly.
6. Stock issue uses `Requisition.ApprovalRequired` and checks before all warehouse and inventory work; other shared guards return `Approval.Required`.

## Formula Draft to effective revision (2026-09-10)

1. A governed create/save transaction writes question metadata plus a Draft revision and preserves
   all legacy option rows. The Draft holds a canonical definition, test corpus, exact authoring
   payload, and independent hashes.
2. Save requests an authoritative validation after commit and returns the outcomes for immediate
   display. Unavailable validation never makes a Draft effective.
3. Submit for review reruns the calculation suite and accepts only an exact definition-hash match
   with all required case categories passing.
4. A different reviewer records review and a third actor approves. Approval atomically retires the
   preceding Approved revision and records both audit transitions.
5. Generic question APIs reject formula writes. Existing active forms continue using their legacy
   payload or existing approved snapshot until a controlled form-revision publication selects the
   replacement; completed response evidence is never rewritten.
6. Release order is formula worker, additive database migration, backend API, then frontend. The
   backend deployment refuses to start formula runtime unless the internal worker is healthy and
   the shared mounted secret exists.
7. Release regression tests use relative agreement validity windows; a historical wall-clock date
   must not make otherwise valid customer-pricing workflow tests fail after that date passes.
8. Every deployment workflow generates migrations with `dotnet-ef` 9.0.3, matching the EF Core
   runtime packages; the pipeline does not use an older major-version migration tool.
## QC checklist report scope (2026-09-14)

1. Resolve the authenticated user's persisted department and department type.
2. Restrict a production user to that production department, regardless of client UI.
3. For a non-production or R&D user, use all production departments by default or the
   selected active production department.
4. Apply optional material-kind and inclusive timestamp filters to pending receipts.
5. Return an empty report when no receipt matches; warehouse configuration is not an
   error for consolidated QC reporting.

## QC worksheet draft edit recovery (2026-09-22)

When editing a Draft worksheet template, fields are matched by their worksheet-wide keys so
their identities and revision histories survive edits. If older overlapping saves left duplicate
stored keys, the edit keeps the oldest field and its history and removes the duplicate rows.
Incoming duplicate keys remain invalid; this recovery applies only to already-stored draft data.

## My Pending Approvals (2026-09-27)

The legacy approval stage lookup considers every role held by the reviewer and refuses inactive stages. Completing a stage activates the next configured stage regardless of which user or role is assigned to it.

The authenticated approver sees only active pending stages. Staff requisitions and production orders now advance through assigned stages on approve or reject and record the actor, time, decision, and comments in `ApprovalActionLog`. Rejected requests do not activate later stages. The dedicated My Pending Approval detail route and generic approval actions verify assignment before returning or mutating a document. QC worksheet instances and OOS cases are listed in the QC queue and open their specialized detail pages. Missing or empty-stage approval configurations still produce an audited system approval for non-QC documents. Controlled QC documents require an explicit configured signer and reauthentication.

## Full Procedures authoring scope (2026-09-28)

All five template kinds now apply the catalog's purpose/subject and allowed-kind
rules on draft creation. Invalid combinations cannot enter the Draft → In Review
→ Published lifecycle. Existing approved records and legacy Routes are not
rewritten. See `docs/full-procedures-template-scope-2026-09-28.md`.

Calculation Question authoring now validates the exact Published source
revision selected by the caller. A superseded source cannot silently change
the new Question's calculation snapshot. Existing ID-only callers retain the
previous lookup behavior until migrated.

Documentation updated for this task: `README.md`, `docs/services.md`,
`docs/workflows.md`, and `docs/approval-unification-design.md` describe
the endpoint, fallback behavior, and deployment limits.

My Pending Approvals selects one ApprovalDocument tab at a time. The backend
list methods avoid materializing other document types; a separate actor-scoped
count response supplies tab badges. Documentation updated for this filter
change: `docs/services.md` and `docs/workflows.md`.
