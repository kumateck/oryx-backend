# Material-distribution department query timeout — 2026-10-24

## Confirmed failure

The reported shipment distribution request remained pending and then failed with
an Npgsql read timeout. The supplied API logs identify a 30,196 ms command for
`GetRequisitionDepartment`; read-only `pg_stat_activity` inspection also observed
that requisition lookup running for roughly 28 seconds.

The old query included `Requisition.Department` while global AutoInclude rules
also loaded `Requisition.CreatedBy.Department`. Both departments brought their
warehouse/location/rack/shelf trees into one SELECT, multiplying joined rows.
This work is unnecessary to calculate the shipment's department allocations.

## Correction and response contract

The shared department helper used by both material-distribution read paths now
queries the requisition's own department, suppresses incidental AutoIncludes,
and explicitly includes only warehouses and locations. These are the nested
fields exposed by `DepartmentDto` / `WarehouseDto`; racks and shelves were
loaded but not returned in that department DTO.

The query uses `AsSplitQuery` to avoid collection row multiplication and
`AsNoTracking` because the department is only mapped into a read response. A SQL
tag identifies this lookup in query logs. Requisition, department, warehouse and
location query filters remain active. It does not fetch creator credentials or
the creator department's storage tree.

`GET /api/v1/procurement/shipment-document/{shipmentDocumentId}/material-distribution`
keeps its existing DTO, permissions and read semantics. Allocation proportions,
rounding, FIFO source attribution, confirmation, approvals, inventory writes and
audit records are unchanged. The read does not call SaveChanges.

## Verification and delivery

PostgreSQL-provider SQL compilation reproduces the old multiple-collection join
warning and verifies the replacement compiles as split queries without creator,
credential, rack or shelf joins. In-memory checks verify the requesting department
and warehouse/location data, active filters and absence of tracking side effects.

This is a source fix, not a deployed release. Deploy the backend and retry the
reported shipment. No database migration, timeout increase, live stock adjustment
or distribution confirmation is required. Live inspection was read-only.

Documentation updated: this incident note, `docs/services.md`, and
`docs/workflows.md`. Existing commands and setup are unchanged.
