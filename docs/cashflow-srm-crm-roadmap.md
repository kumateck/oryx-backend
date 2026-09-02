# Cashflow, SRM & CRM — Findings and Codex Backend Prompts

## Context

Adding Cashflow visibility, SRM (Supplier Relationship Management), and CRM
(Customer Relationship Management) to this pharma ERP, and bringing the app
toward pharma-industry ERP standards generally — even though there's no full
accounting/GL module and none is being proposed here. Three research passes
mapped the existing `oryx-backend`/`oryx-next` codebase in full before any
design work, so the plan below is grounded in what's actually there, not
assumptions.

Decisions made before drafting the prompts:

1. **Sequencing: Cashflow first.** It's the smallest, most self-contained gap
   (enrich existing documents + one new `Payment` entity + reporting) and it
   unblocks real spend analytics for SRM and real credit-control for CRM
   later. SRM and CRM prompts are written in full now too, but Codex should
   be run on them in this order: **Cashflow → SRM → CRM**.
2. **CRM route collision: rename the existing `/crm` namespace, build CRM
   fresh.** The app already has a `/crm/*` frontend route prefix and
   "Customer Management" nav section (Customers, Invoices, Proforma Invoice,
   Production Order, Allocations) living under a `finishedGoodsWarehouse`
   permission domain. That existing surface moves to a `/finished-goods` (or
   similar) namespace and genuine CRM gets designed fresh. **This is a
   frontend/routing concern** — the backend prompts below don't rename any
   C# classes or controllers on that basis; they just avoid colliding names
   and build the real CRM data model as new entities.
3. **Pharma QMS gaps: included in scope**, but threaded into the three
   module prompts where they naturally belong (supplier GMP certifications
   in SRM, maker-checker/approval rigor in Cashflow) rather than spun up as
   a fourth module right now. The deeper QMS work this surfaced — standalone
   deviation management, formal cross-source CAPA, change control,
   recall/withdrawal tracking, stability/retest scheduling, and real 21 CFR
   Part 11 electronic signatures (today's "signature" is a static stamped
   image, not re-authentication + reason-for-change capture) — is real and
   worth doing, but is a distinct future phase, not part of these three
   prompts.

## Part 1: Findings — Current State & Gaps

### What's already mature

- **Procurement lifecycle is genuinely well-built end to end**:
  `Requisition` (New→Pending→Sourced→Completed) →
  `SourceRequisition`/`SupplierQuotation` (RFQ send → receive → compare →
  award) → `PurchaseOrder` (11-state machine, full revision/audit trail via
  `RevisedPurchaseOrder`) → `ShipmentDocument`
  (New→AtPort→Cleared→InTransit→Arrived) + `ShipmentInvoice` (with dedicated
  `ShipmentDiscrepancy` variance tracking) → `BillingSheet` (freight/demurrage
  charges) → `Grn` (batch intake gate). Real FK linkage at every step,
  multi-stage approvals everywhere via a shared `IRequireApproval` /
  `ResponsibleApprovalStage` pattern.
- **QC/lab and batch-release tooling is mature**: `MaterialBatch`/BMR/BPR
  status machines, `MaterialSpecification`/STP/ARD with maker-checker verify
  steps, `OosInvestigation` (with `RootCauseAnalysis`/`CorrectiveActions`/
  `PreventiveActions` — a mini-CAPA scoped to OOS lab results), and a
  **functional** Certificate of Analysis generator
  (`FormRepository.GenerateCertificateOfAnalysis[ForProduct]`).
- **Audit logging is solid and convention-driven**: every mutating API call
  logs who/when/what/from-where plus request/response payload, via
  `AuditModules` (label dictionary in `oryx-next/src/lib/constants.ts`) +
  header injection (`audit-headers.ts`) + `ActivityLogMiddleware.cs` on the
  backend, stored per-call in MongoDB.
- **Batch genealogy exists**, if implicit: raw `MaterialBatch` → consumed in
  production → `BatchManufacturingRecord` → `FinishedGoodsTransferNote`
  (which explicitly FKs back to the BMR) → customer invoice/allocation. No
  unified lineage-graph API, but the FK chain is real and traceable.

### What's missing everywhere (the actual gaps)

- **No payment/AP tracking at all.** Every "paid" concept in the system is a
  bare boolean (`BillingSheet.Status: New/Pending/Paid`,
  `BillingSheetCharge.Paid`, `ShipmentInvoice.PaidAt`). No partial payments,
  no amount-outstanding, no due-date field anywhere, no AP aging.
  `PurchaseOrder.TermsOfPayment` is a free-text lookup
  (`TermsOfPayment.Name`/`Description`, e.g. "Net 30" as a string) with
  **no day-count that could actually drive a due-date calculation.**
- **No FX/base-currency concept anywhere.** `Currency` is `Name`/`Symbol`/
  `Description` only — no exchange rate, no rate history, no base-currency
  flag. Money is recorded per-transaction in whatever currency was picked at
  entry time; nothing can consolidate multi-currency spend/revenue into one
  reporting currency.
- **`Supplier` is an 8-field stub**: `Name`, `Email`, `Address`,
  `ContactPerson`, `ContactNumber`, `Country`, `Currency`, `Type`
  (Foreign/Local), `Status` (New/Approved/Rejected), plus a manufacturer
  association list. No performance ratings, no formal AVL lifecycle
  (re-qualification/expiry), no certifications (critical for pharma — no
  GMP cert tracking at the supplier level at all), no lead-time field, no
  standing contracts/pricing agreements (pricing only exists as point-in-time
  quotations), no multiple contacts, no bank details.
- **`Customer` is even thinner**: `Name`, `Email`, `Phone`, `Address` only.
  No credit limit, no payment terms, no pricing tier/segment, no
  billing-vs-shipping address distinction. There is no real customer-facing
  Sales Order/Quotation entity — `ProductionOrder` (production-triggering)
  is the de-facto sales order, with pricing baked in as
  `quantity × Product.Price` and no negotiation/quotation step before it.
- **A parallel, largely duplicate procurement subsystem exists**
  (`extrals/*` routes, backend `Vendor`/`VendorQuotation` entities) alongside
  the main `Procurement/Suppliers` module — worth being aware of so SRM
  doesn't add a *third* parallel concept; the prompts below flag this for
  Codex to note but not force a risky unification.
- **Existing procurement KPIs are pure throughput/queue-depth** (counts of
  requisitions/POs/quotations by status) — **no cost, no on-time-delivery %,
  no supplier performance, no spend-by-supplier metric exists today.**

### Conventions any new module must follow (verified from the codebase)

- **Backend**: `DOMAIN/Entities/<Module>/<Entity>.cs` (entity + sibling DTO
  classes) → `APP/IRepository/I<Module>Repository.cs` →
  `APP/Repository/<Module>Repository.cs` →
  `API/Controllers/<Module>Controller.cs` → an EF Core migration. Smaller
  entities that conceptually belong to a bigger domain (e.g. `BillingSheet`
  under Procurement) get folded into that domain's controller/repository
  rather than getting their own — first-class aggregates (e.g. `Customer`)
  get their own controller/repository. Approval workflows implement
  `IRequireApproval` with a `<Entity>Approval : ResponsibleApprovalStage`
  join entity, matching `BillingSheetApproval`/`RequisitionApproval`/etc.
- **Tests**: xUnit in `tests/APP.Tests/Repository/`, using EF Core's
  in-memory provider (`UseInMemoryDatabase`) with a `PipelineIds`-style
  helper class for stable test GUIDs — see `MaterialPipelineQueryTests.cs`
  and `MaterialPipelineBuilderTests.cs` as the exact reference pattern (seed
  entities in a `SeedPipeline`-style helper, assert on repository/builder
  output).
- **Money fields**: `decimal`, paired with a `CurrencyId`/`Currency` FK at
  the line level (never a single header-level currency assumption).

## Part 2: Codex Prompts (Backend Only, `.NET`)

Each prompt below is self-contained — copy one at a time into Codex. They
target this repo (`oryx-backend`) only; no frontend work is requested. Run
them in order: Cashflow, then SRM, then CRM (SRM's supplier-spend reporting
and CRM's credit-control both read from the Cashflow payment data).

---

### Prompt 1 — Cashflow (Payment / AP-AR Visibility)

```
You're working in a .NET pharma ERP backend at this repo root. Read
CLAUDE.md-equivalent conventions by example: look at how
DOMAIN/Entities/PurchaseOrders/BillingSheet.cs, APP/Repository/
ProcurementRepository.cs, and API/Controllers/ProcurementController.cs are
structured, and follow the exact same layering (Entity+DTO in DOMAIN,
IRepository/Repository in APP, Controller in API, EF Core migration).

GOAL: Add real payment/AP-AR tracking. Today every "paid" concept in this
system is a bare boolean (BillingSheet.Status, BillingSheetCharge.Paid,
ShipmentInvoice.PaidAt) with no due dates, no partial payments, no
outstanding-balance concept, and no AP aging. Fix that without building a
full general ledger — this is payment tracking, not double-entry accounting.

1. Structured payment terms: extend DOMAIN/Entities/Base/TermsOfPayment.cs
   (currently just Name + Description strings, e.g. "Net 30" as free text)
   with a numeric DueDays field. Add a migration. Backfill existing rows by
   parsing common patterns from Name (e.g. "Net 30" -> 30, "Net 60" -> 60,
   "Due on Receipt" -> 0) where you can infer it safely, defaulting to null
   otherwise (don't guess wrong on ambiguous names).

2. New Payment entity (DOMAIN/Entities/Payments/Payment.cs, new folder):
   fields: Id, Amount (decimal), CurrencyId -> Currency, PaymentDate,
   Method (enum: BankTransfer, Cheque, Cash, CreditCard, Other), Reference
   (string, e.g. transaction/cheque number), Notes, RecordedById -> User,
   plus a polymorphic link to what was paid: PayableType (enum:
   BillingSheet, ShipmentInvoice, PurchaseOrderInvoice, CustomerInvoice) and
   PayableId (Guid). Do NOT use EF Core TPH/discriminator inheritance for
   this - just the two plain fields (enum + id), resolved at the repository
   layer. This entity needs IRequireApproval (maker-checker on recording a
   payment), matching the ResponsibleApprovalStage pattern used by
   BillingSheetApproval/RequisitionApproval - create PaymentApproval
   accordingly.

3. Computed outstanding-balance support: for each of BillingSheet,
   ShipmentInvoice, and the customer-facing Invoice entity
   (DOMAIN/Entities/Invoices/Invoice.cs), add a DueDate field (nullable
   DateTime, set from TermsOfPayment.DueDays + the document's reference date
   at creation time - for BillingSheet use ExpectedArrivalDate as the base,
   for ShipmentInvoice and customer Invoice use CreatedAt as the base unless
   you find a more appropriate existing date field on that entity - check
   first). Add repository methods (not stored columns) that compute
   AmountPaid (sum of linked Payment.Amount where PayableId matches) and
   OutstandingBalance (document total - AmountPaid) - expose these on the
   read DTOs, don't persist them as columns that could drift out of sync.

4. AP aging report: new endpoint on a new PaymentController (or extend
   ProcurementController if you judge Payment doesn't warrant its own
   controller after seeing how BillingSheet was folded into
   ProcurementController - use your judgment, but Payment spans multiple
   document types so it likely deserves its own controller/repository) -
   GET /api/v1/payments/ap-aging - returns, grouped by Supplier: total
   outstanding, and outstanding split into 0-30 / 31-60 / 61-90 / 90+ day
   buckets based on DueDate vs today. Cover BillingSheet + ShipmentInvoice +
   PurchaseOrderInvoice as the "payable" sources (check
   DOMAIN/Entities/PurchaseOrders/PurchaseOrderInvoice.cs for its shape
   before including it).

5. AR aging report: same shape as #4 but grouped by Customer, sourced from
   the customer Invoice entity. GET /api/v1/payments/ar-aging.

6. Cashflow summary endpoint: GET /api/v1/payments/cashflow-summary -
   returns projected outflows (unpaid BillingSheet/ShipmentInvoice/PO-invoice
   balances bucketed by DueDate, e.g. next 7/30/60/90 days) and projected
   inflows (unpaid customer Invoice balances, same buckets) plus current
   totals. This is a projection/reporting endpoint, not a forecast model -
   keep it simple, sum what's actually outstanding by due-date bucket.

7. Currency/FX groundwork (required for #4-6 to be meaningful across
   currencies): extend DOMAIN/Entities/Currencies/Currency.cs with an
   IsBaseCurrency bool flag (exactly one currency should be markable as
   base - enforce this in the repository, not a DB constraint, since only
   one boolean column exists per row already established in this codebase's
   style). Add a new ExchangeRate entity (DOMAIN/Entities/Currencies/
   ExchangeRate.cs): CurrencyId, RateToBase (decimal), EffectiveDate. Add a
   repository method to resolve the most-recent rate for a currency as of a
   given date. Use this to convert every amount in the aging/cashflow
   endpoints above into the base currency for the totals (report both the
   original-currency amount and the base-currency-converted amount per line,
   don't silently only show one).

8. Register new permission keys following the existing pattern in
   API - check how permissions are declared/checked server-side (look at
   how ProcurementController's actions are decorated, e.g. [Permission("...")]
   or similar attribute - mirror that exactly) for: ViewPayments,
   RecordPayment, ApprovePayment, ViewCashflowReports.

9. Tests: add tests/APP.Tests/Repository/PaymentRepositoryTests.cs following
   the exact style of MaterialPipelineQueryTests.cs (in-memory DbContext,
   PipelineIds-style GUID helper, seed via a SeedX helper method). Cover at
   minimum: AmountPaid/OutstandingBalance computation with zero, partial, and
   full payment; AP aging bucket assignment at boundary dates (29/30/31 days
   overdue); multi-currency conversion in the aging report using a seeded
   ExchangeRate.

Constraints: don't touch any frontend code. Don't modify unrelated existing
migrations. Run `dotnet build` and `dotnet test tests/APP.Tests/APP.Tests.csproj`
before considering this done - all existing tests must still pass alongside
your new ones. Don't add a general ledger, chart of accounts, or double-entry
concept - this is payment tracking and reporting only, explicitly not full
accounting.
```

---

### Prompt 2 — SRM (Supplier Relationship Management)

```
You're working in a .NET pharma ERP backend at this repo root. Follow the
same layering convention as DOMAIN/Entities/PurchaseOrders/BillingSheet.cs
-> APP/Repository/ProcurementRepository.cs -> API/Controllers/
ProcurementController.cs (Entity+DTO in DOMAIN, IRepository/Repository in
APP, Controller in API, EF Core migration per change).

GOAL: The existing Supplier entity (DOMAIN/Entities/Procurement/Suppliers/
Supplier.cs) is an 8-field stub: Name, Email, Address, ContactPerson,
ContactNumber, CountryId, CurrencyId, Type (Foreign/Local), Status
(New/Approved/Rejected), plus a manufacturer-association list. Build it out
into a real SRM data model. Note: there is a SEPARATE, parallel Vendor
entity (DOMAIN/Entities/Vendors/Vendor.cs) used by a different subsystem
(extrals/* routes, VendorQuotation) - do not touch Vendor, do not merge it
with Supplier, and do not duplicate its concepts pointlessly - just be aware
it exists so you don't reinvent something already named differently there.
Leave a short comment near the Supplier class noting Vendor's existence and
that consolidating them is a deliberate future decision, not something to
do here.

1. Supplier certifications (this is the highest-priority pharma-specific
   gap - there is currently ZERO GMP/compliance-document tracking at the
   supplier level): new child entity SupplierCertification
   (DOMAIN/Entities/Procurement/Suppliers/SupplierCertification.cs):
   SupplierId, CertificationType (string or enum - check if a
   CertificationType lookup already exists anywhere in DOMAIN before adding
   a new enum; if pharma-specific types like GMP/ISO9001/ISO13485/WHO-PQ
   aren't modeled anywhere, add a simple enum with those plus Other),
   CertificateNumber, IssuingBody, IssueDate, ExpiryDate,
  AttachmentId (link to the existing Attachments entity - check
   DOMAIN/Entities/Attachments/ for the exact FK pattern other entities use
   to attach documents, e.g. how MaterialBatch or Checklist attach files,
   and mirror it exactly). Add a repository method to list suppliers with
   certifications expiring within N days (for a future expiry-alert use) and
   expose it on SupplierController.

2. Supplier contacts: replace the single ContactPerson/ContactNumber/Email
   string fields' limitation by ADDING (don't remove the existing fields -
   they may be relied on elsewhere, check call sites first with a repo-wide
   search) a new child entity SupplierContact: SupplierId, Name, Role
   (e.g. Sales, Quality, Finance, Logistics - free string is fine), Email,
   Phone, IsPrimary (bool). Migration + repository CRUD methods
   (add/update/remove contact, list contacts for a supplier).

3. Supplier banking details: new child entity SupplierBankDetail:
   SupplierId, BankName, AccountNumber, AccountName, SwiftCode/IBAN
   (nullable, not every country uses these), BranchAddress, CurrencyId.
   A supplier can have more than one (e.g. accounts in different
   currencies) - model as a list, not a single embedded set of fields.

4. Standing pricing agreements: new entity SupplierPricingAgreement:
   SupplierId, MaterialId, UoMId, AgreedPrice (decimal), PriceUoM,
   CurrencyId, EffectiveFrom, EffectiveTo (nullable = open-ended), Notes.
   This is distinct from point-in-time SupplierQuotationItem pricing (check
   DOMAIN/Entities/Requisitions/SourceRequisition.cs for that entity before
   building this, so you don't duplicate its shape) - a pricing agreement is
   a standing rate a buyer can reference, not a one-off quote. Add a
   repository method to fetch the currently-active agreement for a
   Supplier+Material+UoM as of a given date (there should be at most one
   active agreement per Supplier+Material+UoM+date - the repository method
   should pick the one with the latest EffectiveFrom that's still within its
   EffectiveTo window, and log/return null if it finds more than one
   ambiguous match rather than guessing).

5. Approved-vendor-list (AVL) lifecycle: extend Supplier's existing Status
   enum handling - currently New/Approved/Rejected is a one-time flag.
   Add ApprovedAt (DateTime?) and RequalificationDueDate (DateTime?, e.g.
   Approved suppliers should be periodically re-qualified - default this to
   ApprovedAt + 1 year on approval, but make the interval configurable via a
   parameter rather than hardcoding 365 everywhere). Add a repository method
   to list suppliers whose RequalificationDueDate has passed or is within N
   days (mirrors the certification-expiry method in #1 - consider a shared
   helper if the logic is identical).

6. Supplier performance scorecard: new entity SupplierPerformanceRecord
   (one row per evaluation period, e.g. monthly/quarterly - don't assume a
   period length, just store PeriodStart/PeriodEnd): SupplierId,
   PeriodStart, PeriodEnd, OnTimeDeliveryRate (decimal, computed from
   comparing ShipmentDocument.ArrivedAt against the linked PurchaseOrder's
   ExpectedDeliveryDate - check DOMAIN/Entities/Shipments/ShipmentDocument.cs
   and PurchaseOrders/PurchaseOrder.cs for the exact field names and FK
   path), QualityRejectRate (decimal, computed from GRN/QC rejection data -
   check DOMAIN/Entities/Grns/Grn.cs and how MaterialBatch status
   Rejected/Retest is tracked for the right source data), overall Score
   (decimal 0-100, weighted combination - make the weights parameters, not
   hardcoded constants). Add a repository method
   ComputeSupplierPerformance(supplierId, periodStart, periodEnd) that
   calculates this from existing Shipment/GRN/Batch data rather than
   requiring manual entry, and a method to persist the computed result as a
   SupplierPerformanceRecord row (so history is preserved even as the
   underlying source data changes later).

7. Spend analytics (depends on Prompt 1's Payment entity - implement this
   step only if Payment/BillingSheet-outstanding-balance work from the
   Cashflow prompt is already in the codebase; if not, compute spend from
   PurchaseOrder.TotalFobValue/TotalCifValue instead and note in a comment
   that it should be revisited to use actual payment amounts once Payment
   exists): endpoint on SupplierController - GET
   /api/v1/suppliers/{id}/spend-summary - total spend over a date range,
   broken down by month, converted to base currency if the ExchangeRate
   entity from Prompt 1 exists (check DOMAIN/Entities/Currencies/ first;
   if it doesn't exist yet, report spend in each transaction's original
   currency, grouped by currency, and leave a comment that base-currency
   conversion needs the Cashflow prompt's ExchangeRate entity).

8. Extend SupplierController with endpoints for all of the above (certifications
   CRUD, contacts CRUD, bank details CRUD, pricing agreements CRUD,
   performance record retrieval/computation, spend summary). Register new
   permission keys following the existing attribute pattern used on
   SupplierController's current actions: ViewSupplierCertifications,
   ManageSupplierCertifications, ViewSupplierPerformance,
   ManageSupplierContracts, ViewSupplierSpend.

9. Tests: tests/APP.Tests/Repository/SupplierRepositoryTests.cs (or extend
   an existing one if a SupplierRepository test file already exists - check
   first) following the MaterialPipelineQueryTests.cs pattern. Cover: active
   pricing-agreement resolution at a boundary date, certification-expiry
   listing at boundary dates, on-time-delivery-rate computation with a mix
   of on-time/late shipments, AVL requalification-due listing.

Constraints: don't touch any frontend code. Don't modify the Vendor entity
or its subsystem. Run `dotnet build` and `dotnet test
tests/APP.Tests/APP.Tests.csproj` before considering this done - all
existing tests must still pass alongside your new ones.
```

---

### Prompt 3 — CRM (Customer Relationship Management)

```
You're working in a .NET pharma ERP backend at this repo root. Follow the
same layering convention as DOMAIN/Entities/PurchaseOrders/BillingSheet.cs
-> APP/Repository/ProcurementRepository.cs -> API/Controllers/
ProcurementController.cs (Entity+DTO in DOMAIN, IRepository/Repository in
APP, Controller in API, EF Core migration per change), and how Customer
already has its own first-class CustomerController.cs/CustomerRepository.cs
(check those files first - this prompt extends that existing module, it
does not replace it).

GOAL: DOMAIN/Entities/Customers/Customer.cs is currently a 4-field stub
(Name, Email, Phone, Address). There is no real customer-facing Sales
Order/Quotation flow - DOMAIN/Entities/ProductionOrders/ProductionOrder.cs
acts as the de-facto sales order today (CustomerId + line items with
Quantity x Product.Price), but has no pre-order quotation/negotiation step,
and Customer has no credit limit, payment terms, or pricing tier. Build
this into a real CRM data model WITHOUT breaking the existing
ProductionOrder-driven fulfillment flow that Production/Warehouse/QA
already depend on - enrich it, don't replace it. Note: the frontend team is
separately renaming the existing /crm route namespace to make room for a
new CRM UI - that's a frontend-only concern, don't rename any C# classes,
namespaces, or DB tables here on that basis; just don't introduce backend
names that collide with what already exists (Customer, Invoice,
ProformaInvoice, ProductionOrder all stay as-is).

1. Enrich Customer: add CreditLimit (decimal?, nullable = no limit
   enforced), TermsOfPaymentId -> TermsOfPayment (reuse the same
   TermsOfPayment entity suppliers use - check DOMAIN/Entities/Base/
   TermsOfPayment.cs; if Prompt 1's DueDays field exists, this gives
   customers a due-date basis too), CustomerType/Segment (string or enum -
   e.g. Distributor/Pharmacy/Hospital/Retail - check if anything like this
   is implied anywhere in existing Customer-adjacent code before inventing
   values), CurrencyId -> Currency (preferred billing currency), and a
   BillingAddress/ShippingAddress distinction (currently just one Address
   field - add the two new fields, keep the old Address field for backward
   compatibility but have new code prefer the new ones, don't drop the old
   column in this pass).

2. Multiple customer contacts: new child entity CustomerContact (mirror
   whatever shape you use for SupplierContact if Prompt 2 already ran in
   this codebase - check DOMAIN/Entities/Procurement/Suppliers/ for it
   first and match its field names for consistency; otherwise use Name,
   Role, Email, Phone, IsPrimary): CustomerId, Name, Role, Email, Phone,
   IsPrimary.

3. Credit control: add a repository method
   GetAvailableCredit(customerId) that returns CreditLimit minus
   currently-outstanding customer invoice balances (this depends on Prompt
   1's Payment/OutstandingBalance work - if it's not in the codebase yet,
   compute outstanding as the sum of unpaid Invoice amounts using whatever
   "Approved but not yet marked paid" signal currently exists on
   DOMAIN/Entities/Invoices/Invoice.cs, and leave a comment that this should
   be replaced with real OutstandingBalance once Payment exists). Add a
   method IsWithinCreditLimit(customerId, additionalOrderValue) intended to
   be called before confirming a new ProductionOrder/sales commitment - do
   NOT wire this into ProductionOrder's existing creation flow automatically
   (that's a business-process decision outside this prompt's scope) - just
   expose the check as a callable repository method and a
   GET /api/v1/customers/{id}/credit-status endpoint.

4. Customer sales quotation (the missing pre-order step): new entity
   CustomerQuotation (DOMAIN/Entities/Customers/CustomerQuotation.cs):
   CustomerId, Code, Status (enum: Draft, Sent, Accepted, Rejected,
   Expired, ConvertedToOrder), ValidUntil (DateTime), Items
   (CustomerQuotationItem: ProductId, Quantity, UoMId, UnitPrice,
   DiscountPercent (decimal, default 0)). This should implement
   IRequireApproval matching the existing ResponsibleApprovalStage pattern
   (CustomerQuotationApproval) since every other document-approval flow in
   this codebase does. Add a repository method
   ConvertQuotationToProductionOrder(quotationId) that creates a
   ProductionOrder from an Accepted quotation, carrying over the negotiated
   UnitPrice/DiscountPercent onto the resulting order lines (check
   ProductionOrder.cs and ProductionOrderProducts for the exact fields it
   expects - TotalOrderQuantity, VolumePerPiece, TotalValue - and populate
   them correctly, applying the discount to TotalValue). Mark the source
   CustomerQuotation as ConvertedToOrder and link it (add a
   SourceCustomerQuotationId nullable FK on ProductionOrder pointing back).

5. Customer pricing tiers: new entity CustomerPricingAgreement, same shape
   as Prompt 2's SupplierPricingAgreement (CustomerId, ProductId, UoMId,
   AgreedPrice, CurrencyId, EffectiveFrom, EffectiveTo, Notes) - a standing
   negotiated price a customer gets regardless of quotation, checked when
   building CustomerQuotationItem.UnitPrice defaults (if an active agreement
   exists for that Customer+Product+UoM+date, default UnitPrice to it, but
   still let the quotation item override it - agreements set the default,
   not a hard floor/ceiling, unless you find evidence elsewhere in this
   codebase that pricing should be enforced rather than defaulted, in which
   case mirror that behavior instead).

6. Customer order history & performance: endpoint
   GET /api/v1/customers/{id}/order-history - paginated list of that
   customer's ProductionOrders with status, value, delivery date;
   GET /api/v1/customers/{id}/summary - lifetime order count, total value,
   average order value, on-time-delivery rate FROM the customer's
   perspective (their orders' actual delivery vs promised delivery - check
   ProductionOrder.DeliveredAt and whatever "promised" date field exists on
   it), and current outstanding balance (reuse #3's outstanding computation).

7. Register new permission keys following the existing attribute pattern on
   CustomerController's current actions: ViewCustomerCreditStatus,
   ManageCustomerContracts, ViewCustomerQuotations, CreateCustomerQuotation,
   ApproveCustomerQuotation, ConvertCustomerQuotation.

8. Tests: tests/APP.Tests/Repository/CustomerRepositoryTests.cs (extend if
   one already exists) following the MaterialPipelineQueryTests.cs pattern.
   Cover: credit-limit availability computation with zero and partial
   outstanding balance, quotation-to-order conversion carrying over
   negotiated price/discount correctly, active pricing-agreement resolution
   at a boundary date (mirror Prompt 2's equivalent test), quotation
   expiring (ValidUntil in the past) being excluded from "active" quotation
   listings if you add such a listing method.

Constraints: don't touch any frontend code. Don't rename or move
Customer/Invoice/ProformaInvoice/ProductionOrder - only add to them. Don't
automatically enforce credit limits inside ProductionOrder creation - expose
the check, don't wire the block-on-exceed behavior without a product
decision. Run `dotnet build` and `dotnet test
tests/APP.Tests/APP.Tests.csproj` before considering this done - all
existing tests must still pass alongside your new ones.
```

## Verification (once Codex's backend work lands)

1. `dotnet build` at the solution root — must succeed with no new warnings
   beyond what already exists.
2. `dotnet test tests/APP.Tests/APP.Tests.csproj` — all existing tests plus
   the new Payment/Supplier/Customer repository tests must pass.
3. Check `INFRASTRUCTURE/Migrations/` for one coherent migration per prompt
   (not a tangle of half-applied ones) — run `dotnet ef database update`
   against a local/dev DB copy, not production, and confirm no data loss on
   existing rows (especially the `TermsOfPayment.DueDays` backfill in
   Prompt 1 and the `Supplier`/`Customer` field additions).
4. Spot-check the new endpoints via the local backend + swagger (`dotnet run
   --project API/API.csproj`, hit `/swagger/v1/swagger.yml`) — confirm AP/AR
   aging totals reconcile against a manually-computed sum on a couple of
   seeded BillingSheet/Invoice records before trusting the bucket math.
5. Only after backend work for a module is verified: regenerate the
   frontend OpenAPI client (`pnpm codegen`, pointed at the local backend)
   so frontend work on that module can begin against real generated types
   instead of hand-written ones.
