# Service contracts

## Product ARD and COA responses

`GET /api/v1/form/response` resolves a product response by the exact `batchManufacturingRecordId` and `productionActivityStepId`. Response detail includes `approved`, `rejected`, and `hasPendingApproval`.

Draft and final form submission require every field to belong to the response form. Existing response updates must retain the original form, batch, and production-step context. Product COA generation verifies the BMR/step ATR pair and refuses mixed-form responses. A pending or completed approval round produces a conflict; generating a COA never silently starts a replacement approval round.


## Cashflow and payments

All routes use the existing authenticated API version prefix. Mutating requests
are captured by the existing request audit middleware.

| Method and route | Permission | Contract |
| --- | --- | --- |
| `POST /api/v1/payments` | `CanRecordPayment` | Records a pending payment and returns its ID. |
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

Quotation item quantity must be positive, discount must be from 0 through 100,
and an omitted unit price is only defaulted from an unambiguous active agreement
or the current product price. The customer preferred currency is captured on the
quotation so monetary values are never currency-less.
