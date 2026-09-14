# Materials ready for checklist report

## Purpose

`GET /api/v1/report/materials-ready-for-checklist` returns pending raw and packaging
material receipts that QC can use to monitor checklist work.

## Query contract

The endpoint accepts these optional query parameters:

- `departmentId`: a production department. For a non-production or R&D user, omit
  it to consolidate all active production departments. A production user is always
  restricted to their own department and cannot select another department.
- `materialKind`: `0` for raw material or `1` for packaging material. Omit it to
  include both types.
- `startDate` and `endDate`: inclusive timestamps applied to the receipt record's
  `CreatedAt`. A start later than the end returns `Report.DateRange` with HTTP 400.

The endpoint returns an array of `MaterialReadyForChecklistDto`. Each row includes
the receipt ID, material identity and kind, shipment-invoice identity, quantity and
UOM, arrival/creation timestamps, status, and production-department identity.

## Scope and validation

| Caller | No `departmentId` | Matching production department | Other/non-production department |
| --- | --- | --- | --- |
| Production | Own department | Own department | HTTP 400 `Report.DepartmentScope` |
| Non-production / R&D | All production departments | Selected department | HTTP 400 `Report.DepartmentScope` |

Users without a valid department receive `Report.DepartmentScope`. Missing warehouse
configuration does not produce a report error; the result is an empty array when no
matching pending receipt exists.

## Performance design

The query uses `AsNoTracking()` and `IgnoreAutoIncludes()`, then projects directly to
the report DTO in SQL. It does not materialize `Material.Batches`, batch events,
reserved quantities, shelf assignments, checklists, requisition items, or supplier
graphs. This removes the former `s1`-through-`s43` join/subquery expansion and avoids
AutoMapper resolver N+1 queries. Request cancellation is passed through to EF Core so
abandoned HTTP requests do not continue consuming a database connection.

## Verification and rollout

Focused repository tests cover consolidated QC access, selected-department filtering,
production-user isolation, and optional material-kind behavior. After deployment,
exercise the production-sized date range and verify latency and database command time
remain comfortably below the configured timeout. Regenerate the frontend OpenAPI
client on the next normal API-contract generation run; the current QC adapter declares
the new lean row type locally to keep the dashboard aligned meanwhile.
