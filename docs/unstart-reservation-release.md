# Release historical Unstart reservations

Normal Unstart marks unused reservations released and retired with the original Unstart receipt ID. Active EF queries exclude ReleasedAt rows even if a legacy row has no DeletedAt. No physical stock is restored, and no QC/retest status changes.

The material-scoped `/api/v1/unstart-reservations/{materialId}/raw` and `/packaging` endpoints expose GET preview and POST release. Preview is limited to 200 candidate rows for this material in the actor's department. Each selected reservation must be identified in an immutable Unstart receipt, have an archived production run, no restart, no issue/receipt/consumption evidence, and consistent material/unit/warehouse ownership. Missing historical proof is blocked for investigation. The preview token covers current reservation and eligibility evidence. Apply rechecks grants and evidence inside a serializable transaction, rejects stale or blocked selections atomically, and writes an append-only audit receipt and original-run log. Identical request IDs are idempotent only for the exact same actor and payload.

Apply `AddUnstartReservationRepairs` before enabling the companion desktop dialog on the material Reserved tab. Database triggers and EF prevent mutation/deletion of repair receipts. This migration adds no role grants and performs no stock repair automatically.

Permissions registered once under **Warehouse / Approved Materials**:

- `CanRepairUnstartRawMaterialReservations`: POST raw; granted to nobody.
- `CanRepairUnstartPackagingMaterialReservations`: POST packaging; granted to nobody.

GET requires `CanViewApprovedRawMaterials` or `CanViewApprovedPackagingMaterials` respectively. Apply additionally requires that same view key, an active user and current department membership. Assign the new repair tasks explicitly. The desktop must use the same dedicated keys and refresh reservations and stock reports after success.

Tests include endpoint policies/catalog, deny and variant checks, unsafe evidence blockers, the released-row query filter, and real PostgreSQL stale-preview, apply, replay, audit-trigger and physical-stock/QC preservation checks. Run with the disposable PostgreSQL connection in `ORYX_ISSUE_REPAIR_TEST_CONNECTION`. No live repairs or grants are performed by tests.
