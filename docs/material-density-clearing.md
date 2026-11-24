# Clear a saved material density

An authorized user can clear a saved density from the linked material's stock-level dialog. The explicit Clear density action asks for confirmation because density is shared by all departments. Leaving the measurement field untouched or blank does not remove existing data.

The server sets Material.DensityGramsPerMilliliter to null and removes legacy density fallbacks on every department link, including archived links that could otherwise be restored. It appends one immutable audit entry with a null resulting density, the previous shared value and each department link's previous density/unit. Repeating the same clear is a no-op.

Physical stock quantities, batch units, ledger history and density already captured on reservations/transfers are preserved. New conversions between mass and volume require a verified density again. This action does not reset a BOM, reservation or stock quantity.

DELETE `/api/v1/material/raw/{materialId}/density` uses existing `CanEditRawMaterialDensity`; packaging uses its separate path and existing `CanEditPackagingMaterialDensity`. Both belong to Warehouse / Linked Materials. The server checks the stored material kind, active actor and freshly loaded grant inside the transaction. No role is newly granted access.

Deploy `AllowClearingMaterialDensity` before activating the new endpoints. It makes the audit result nullable and adds prior-link evidence as JSONB. Existing audit rows and append-only protections remain. Rollback refuses to discard recorded clear evidence. No application migration is applied by this source change.

Validation: API build passed; 27 focused permission, shared-density and real PostgreSQL migration/history tests passed with no skips. EF reports no pending model changes. The migration and tests ran only against disposable local databases.
