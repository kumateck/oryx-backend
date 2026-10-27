# Stores, services and HR repair — 2026-10-22

The repair is on `desmond-latest` and is not deployed. It closes access and integrity gaps found while porting these screens to Oryx Live.

## Contract changes

New or changed endpoints:

- `GET api/v1/items/choices?store=&searchQuery=&page=&pageSize=` returns paged item choices for a store.
- `GET api/v1/service-proforma-invoices` takes a new `memoPending` filter. It returns approved proformas whose job order has no service memo yet. Memo viewers (`CanViewServicesMemo`) can now read proformas.
- `GET api/v1/employee/me` returns the signed-in account's employee record, linked through `Employee.UserId`. If no record is linked it answers 404 `Employee.NotLinked`.
- `GET api/v1/performance/reviews/mine` lists the reviews the signed-in employee is reviewed in, or reviews.
- `GET api/v1/performance/goals` without `CanViewTeamGoals`:
  - returns only the caller's own goals;
  - also returns the reviewed employee's goals when the caller is the assigned reviewer of a review in that cycle. The request must name both `employeeId` and `cycleId`.

Onboarding, all anonymous, with the token sent in the body:

- `POST api/v1/onboarding/invitation` `{token}`
- `POST api/v1/onboarding/submit` `{token, employee}`
- `POST api/v1/onboarding/photo` `{token, file}`

The anonymous employee and user creation endpoints are removed.

Invitation rules:

- Tokens are stored hashed. They expire and work once.
- The invitation sets the email and the employee type. The staff number is always cleared; HR assigns it.
- Invitation links are built from `Onboarding:BaseUrl` (falling back to the request origin) as `/onboarding/{type}?token=…`. Oryx Next's old links stop working.

Permissions:

- `CanViewFormulaBuilder` is added to the permission catalogue under "R&D Formula Builder".

## Authorization

- Stock requisitions, issue, returns, damage/missing and purchase requisitions check per-store keys on current grants (`StoreWorkflowAccess`).
- Store procurement checks sourcing, trusted-vendor and open-market keys.
- Job requests, orders, executions, quotations, proformas, memos, vendors, providers and services check `ServiceKeys` on current grants. Role names never bypass these checks.
- Actors (issuer, approver, requester) come from the token, never from the request body.

## Integrity rules

Stores:

- Issued quantities are tracked per requisition line and cannot exceed the request.
- Returns are limited to what was issued less earlier returns, and they restore stock.
- Damage edits and deletes post to the ledger.
- Open-market memo lines are no longer doubled.

Service quotations and proformas:

- Quotations close once one is selected. A provider cannot quote twice, and charges must be valid.
- A proforma response may change only its own lines. Prices cannot be negative and quantities must be above zero.

Service memos:

- A job order gets one memo (`ServiceMemo.Exists`).
- The memo must use the quotation the approved proforma was raised against (`Quotation.Mismatch`), and go to that quotation's provider (`ServiceProvider.Mismatch`).
- Amounts cannot be negative, and the expected completion cannot be before the start. Creating and updating a memo both enforce this.
- Only an issued memo can be marked paid.

Services:

- A service cannot be deleted while job requests, job orders or service providers still use it (`Service.InUse`).

## Migrations

- `20261022041713_TrackStockIssueRequisitionLines`
- `20261022042310_LinkItemReturnsToRequisitionLines`
- `20261022044450_LinkEmployeesToUsersAndInvitations`

## Tests

Focused suites:

- `StoreStockRequisitionTests`
- `StoreReturnsAndDamageTests`
- `StoreProcurementIntegrityTests`
- `ServiceWorkflowIntegrityTests` (10)
- `HrOwnershipAndOnboardingTests` (9)

Full suite: 1,303 pass and 57 are skipped. Three tests fail, and they failed before this work: `RndFormulationRepositoryTests.UpdateStatus_allows_draft_to_in_review_to_approved`, `RndTechnologyTransferRepositoryTests.PromoteToProduction_creates_bom_and_stamps_product` and `QcSignatureAcceptanceTests.Approval_engine_refuses_to_auto_approve_qc_model_types`.
