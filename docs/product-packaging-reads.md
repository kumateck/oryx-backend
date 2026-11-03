# Product packaging reads and style selection

Product details (and their print view) must show the active packaging definitions
returned by `GET /api/v1/product/{productId}/packages`. The broader product query
uses `IgnoreQueryFilters` for other historical sections, so it explicitly excludes
deleted packaging. Read identity resolution prevents previously tracked history
from being added back through navigation fixup while preserving packing styles.

For `GET /api/v1/production-schedule/package-material-stock/{productionScheduleProductId}`,
the legacy/unadopted path also excludes deleted definitions. Selection uses the
batch's saved `ProductPackingId`, never a style label or the first style on the
product. A matching specific row takes precedence over All styles for that
material; All styles is the fallback for other materials. Different specific
styles are excluded. Forecasts and the requirement source used for new/adopted
batch configurations share `ProductPackagingSelection.ForStyle`.

A material can be saved for multiple styles, including All styles alongside
specific styles. Duplicate material/style pairs are rejected before a whole-list
replacement deletes anything, and individual updates cannot duplicate another
active pair. Deleted history and the edited row itself are not conflicts.

Already applied batch configurations retain their adopted requirements even if
the master changes. Changing them requires the existing audited repair workflow.
Half batches still halve the prescribed quantity and retain the full allowance;
stock, reservations, approvals and consumption are not rewritten by a read.
No API shape, schema migration or data cleanup is required for this fix.

`ProductPackagingReadTests` reproduces fourteen saved rows with eight active and
six deleted, including historical material/style duplicates. It checks product
detail/edit agreement, full/half batch quantities, selected-style/default behavior,
unchanged applied requirements, and save validation. It runs with EF InMemory by
default. The same tests can run against PostgreSQL using
`ORYX_PACKAGING_READ_TEST_CONNECTION`, guarded to the dedicated
`oryx_packaging_read_test` database on port 55444 under a private
`/tmp/oryx-packaging-pg-*/socket` directory. It recreates only that fixture database.
Never use a rehearsal or customer connection for these write tests.
