# Finance costing: list prices and customer pricing agreements (2026-10-31)

Costing answers one question: what a product is priced at for a customer, and in
which currency. Two prices exist, and both now record their currency:

| Price | Where it lives | Currency |
| --- | --- | --- |
| Customer pricing agreement | `CustomerPricingAgreements` | `CurrencyId` (required, unchanged) |
| Product list price | `ProductPrices` (dated history, owned by `Product`) | `CurrencyId` (new, nullable) |

An active agreement for the customer, product and packing style wins; otherwise
the product's current list price applies (`CustomerPricingResolver`). Every
resolution now carries the currency of the price it chose.

## No conversion

Prices are never converted between currencies.

- **Quotations.** When the customer has a preferred currency, every resolved
  price must already be in it. A list price in another currency is refused with
  `CustomerPricing.Currency` (409); the user adds an agreement in the customer's
  currency or types the unit price. Before this change the list price was
  silently treated as the customer's currency. When the customer has no
  preferred currency, the quotation takes the one currency its prices are in
  (agreement or list price); prices in different currencies are refused.
- **Invoices.** Proforma/invoice amounts are totalled per currency using the
  agreement currency or the list price currency. Before, list prices were always
  assumed to be in the base currency, which is what the backfill records.
- **Reports.** The production schedule summary/detailed reports and the invoiced
  products reports return a `Currency` on every row, so amounts in different
  currencies are never read as one.
- `GET /customers/{id}/quotations/resolve-price` also returns `CurrencyId`.

## Setting list prices

List prices are set only in Finance costing.
`PUT /api/v1/costing/list-prices/{productId}` (`SetListPriceRequest`: price,
optional unit, required `CurrencyId`) appends a dated `ProductPrices` row, so
quotations and invoices already raised keep the price they used.

A product's `Price` on create/update is now optional (minimum 0) so the product
forms no longer carry it: 0 creates the product unpriced and, on update, keeps
the current list price. A positive price still works for older clients (Oryx
Next); with it, an optional `PriceCurrencyId` defaults to the base currency on
create and to the current one on update, and a changed currency alone is
recorded as a new price row. A quotation for a product with no list price and
no agreement is refused (`CustomerQuotation.PriceRequired`) unless the unit
price is typed, instead of quoting 0.

## Permissions: a Finance section

The role permissions page now has a **Finance** module:

| Submodule | Key | Moved from |
| --- | --- | --- |
| Payments | `CanViewPayments`, `CanRecordPayment`, `CanApprovePayment` | Logistics |
| Cashflow Reports | `CanViewCashflowReports` | Reports |
| Costing | `ManageCosting` (new) | — |

Grants are stored by key and grouped into modules when read, so moving keys
keeps every role's access. The permission list caches were renamed
(`Permission_Cache_V3_*`) so old groupings are not served after deploy.

`ManageCosting` guards every costing endpoint and the customer
pricing-agreement endpoints (`/customers/{id}/pricing-agreements*`).
`ManageCustomerContracts` stays in Finished Goods Warehouse → Customer
Management for customer profile and contacts. Migration
`GrantManageCostingToContractManagers` gives `ManageCosting` to every role that
held `ManageCustomerContracts`, with the same access types, so nobody loses
access on deploy; after that the two can be granted separately. Its Down removes
every `ManageCosting` grant, including ones made later. Both migrations were run
twice against a scratch PostgreSQL 18 with sample rows (idempotent), and the
Down was checked to leave `ManageCustomerContracts` untouched.

## Costing API

All endpoints require `ManageCosting`.

| Method | Path | Returns |
| --- | --- | --- |
| GET | `/api/v1/costing/list-prices?page&pageSize&searchQuery&departmentId&currencyId` | `Paginateable<ProductListPriceDto>` |
| GET | `/api/v1/costing/list-prices/{productId}/history` | `ListPriceHistoryDto[]`, newest first |
| PUT | `/api/v1/costing/list-prices/{productId}` | 204 |
| GET | `/api/v1/costing/pricing-agreements?page&pageSize&searchQuery&customerId&productId&activeOn` | `Paginateable<CustomerPricingAgreementDto>` across customers |
| GET | `/api/v1/costing/pricing-agreements/{id}` | `CustomerPricingAgreementDto` |

Agreements are still written through `/api/v1/customers/{id}/pricing-agreements`
(create, update, delete). Those endpoints now declare their response types, so
the desktop generates typed hooks for them. `CustomerPricingAgreementDto` gains
`CustomerName` and `ProductCode`. There is no approval step for customer pricing
agreements, before or after this change, and no department scoping: products
and customers are not department-scoped for pricing.

## Migration `AddListPriceCurrency` — changes live data

Up:
1. Adds nullable `ProductPrices.CurrencyId` with an index and a restricting
   foreign key to `Currencies`.
2. **Backfills every existing list price to the base currency**
   (`IsBaseCurrency`, not deleted, oldest first). Amounts are not changed. If
   no base currency is configured the rows stay empty and are read as the base
   currency once one exists.

Down drops the column, which removes the backfill with it. Nothing else is
touched, so rolling back is lossless for data that existed before the
migration; list-price currencies chosen after it are lost on rollback.

Behaviour to expect after deploy: a quotation for a customer whose preferred
currency is not the base currency, for a product with no agreement in that
currency, is now refused instead of using the base-currency list price as if it
were in the customer's currency. Add agreements in the customer's currency, or
set the list price currency, before deploying if that is common.

Tests: `CostingRepositoryTests`, `ProductListPriceTests` (unpriced products and
the Finance permission section), plus currency cases in
`CustomerQuotationPricingTests` and `InvoicePricingTests`.
