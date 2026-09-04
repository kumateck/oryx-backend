# Workflow behavior

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

1. A quotation starts as `Draft` with a captured customer currency and one or
   more product/UoM lines. Standing prices are inclusive at both date boundaries
   and only provide defaults; an explicit quotation price may override.
2. Sending a valid, unexpired draft copies configured `CustomerQuotation`
   approval stages and activates the first stage.
3. The creator cannot approve it. Only the assigned user or assigned-role member
   can act on the active stage. Every action retains reviewer, time, decision,
   and comments.
4. Rejection moves the document to `Rejected`; final approval moves it to
   `Accepted`. Approval transitions use serializable transactions.
5. Expiry is derived when reading a past-due draft or sent quotation; the backend
   does not silently rewrite stored status.
6. Conversion accepts one unexpired, approved quotation and atomically creates
   one ProductionOrder. Negotiated price, discount, quantity, and UoM are copied,
   and the source is marked `ConvertedToOrder`.

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
