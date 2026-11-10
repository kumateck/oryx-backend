# BOM mass-to-volume conversion

This feature lets a raw-material BOM line be entered in mg while the production
department stores and issues the material in a liquid volume unit. The BOM line
must explicitly enable **Convert BOM mg to stock volume using density**.

## Configure a material

In Materials, edit the department link for the raw material and enter its
measured density in **g/mL**. Do not use a range midpoint or a generic default.
The customer reported 1.35–1.56 g/mL for the affected materials; enter the
measured value that applies to each material. A missing or nonpositive density
prevents the conversion from being saved or applied to a production batch.

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
