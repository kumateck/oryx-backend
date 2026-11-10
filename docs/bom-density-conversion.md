# BOM mass-to-volume conversion

This feature lets a raw-material BOM line be entered in mg while the production
department stores and issues the material in a liquid volume unit. The BOM line
must explicitly enable **Convert BOM mg to stock volume using density**.

## Configure a material

Configure an active unit with the symbol `g/mL` in the unit catalog. In
Materials, edit the raw material's department link and enter its verified
measurement in **Density (g/mL)**. Stock level edits preserve the previous
density and unit. A positive value with no unit, a deleted unit, or a different
unit is ineligible for conversion. Check the source measurement before
entering its g/mL equivalent. The system never relabels legacy values. For raw links the current desktop
edit action requires `CanEditLinkedRawMaterials`, and the existing department
link endpoint requires `CanLinkRawMaterials`.

The department link's stock unit must be a liquid unit (`ml` or `l`). The
conversion source is mg. When the toggle is enabled, the backend stores the
canonical mg unit on the BOM, verifies the linked stock unit and density, and
converts the required amount before production can reserve stock.

## Quantity rules

For density \(\rho\) in g/mL:

- `mL = mg ÷ (ρ × 1,000)`
- `mg = mL × ρ × 1,000`

The factor 1,000 is the number of milligrams in one gram. The backend also
scales between mg/g/kg and mL/L. Same-dimension and unknown-unit conversions
remain rejected.

Production requirements, requisitions, reservations and issues use the
department's stock unit. Warehouse stock and picking continue to show that
same stock unit. The BOM keeps its authored mg amount. At production start,
Oryx snapshots the exact density, units and converted stock quantity on the
batch, so later material-link edits do not change an active batch.

## Audit existing density records before enabling conversion

Run this read-only query against an approved copy or read replica. Review the
rows for the intended materials and all of their substitutes. For
`REVIEW` rows, use the original measurement to verify or correct the value
through Materials before enabling any BOM line. Review eligible rows against
the original measurement as well. The query checks metadata and cannot verify
how a measurement was taken.

```sql
SELECT link."DepartmentId", link."MaterialId", link."Density",
       link."DensityUoMId", density_unit."Symbol" AS density_unit,
       stock_unit."Symbol" AS stock_unit,
       CASE WHEN link."Density" > 0
                 AND density_unit."DeletedAt" IS NULL
                 AND lower(btrim(density_unit."Symbol")) = 'g/ml'
                 AND stock_unit."DeletedAt" IS NULL
                 AND lower(btrim(stock_unit."Symbol")) IN ('ml', 'l')
            THEN 'ELIGIBLE' ELSE 'REVIEW' END AS conversion_status
FROM "MaterialDepartments" link
LEFT JOIN "UnitOfMeasures" density_unit ON density_unit."Id" = link."DensityUoMId"
LEFT JOIN "UnitOfMeasures" stock_unit ON stock_unit."Id" = link."UoMId"
WHERE link."DeletedAt" IS NULL
ORDER BY conversion_status DESC, link."DepartmentId", link."MaterialId";
```

## Release and rollback

Apply the `AddBomDensityConversion` migration and deploy the backend before the
desktop release that sends the new BOM field. Existing BOM items default to
conversion off. Verify a converted quantity in the product BOM dialog and the
production batch's stock-unit requirement before starting a real batch.

To stop using conversion, first prevent new density-converted batches and
change the affected active BOMs to reviewed, unit-matched quantities. Let
already-started batches finish using their saved stock-unit snapshots. Roll
back backend and desktop releases together only after those batches are clear;
leave the additive database migration applied. Do not edit saved reservations
or issue records as part of rollback.
