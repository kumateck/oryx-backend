# Draft stock migration material links

GET and POST `/api/v1/stock-migrations/{id}/material-links/{kind}` preview and
create missing department links. Stable operation IDs:
`PreviewStockMigrationMaterialLinks`, `CreateStockMigrationMaterialLinks`.
The desktop shows one preview group per permitted kind, allowing all eligible,
subset or individual material selection (maximum 1,000 per request).

Both operations require `CanViewOpeningBalances`, `CanCorrectOpeningBalances`
and the stored kind's `CanLinkRawMaterials` or `CanLinkPackagingMaterials`.
`MaterialKindPermission` guards the route kind; the repository verifies stored
material kinds and reloads grants immediately before writes. The actor must
have the same active department as the migration, including for an otherwise
unscoped account. No new keys, grants or schema migration are introduced.

Preview re-resolves all draft rows and deduplicates materials whose blockers
identify a missing department link. UOM comes from the working Excel unit,
including row corrections: PCS/Piece → Piece, mass → mg, liquid → ml. Conflicting
families, unsupported/absent units, missing/ambiguous unit definitions or retired
existing links require explicit review and cannot be selected. Excluded and
unlimited materials are not offered.

Defaults are 100 / 1,000 / 10,000 pieces, kg or L respectively, converted with
`SHARED.UomConverter` and stored as pieces, mg or ml. POST compares the selected
preview's unit ID and stored levels with freshly computed defaults. It never
accepts caller-specified replacement settings for an existing link.

The POST transaction acquires the existing migration advisory lock, checks the
draft, then holds `SHARE ROW EXCLUSIVE` on `MaterialDepartments` while checking
absence and inserting. That short table lock also blocks legacy upsert writes;
the legacy POST `/material/department` itself remains unchanged. New link
requests serialize with each other. Newly existing links are counted and
preserved, including unit, levels, density and audit metadata. Other stale
preview changes return 409 and roll back the group. All draft rows are rechecked
before commit. Approval, stock batches, quantities and write-offs are untouched.
Normal request audit headers and entity creation audit fields remain in use.

Verification:

```sh
ORYX_MATERIAL_LINK_TEST_CONNECTION='Host=localhost;Database=postgres;Username=TEST_USER' \
  dotnet test tests/APP.Tests --filter StockMigrationMaterialLinkTests
ORYX_WRITE_PERMISSION_FILES=1 dotnet test tests/APP.Tests --filter PermissionCatalogTests
```

The PostgreSQL test creates and drops a uniquely named database; the configured
test account needs CREATE DATABASE. It checks defaults, Excel unit handling,
permissions, missing departments, non-drafts, deduplication, stale previews,
existing-link preservation, concurrent submissions and unchanged stock.
The permission catalog/baseline remain unchanged because no key or unguarded
endpoint is added. Companion desktop documentation:
`oryx-live-desktop/docs/migration-material-links.md`.

Backend PR target: `desmond-latest`; desktop PR target: `development`. List the
four existing keys and link the companion PRs when opening them. No push,
deployment, live stock operation or schema migration is part of this change.
