# Finished goods: transfer through delivery and recall

Authorised scope (21 November 2026): implement the complete discussed workflow,
starting at FGTN generation. Preserve Entrance's customer-order operation and
allow loading while commercial paperwork/waybill preparation continues. Reuse
existing desktop components, FormWizard, tables, approvals and print primitives.
No return to upstream production redesign. No deployment is implied.

## Operating rules

- Generating an FGTN records an intended transfer, not a confirmed warehouse receipt.
- Receipt records the actual quantity, receiver and time; shortages require a reason.
  Original sent quantities remain immutable. Transfer approval and store acceptance
  are distinct. Configured approval stages remain authoritative for release.
- Received/released stock is tracked by product, batch, packing style and location.
  Stock is held in base units; packs and shippers always follow the current packing
  style until dispatch, which pins the layout (see `finished-goods-live-packing.md`;
  this replaces the earlier "preserve the original conversion" rule). No expired, recalled or held batch may be
  newly allocated, loaded or dispatched.
- A packing list reserves stock; loading retains custody on site. Neither event
  means dispatch. Exit requires verified commercial documents and a waybill.
- Proforma -> customer PO verification -> issued invoice -> linked waybill ->
  dispatch -> evidenced delivery. Loading can occur in parallel after allocation
  approval. Document uploads alone must never issue an invoice.
- Keep physical on-site, reserved, available, dispatched/in-transit, delivered and
  held stock distinct. Adjustments must affect the same balances and audit ledger.
  Reject duplicate posting, stale writes and over-allocation.
- QA owns quality/release/recall disposition; costing owns approved pack prices
  and cost information; finance owns invoice/commercial clearance; FG/logistics
  owns counting, custody, transport and delivery evidence. Do not invent a cost
  or an exchange rate where none has been recorded.
- Recall locates every affected batch in storage, loading, transit and deliveries,
  blocks further issue, tracks notifications, recoveries and effectiveness, and
  keeps returns quarantined until authorised disposition. Financial settlement is
  separate from physical recovery. Regulatory classification is a human decision.

## Benchmark

WHO TRS 1025 Annex 7, especially sections 9, 10, 13, 17 and 18:
https://cdn.who.int/media/docs/default-source/medicines/norms-and-standards/guidelines/distribution/trs1025-annex7-%281%29.pdf

Ghana FDA Product Recall Guideline FDA/OPS/GDL-02/02 (effective 7 August 2023):
https://fdaghana.gov.gh/wp-content/uploads/2024/09/02-GUIDELINES-FOR-PRODUCT-RECALLdocx-1.pdf

These are process/control benchmarks, not a certification of this application.
The customer's commercial sequence is retained; the standards do not prescribe
that exact invoice sequence. QA must validate operating procedures and release
stage configuration before rollout.

## Implementation and validation log

| Workstream | Status | Evidence / checks |
| --- | --- | --- |
| FGTN creation, receipt, discrepancy and immutable sent count | Implemented | Cumulative final-packing capacity, actual counted receipt, zero/short receipt evidence, saved packing conversion |
| Consistent stock balances, adjustments and all-batch summaries | Implemented | Canonical received-stock balances; one adjustment posting; reserved-stock protection; no per-row stock requests |
| Reservation, loading, departure and delivery | Implemented | Serializable writes; loading before paperwork; evidenced departure; exact delivery reconciliation |
| Structured PO, invoice and waybill links | Implemented | Verified saved pack prices/currencies; actual invoices; void/reissue history; issued-invoice reporting and printing |
| Expiry, holds, recall, recovery and disposition | Implemented | Batch-wide hold, trace, authority decision, contacts, quarantine, disposition, close/reopen and print |
| Permissions, schema/data compatibility and generated contracts | Implemented | Specific endpoint policies, fresh write checks, catalog, grant migration, scoped generated contract |
| Automated and PostgreSQL verification; operator documentation | Validation recorded below | Disposable PostgreSQL migrations/concurrency, endpoint policy matrix, UI controls, contract regression, builds and visual checks |

Historical data needs an explicit, auditable reconciliation. Do not infer customer
delivery, QA release or missing PO evidence from legacy status names. Retain
historical document references and surface unresolved records for review.

### Working implementation notes

- Generation no longer posts a movement, distributed-product arrival or receipt bin card.
  Actual acceptance posts the counted receipt, receiver, timestamp and discrepancy reason.
- Quantity sent remains immutable; approved adjustments use a separate received-stock delta.
- Allocation/loading/departure recheck QC release (Approved or Available), holds and expiry.
  FEFO exceptions require a recorded justification. Departure enforces customer authorisation
  evidence, remaining shelf life, commercial clearance and linked invoice/waybill.
- Loading keeps stock on site; dispatch posts an issue once. Delivery reconciles accepted,
  damaged and short quantities with receiver and proof reference. Cancellation releases
  reservations after documented unloading; unpaid undeparted invoices can be explicitly voided.
- Customer PO attachment is evidence only. Verification matches each saved pack price,
  quantity and currency. Issuance produces the actual invoice, not a relabelled upload.
- Recalls place every transfer in the batch on hold, identify consignees/vehicles, capture
  notifications and effectiveness, quarantine returns, and require QA destruction evidence.
  Closure accounts for any unrecoverable quantity explicitly and does not release stock for sale.
- Existing stock must be marked for reconciliation by the schema migration. It is blocked
  from new progression until an authorised operator records actual counts, conversion and
  departure evidence. No old delivery/loaded flag is silently converted into departure proof.
- Migration application belongs to the user's build/deployment pipeline (confirmed in chat).
  Only disposable test databases may be migrated by the agent for validation.


## Operator flow

1. **Production generates the FGTN** against the final packing record. The saved
   product/packing style belongs to the batch's product; cumulative transfers cannot
   exceed the final packed yield. Generation records the intended transfer only.
2. **Approve the transfer using the existing approval workflow**, then **Receive**
   in the FG store. Count shippers and complete packs using the saved conversion.
   Enter the discrepancy reference for a short or zero receipt. The receiver,
   time, actual count and original sent count are retained separately.
3. **QA release and stock eligibility:** stock becomes available only after receipt,
   transfer approval, released BMR status (Approved/Available), a future expiry,
   completed historical reconciliation and no hold. A physical receipt can exist
   while quality progression is blocked. Do not equate store acceptance with QA release.
4. **Generate and approve the packing list.** It reserves exact batches and packing
   styles against the customer's remaining order quantity. FEFO is checked on the
   server; taking later-expiring stock while earlier eligible stock remains needs
   an explicit exception reason. An unapproved packing list can still be corrected
   through the existing review workflow.
5. **Prepare in parallel:** FG can load a suitable truck and record the driver,
   contact, inspection and bay while commercial paperwork continues. Finance sends
   the proforma through the customer's existing communication process, attaches the
   customer's PO, verifies every line's quantity, saved pack price and currency,
   then completes the configured proforma approval before issuing the actual invoice.
   The UI's sent action records the event; it does not send an email automatically.
6. **Costing confirms the approved manufacturing cost per complete pack**, its
   currency and approved batch/valuation reference for each product/packing style.
   This is a saved consignment cost basis, not an automatic manufacturing-cost
   calculation and not the selling price. Finance separately records the approved
   payment/credit basis, customer authorisation/licence reference and minimum
   remaining shelf life. These controls can be completed independently of loading.
7. **Create the waybill** against that actual invoice. Record carrier, actual delivery
   address, driver/contact, vehicle, storage/transport conditions and seal where used.
   The loaded vehicle and waybill vehicle must match. Print the waybill using the
   existing preview and print flow; a prepared document does not itself prove departure.
8. **Record departure** only when loading, costing, commercial clearance and the
   linked invoice/PO/waybill are complete. Recheck release, hold, expiry, shelf life,
   vehicle suitability and exact reserved/physical stock. Record the gate reference.
   This is the stock-issue event. Repeating it cannot issue the stock twice.
9. **Record delivery:** capture the customer receiver, signed POD reference, product
   and transport condition, and each batch's accepted, damaged and short quantities.
   Their sum must exactly equal the dispatched quantity. Enter fractional pack
   equivalents where a damaged quantity is smaller than a complete pack. Delivery
   never creates warehouse stock for short or damaged goods automatically.

### Counts and document corrections

- All stock arithmetic uses the product's packing **base-unit count**, not its dosage
  volume (a 100 ml bottle is one bottle). Units/pack, packs/shipper and the style name
  are saved when the transfer is created; later master-data edits cannot reinterpret it.
- Stock basis = actual received + approved adjustment + any explicitly released
  returned stock. Physical on-site = stock basis - departed + quarantined returns.
  Reserved = allocated - departed, bounded at zero. Available excludes holds,
  expiry, missing release and reconciliation blocks, and then subtracts allocations.
  Quarantined returns are never saleable stock in this workflow.
- Loading keeps stock physically on site and reserved. It never posts a receipt,
  sale or warehouse issue. Dispatch posts the issue, bin card, ledger and movement
  together. Stock adjustments update the same basis and preserve the sent count.
  A stale count or adjustment consuming a customer reservation is refused.
- Proforma, actual invoice and waybill are separate documents. Uploading a file
  does not create an invoice. Issued invoices use the verified proforma's frozen
  pack amounts and currencies; no later price list or exchange rate changes them.
- Sent/verified proformas and issued invoices cannot be overwritten or deleted.
  Finance may void an unpaid, undeparted invoice with evidence. Its old waybill
  becomes invalid, commercial clearance is reset, and Finance can issue a replacement
  invoice and waybill. Both invoice records remain in history. Voided invoices are
  excluded from payment, credit exposure and invoiced-product report totals.
- Cancel an undeparted packing list only after recording unloading/count-back
  evidence and voiding any live invoice. Cancellation releases its reservations
  and rebuilds the order's packed quantities; it does not issue stock.
- The old quantity-only FGTN creation and status-only loaded/delivered mutations
  refuse progression and direct callers to the evidenced tasks. Deploy the desktop
  and API together so users have the new forms.

## Recall and Finance responsibilities

**QA** initiates the batch recall immediately, even while an authority classification
or notification is pending. Every existing transfer is held and later transfers
inherit the hold. Record the authority's classification/reference using **Record
 authority decision**; prior values and evidence remain in the decision history.
Classification is never assigned automatically by the application.

The recall lists stock on site and all reserved, loaded, in-transit, delivered and
cancelled consignments with batch, packing style, consignee/contact and transport
references. Historical customer records remain traceable. Record recipient
notifications, acknowledgements and documented effectiveness/follow-up outcomes.
Recoveries identify the original consignment and transfer note, cannot exceed its
unrecovered dispatched quantity, and require a unique receipt reference, condition
and quarantine evidence. Recovery increases quarantined on-site stock only.

**QA disposition** destroys quarantined recoveries through their own recorded task.
Disposing original held stock requires cancellation/unloading of its reservations
first. Closure requires received/reconciled transfers, completed consignment
follow-up, disposition of recoveries, no stock left on site, and exact accounting
of dispatched quantities as recovered or explicitly unrecoverable with a justified
final report. Closure never releases recalled stock for resale. Late evidence or
newly found goods require **Reopen recall**, preserving prior closure reports.

**Finance** separately records the approved external credit note, refund or
replacement reference against the affected issued invoice and original currency.
Replacement resolutions link an approved order for the same customer/product.
These records link financial evidence to the recall; they **do not post a second
credit/refund into receivables, create inventory, or ship a replacement**. Complete
those commercial/accounting actions through the organisation's approved Finance
process. An amount is bounded by the original invoiced amount for that resolution
type; currencies are not combined or silently converted. Financial evidence can be
recorded after physical recall closure without changing its stock reconciliation.

## Migrations and rollout

The user/pipeline applies migrations after the build. No application database was
migrated during this work. The new FG migrations, in order, are:

1. `20261121154213_HardenFinishedGoodsWorkflow`: stock/receipt/transport/document/
   recall fields and tables. **Every existing FGTN is marked as requiring stock
   reconciliation.** Original quantities and document evidence are preserved;
   old loaded/delivered status names are not inferred to be departure evidence.
2. `20261121154214_GrantFinishedGoodsWorkflowTasks`: data-only preservation of
   existing task grants, listed below. Down does not erase later administrator grants.
3. `20261121185126_AddFinishedGoodsCostBasisAndRecallSettlements`: saved approved
   manufacturing cost bases and external recall Finance resolutions.

The earlier costing migrations remain predecessors. Back up and use the normal
pipeline migration process. Before operational rollout, configure the existing
transfer/packing-list/proforma approval and review stages, assign the new sensitive
permissions, and have QA/FG/Finance validate their operating procedures.

### Historical reconciliation

An authorised reconciler opens the received transfer note's **Reconcile stock**
form, verifies its physical on-site count, base units per pack, complete packs per
shipper and packing style name, and cites signed count/reconciliation evidence.
The form lists linked allocations. Confirm each actual historical departure with
its real date and evidence; old delivery flags alone are insufficient. Resolve
cancelled lists and count goods back as appropriate before entering available stock.
The server rebuilds allocations/order counters and appends an explicit bin-card
baseline correction. It preserves sent/received evidence and refuses a count that
cannot cover still-reserved goods. Historical stock stays blocked until this is done.
Do not bulk-clear the reconciliation flag or manufacture departure/PO evidence.

## Permission and endpoint map

Each new action has one named backend policy; fresh grants are loaded immediately
before writes inside the stock transaction. UI visibility and FormWizard submission
use the same task keys. Existing permissions continue to gate transfer receipt,
packing-list creation, loading, delivery, proforma generation/sending, actual invoice
creation/viewing and waybill creation.

| Preserved task key | Migration source (same access types) |
| --- | --- |
| `CanVerifyFinishedGoodsCustomerPo` | `CanSendProformaInvoice` |
| `CanDispatchFinishedGoods` | `CanDeliverFinishedGoods` |
| `CanPrintFinishedGoodsWaybill` | `CanViewWaybillForFgw` |
| `CanApplyFinishedGoodsStockAdjustment` | `CanCreateMaterialStockAdjustment`, `CanApprovePendingApproval` |
| `CanViewApprovedProducts` | `CanViewFgtn` |
| `CanViewInvoice` | `CanViewPayments`, `CanViewCashflowReports` (previous issued-invoice list guards) |

New clearance, costing confirmation, hold/release, reconciliation, cancellation,
invoice void, and recall/authority/settlement powers are **granted to nobody** by
these migrations. Administrators assign them explicitly to the correct QA, FG,
Costing or Finance responsibilities. Permission names alone never bypass stock,
record state, approval, customer or source-document checks.

| API task (under `/api/v1`) | Named permission |
| --- | --- |
| `POST /finished-goods/proformas/{id:guid}/verify-po` | `CanVerifyFinishedGoodsCustomerPo` |
| `POST /finished-goods/allocations/{id:guid}/clearance` | `CanClearFinishedGoodsDispatch` |
| `POST /finished-goods/allocations/{id:guid}/dispatch` | `CanDispatchFinishedGoods` |
| `POST /finished-goods/allocations/{id:guid}/delivery` | `CanDeliverFinishedGoods` |
| `POST /finished-goods/transfers/{id:guid}/hold` | `CanHoldFinishedGoods` |
| `POST /finished-goods/transfers/{id:guid}/release-hold` | `CanReleaseFinishedGoodsHold` |
| `GET /finished-goods/recalls` | `CanViewProductRecalls` |
| `GET /finished-goods/recalls/{id:guid}` | `CanViewProductRecalls` |
| `GET /finished-goods/recalls/{id:guid}/print` | `CanPrintProductRecall` |
| `POST /finished-goods/recalls` | `CanCreateProductRecall` |
| `POST /finished-goods/recalls/{id:guid}/contacts` | `CanNotifyProductRecall` |
| `POST /finished-goods/recalls/{id:guid}/recoveries` | `CanRecordProductRecallRecovery` |
| `POST /finished-goods/recalls/{id:guid}/recoveries/{recoveryId:guid}/dispose` | `CanDisposeProductRecallRecovery` |
| `POST /finished-goods/recalls/{id:guid}/close` | `CanCloseProductRecall` |
| `POST /finished-goods/invoices/{id:guid}/void` | `CanVoidFinishedGoodsInvoice` |
| `POST /finished-goods/allocations/{id:guid}/cancel` | `CanCancelFinishedGoodsAllocation` |
| `POST /finished-goods/recalls/{id:guid}/dispose-stock` | `CanDisposeRecalledFinishedGoodsStock` |
| `POST /finished-goods/transfers/{id:guid}/reconcile` | `CanReconcileFinishedGoodsStock` |
| `POST /finished-goods/allocations/{id:guid}/load` | `CanLoadFinishedGoods` |
| `GET /finished-goods/allocations/{id:guid}/documents` | `CanViewInvoice` |
| `GET /finished-goods/recall-batches` | `CanViewProductRecalls` |
| `GET /finished-goods/transfers/{id:guid}/reconciliation` | `CanReconcileFinishedGoodsStock` |
| `POST /finished-goods/allocations/{id:guid}/costing` | `CanConfirmFinishedGoodsCosting` |
| `POST /finished-goods/recalls/{id:guid}/settlements` | `CanRecordProductRecallSettlement` |
| `GET /finished-goods/allocations/{id:guid}/waybill-print` | `CanPrintFinishedGoodsWaybill` |
| `GET /finished-goods/recalls/{id:guid}/settlement-options` | `CanRecordProductRecallSettlement` |
| `GET /finished-goods/recalls/{id:guid}/replacement-orders` | `CanRecordProductRecallSettlement` |
| `POST /finished-goods/recalls/{id:guid}/reopen` | `CanReopenProductRecall` |
| `POST /finished-goods/recalls/{id:guid}/authority-decision` | `CanUpdateProductRecallDecision` |

## Verification record (21 November 2026)

- API/backend compile through `dotnet test tests/APP.Tests` with the finished-goods,
  invoice-pricing, payment/credit, permission-catalog, costing and QA-material filters:
  **122 passed, one PostgreSQL costing test skipped in that combined invocation**.
  That costing test was then run separately with its connection variable: **one passed**.
  The finished-goods PostgreSQL migration/concurrency test ran and passed in the
  combined invocation. Only randomly named disposable databases were migrated.
- PostgreSQL checks execute both new FG schema migrations and the task-grant data
  migration, preserve historical sent quantities, flag legacy stock, verify inherited
  and ungranted sensitive permissions, and race two serializable reservations so
  only one can consume the same stock. Costing separately validates unique/open
  revisions, pack lines, optimistic writes and narrow price queries.
- Endpoint tests check named policy attributes, registration, current-grant allow,
  missing-grant deny and revoked-grant deny. Workflow tests cover actual/zero receipt,
  cumulative final-packed transfer capacity, stock holds/expiry, departure posting
  once, delivery reconciliation, cancellation, historical conversion/count/departure
  reconciliation, adjustment delta/idempotency/reservation protection, recall closure/
  reopening/authority history, invoice void/reissue and live-invoice report semantics.
- `dotnet ef migrations has-pending-model-changes --project INFRASTRUCTURE
  --startup-project API --no-build`: **no pending model changes**.
- Desktop/API generation and route checks passed. Contract regression preserves the
  approved predecessor after removing only the recorded FG contract scope. Repository
  checks: **200 passed, 18 pre-existing platform/optional checks skipped**.
- Focused UI suites covering FG, recalls, transfer notes, approved products, production
  orders, approvals and costing: **82 passed**. The final delivery-evidence/control
  suite was rerun after adding its display assertion: **23 passed**. Forms test precise
  deny/allow controls and loading without a PO/waybill; issued invoices read actual
  invoice endpoints and print frozen values without current-price lookups.
- Desktop production build and TypeScript passed. React Doctor reported **no issues**
  on the changed scope (score 89/100). The existing build still reports large bundle
  chunks and the pre-existing craft-preview static/dynamic import warning. Backend
  restores still report existing AutoMapper package advisories and nullable warnings.
- A temporary headless browser reviewed the real built app with mocked business
  records at 1440px and 390px: packing-list documents/actions, recall details and
  waybill preview. No browser runtime errors were observed. This is interface
  validation; it is not a live customer-data acceptance test or pharma certification.

No application/demo database migrations, deployment, customer messages or regulator
notifications were performed. Migration execution remains with the user's pipeline;
role assignment, historical reconciliation and QA/Finance procedure acceptance are
operational rollout tasks.
