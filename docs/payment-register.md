# Payment register and cashflow currency display

GET /api/v1/payments requires CanViewPayments and returns data, pageIndex, pageCount, totalRecordCount. Filters: page (1+), pageSize (1–100), searchQuery (reference, max 255 characters), status (0 pending, 1 approved, 2 rejected), isReceipt (true customer invoices, false outgoing, omitted all). Results sort by payment date descending, created date descending, then ID. Reads do not modify balances or approvals.

GET /api/v1/payments/currency-configuration?asOf=<UTC timestamp> requires CanViewCashflowReports. Returns baseCurrency (nullable), asOf, and rates with currency, nullable rateToBase and effectiveDate. Each rate is the latest effective on or before asOf; future rates are excluded. The base currency has rate 1 and no effective date. Missing rates and missing base configuration never imply a conversion rate.

Existing recording/review rules remain: a configured Payment workflow reviews stages sequentially, disallows recorder self-review, and rechecks outstanding balance before approval. No workflow or no stages causes automatic approval with an ApprovalActionLog reason. Only approved payments affect balances. Both incoming receipts and outgoing payments use this workflow.

Payment is a configurable approval document. The central pending-approval read
includes a Payment only when its pending stage is active and assigned to the
requesting user or role, and excludes records made by that user. Finance detail
reads use `GET /api/v1/payments/{paymentId}/approval-details` with
`CanApprovePayment`. Every manual approval or rejection writes an
`ApprovalActionLog` in addition to updating the responsible stage.

Validation: PaymentRegisterTests covers pagination, receipt/status/reference filtering, invalid pagination, effective date resolution, missing rates, base identity, and missing base currency. Existing PaymentApprovalTests and PaymentRepositoryTests cover approval and balances.
## Shared references for billing sheet charges (2026-10-25)

The bulk charge payment API accepts the same reference on several distinct
charges. Charge IDs must remain unique and every selected charge must be
unpaid, approved for progression, and free of an active payment. It creates a
separate traceable payment for each charge. Migration
`20261025160000_AllowSharedBillingChargeReference` narrows the unique
`(PayableType, PayableId, Reference)` index to non-billing-sheet payables.
Apply it before enabling the frontend copy control. Rolling it back requires
reconciling any shared billing sheet references first. Documentation updated:
this guide, `docs/services.md`, and `docs/workflows.md`.
