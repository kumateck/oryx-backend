# Brief 12: API notes for the frontend (backend branch `qc-rm-test-definitions`)

What changed on the wire for raw-material chemical worksheets (`family 6`) in
`POST qc/worksheets/templates/import`. JSON is camelCase and enums are numbers. No route,
permission or database change; nothing changed for the other families or for Specification
documents (`family 7`).

## New properties on a proposed section

`template.sections[]` gains two properties. Both are null for every other family.

```json
{
  "name": "Assay – Titration",
  "testDefinition": "assay_titration",
  "testDefinitionVariants": ["back titration", "dried basis (100 − LOD)"],
  "fields": [ ... ]
}
```

- `testDefinition`: the key of the test definition the section was read with, or `null` when
  no definition matched and the layout was read as before (brief 09 behaviour).
- `testDefinitionVariants`: the alternatives the sheet's printed formula selected. An empty
  array means the definition's base form. Show them next to the definition name so the
  reviewer sees what was recognised.

Definition keys: `description`, `odour`, `taste`, `solubility`, `appearance_of_solution`,
`acidity_alkalinity`, `reducing_sugars`, `heavy_metals`, `chlorides`, `sulfates`, `iron`,
`identity_ir`, `identity_reaction`, `loss_on_drying`, `loss_on_ignition`, `sulfated_ash`,
`total_ash`, `assay_titration`, `assay_hplc`, `assay_uv`, `relative_density`, `ph`,
`refractive_index`, `melting_point`, `water`, `conductivity`, `optical_rotation`, `acid_value`,
`saponification_value`, `iodine_value`, `cap_colour`, `body_colour`, `printing_details`,
`disintegration_time`, `average_weight`.

Variant names (free text, display only): `back titration`, `no blank`,
`dried basis (100 − LOD)`, `anhydrous basis (100 − Water)`, `ignited basis (100 − LOI)`,
`dilution factor`, `% purity`, `filled weight`, `claim`, `weight per mL`,
`specific absorbance A(1%, 1cm)`, `against a standard`, `water-corrected (C1 − k × C2)`,
`observed angle only`, `blank titre (n2 − n1)`.

## New flag codes (`flags[].code`)

| Code | Meaning | Suggested label |
|---|---|---|
| `FormulaFromDefinition` | A calculated field (or calculated table column) whose formula comes from the test's definition, in the variant the sheet prints. Confidence is Medium, never High. The message quotes the printed formula. | "Formula from the test definition – confirm" |
| `DefinitionInputAdded` | An input the definition requires but the sheet does not print was added (Medium confidence). Also raised when an assay formula uses LOD / Water / LOI and the worksheet has no such test: an entry is added for it. | "Added: required by the test definition" |
| `DefinitionExtraLine` | A printed line or nested table the definition does not know. It is kept as a field. | "Not part of the test definition – kept" |

Changed meaning of existing codes:

- `FormulaFromPrint` is now raised on a defined test only when the printed formula **differs**
  from the definition's (the printed one is used; the message says so). It is still raised as
  before on tests with no definition.
- `FormulaNeedsReview` (`formulaExpression: null`) is now raised only on tests with no
  definition, plus the rare defined test whose formula could not be completed.
- `UnrecognizedContent` is no longer raised for a raw-material line or nested table: those
  are kept as fields (see below).

## Field shapes the Test Room and template builder will now receive

- **Titration assay.** The grid is a Table with one fixed row per **sample** (`Sample 1`,
  `Sample 2`) and columns `wtTaken`, `finalVolume`, `initialVolume`, `titreObtained`
  (calculated per row) and `assay` (calculated per row, unit `%`). The blank's readings are
  separate fields: `…_blank_final_volume`, `…_blank_initial_volume` (Entry, mL) and
  `…_blank_titre` (Calculated). The result is `AVG({…_titration.assay})`. The blank is not a
  table row because a formula can only reach a table through a whole column or the cells of
  its own row.
- **HPLC / UV assay.** The readings Table has one fixed row per reading and one column per
  solution (`standard`, `sample1`, `sample2`). Each column has a calculated
  `…_average` field (and `…_sd`, `…_rsd` when the sheet prints those rows). Each sample has a
  calculated `…_assay_1`, `…_assay_2`; the result is their mean.
- **Calculated column formulas** may reference worksheet fields outside the table and other
  sections' results, for example `(100 - {loss_on_drying_result})`.
- **Numbers read from the sheet are written into the formula as literals** (`* 51.35 *`,
  `715 *`, `- 0.35 *`), because a Constant field is not a formula input at run time. The
  Constant field is still proposed for display: `…_equivalence` (ShortText, as printed),
  `…_a11` and `…_wavelength` (Number), `…_limit` (ShortText, from a test name such as
  "pH [4.5 – 6.0]").
- **Select with options** on limit tests: `…_inference` has
  `options: ["Complies", "Does not comply"]`.
- **Printed notes**: a printed line with no blank of its own ("Solution S was used") arrives
  as `type 9` (Instructions), `mode` Constant, key `…_note`.
- **Generic nested tables**: a nested table that is not a titration or readings grid arrives
  as a Table field keyed `…_table` with the printed column headers.
- **Field keys follow the definition**, so the same test has the same keys on every sheet:
  `…_w1/_w2/_w3`, `…_reading_1/_reading_2`, `…_result`, `…_weight`, `…_preparation`,
  `…_observation`, `…_inference`, `…_factor`. Keys proposed by brief 09 for these tests
  (`ph_determination_i`, `ph_determination_mean`, `…_factor_of_volumetric_solution`) are gone.

## Review screen

- A defined test's formula is ready to save (`formulaExpression` is set), but it is Medium
  confidence and must be confirmed like any other proposal. Show the flag message: it
  carries the formula as printed on the sheet.
- In a titration, the sample weight column's unit is the unit of the printed equivalence
  (`mg` in almost every sheet), because the printed formula has no unit conversion. The
  reviewer should confirm it.
- A field flagged `DefinitionInputAdded` is an Entry field and is therefore required at
  submit. The reviewer may remove it; if a formula references it, the formula must be edited
  too.
