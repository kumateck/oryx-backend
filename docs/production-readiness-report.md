# Production readiness report

`GET /api/v1/report/production-readiness` is one read-only request for the complete
production schedule and its current product configuration readiness. It does not
call the individual product, specification, STP or ARD HTTP endpoints.

```http
GET /api/v1/report/production-readiness?departmentId=<production-department-guid>&startDate=2026-10-28&endDate=2026-10-28
Authorization: Bearer <token>
Module: Reports
SubModule: Report generation
```

All three query parameters are required. Dates are inclusive UTC calendar dates;
use the same date for a daily report or the month's first and last dates for a
monthly report. Any overlapping schedule contributes its non-cancelled batches
for products in the selected production department. Dates select schedules, not
a historical version of definitions or prices; `asOfDate` is unsupported.

## Complete response

- `departmentId`, `departmentName`, `startDate`, `endDate`, `generatedAt`: applied
  scope and generation time.
- `markets`: groups keyed by market ID, including a null-ID group for unrecorded
  markets. Each group has its market name, rows and totals. Names never determine
  grouping identity.
- Each row: product ID/code/name, selected packing ID/name, contributing schedule
  product IDs, expected batches, latest unit price and price unit, expected yield,
  packs per shipper, whole shippers, loose packs and all configuration checks.
- `products`: unique products with their markets, combined batches and configuration.
  This supports the product readiness matrix without additional requests.
- `productCount`, `readyProductCount`, `actionNeededProductCount`,
  `unavailableProductCount`: counts of unique products, not repeated market rows.
- `totals`: grand total batches, expected yield, shippers and loose packs.
- `warnings`: unavailable calculations or unrecorded prices.

`configuration` contains `specification`, `stp`, `ard`, `procedure`, `bom`,
`packaging`, `packingStyle` and derived `readiness`. Definition states are numeric:
0 Missing, 1 Configured, 2 Unavailable. Overall states are 0 ActionNeeded,
1 Ready, 2 CouldNotVerify. An unavailable check cannot mark a product ready.

## Business meaning and calculation

Readiness preserves the existing configuration-presence checks: a specification,
STP, ARD, route, latest effective active product BOM with items, packaging and
product packing definitions exist. The latest active BOM is selected before
checking its items; an older populated BOM does not rescue an empty current BOM.
These checks do not imply stock availability, verified documents, approval,
production authorization or finished-product release. Packing-style configuration
is product-level presence, as in Oryx Next; missing selected packing calculations
are separately flagged by null values and warnings.

Full batches count as 1 and half batches as 0.5. Rows group by market ID, product
ID and selected packing ID. Expected yield is the group's batch count multiplied
by configured packing yield, rounded once to a whole unit (positive midpoint up).
Shippers use floor division; loose packs are the remainder. Totals sum row
shippers and remainders without converting loose packs between packing styles.
Totals do not sum prices. The latest recorded product price is used, not a
historical schedule price.

Real zero prices/yields remain zero. Missing/invalid yield or packs-per-shipper
produce null affected calculations; any affected group or grand total also stays
null rather than returning a partial number. No recorded price produces null,
not zero. An empty plan succeeds with empty collections and zero totals.

## Authorization and failures

The request requires authentication and the live `CanViewQcAndQaReport` grant,
without an administrator-name bypass. Disabled users and unassigned departments
are denied. As in the existing department-scoped checklist report, production
users may select only their own department; other report readers may select an
existing production department. The permission is checked again before returning.

Missing/malformed bound parameters return the API's standard 422; semantic scope
validation (such as reversed dates or an empty department GUID) returns 400.
An absent production department returns 404, denied access 403, and corrupt batch
sizes/date ranges/packing ownership 409. Cancellation
propagates. Database/query failure fails the whole request; it does not manufacture
missing checks or return a partial report. The report projection reads schedule
rows, latest prices and all checks in one SQL statement and does not track entities
or perform writes. No migration is required.

## Verification

```sh
dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~ProductionReadiness
```

The PostgreSQL test is opt-in with `ORYX_READINESS_TEST_CONNECTION`. It refuses
non-loopback hosts and any database other than the dedicated `oryx_readiness_test`
fixture. Use a new disposable PostgreSQL container; the test creates the schema.
No customer database is required or accessed.

Local verification on 2026-10-28: the API build passed; all 18 focused tests passed,
including the real PostgreSQL projection with all seven checks, latest-active BOM
selection and archived-specification handling. The offline OpenAPI export declares
the stable `GetProductionReadinessReport` operation ID, all three required query
parameters and the complete typed response; the desktop's strict contract inspector
accepted it. The disposable database container was removed. Frontend activation,
customer acceptance and deployment have not been performed.
