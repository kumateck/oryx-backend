# QA BMR issued report

`GET /api/v1/report/bmr-issued`, operation ID `GetIssuedBmrReport`, returns one
complete read-only snapshot. `startDate` and `endDate` are required date-only ISO
values; `departmentId` optionally selects a production department. The range is
inclusive in UTC: `IssuedDate >= start at midnight` and `< day after end`.
Missing/reversed ranges, the maximum representable end date and an empty GUID
are rejected. Unknown/non-production department IDs are refused.

The existing `CanViewQcAndQaReport` grant is checked before and after reading.
Actors must be enabled with an assigned department. Production actors are scoped
to their own department even when the filter is omitted. A changed department
assignment/type refuses disclosure; QA readers with the report grant can report
across departments. No implicit administrator bypass or role grants were added.

Issuance evidence is `BatchManufacturingRecord.IssuedDate`, not creation time,
step status, or QC approval. The active issue list excludes completed steps and
therefore cannot supply this report. The projected query explicitly retains
completed, soft-deleted and cancelled history; it returns archived/cancelled
markers as well as the record identity. Ordinary report generation writes nothing
and does not issue, approve or advance records. Nullable issuer navigation remains
nullable; missing historical issuer information must not be guessed.

The desktop prints Product (name/code), Batch (number/size), Validity (manufacture/
expiry), Issued (date/person), and Department, with grouped values on separate
lines. Spreadsheet exports keep all nine fields individually. The snapshot count
counts BMRs; heterogeneous batch quantities are not summed. Print, export and
screen use the same result; incomplete, duplicate or out-of-range responses are
refused by the client.

Limitations: one row per BMR's currently stored issue timestamp, not an event log
of overwritten reissue times. Product names, codes, unit symbols, packing labels
and department assignments are current linked master data; these were not captured
immutably at issuance. Hard-deleted history cannot be reconstructed. No database
migration is required. The companion API must be deployed before UI activation,
with the normal customer contract fingerprint review. This implementation does
not deploy or modify customer data.

Sources: `APP/Repository/ReportRepository.IssuedBmr.cs`,
`DOMAIN/Entities/Reports/IssuedBmrReportDto.cs`, the report controller/interface,
and `tests/APP.Tests/Reporting/IssuedBmrReportTests.cs`.

Focused verification uses EF InMemory for permissions, validation, cancellation,
empty periods and scope. The PostgreSQL case tests the real historical projection,
inclusive date boundaries, nullable issuer and preserved numeric precision:

```sh
ORYX_ISSUED_BMR_TEST_CONNECTION='Host=/tmp;Port=55441;Database=oryx_bmr_issued_report_test;Username=TEST_USER' \
dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~IssuedBmrReportTests
```

The integration fixture accepts only the exact local database name above and
resets that disposable database. Never supply a customer connection. Local runs
passed all 11 issued-BMR cases, including PostgreSQL; the combined readiness
regression passed 22 cases with one unrelated opt-in PostgreSQL readiness test
skipped. API build passed with existing dependency/compiler warnings. Customer
authenticated end-to-end and installed-client acceptance remain release checks.
