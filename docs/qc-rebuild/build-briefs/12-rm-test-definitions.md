# Build brief 12 — Raw-material test definitions

The raw-material chemical recognizer (brief 09) reads each numbered test by its layout:
label-and-blank lines become fields, and a formula is kept only when it is printed on
one line. A study of 329 real worksheets (`QC_RM_STUDY_DIR`, never committed; aggregate
census in `12-corpus-test-census.json`) shows that is not enough:

- Assay formulas **are** printed, but as a stacked fraction over three lines
  (numerator / dashes / denominator), so 196 calculated fields came back empty.
- The same test is laid out the same way in every sheet. About 30 test kinds cover 92%
  of all test instances.

This brief makes the importer recognise **which test** a section is and apply that
test's **definition**: the inputs it requires, its table, its constants, its formula and
its result. The layout reader stays as the fallback for tests with no definition.

## Locked decisions (2026-10-01)

1. A test is identified by its name through a synonym list, never by position.
2. A definition states the required inputs. The sheet is checked against it: an input the
   definition requires but the sheet lacks is added and flagged `DefinitionInputAdded`;
   a sheet line the definition does not know is kept as a field and flagged
   `DefinitionExtraLine`. Nothing on the sheet is silently dropped.
3. Formulas come from the definition, with the variant chosen from what the sheet prints.
   The printed formula text is kept as the field's provenance. A definition formula is
   `Medium` confidence and flagged `FormulaFromDefinition`; it is never `High`, and the
   reviewer still confirms it.
4. A test with no definition keeps today's behaviour, including `FormulaNeedsReview`.
5. Constants printed on the sheet (equivalence, A(1%,1cm), wavelength, chromatographic
   conditions, limits in the test name such as "pH [4.5 – 6.0]") are read from the sheet
   into Constant fields. They are never taken from the definition.

## The definition model

`RawMaterialTestDefinition` (code, not database; one file per group under
`WorksheetDocxImport/TestDefinitions/`):

- `Key`, `Title`, `NamePatterns` (synonyms, canonical-text match, ordered so that
  "Assay – Titration" wins over "Assay").
- `Inputs`: key, label, type, unit, `Replicates` (1, or 2 for "(i) (ii)" readings),
  `Required`.
- `Table` (optional): fixed row labels and columns, with per-row calculated columns.
- `Constants`: what to read from the sheet and the pattern that finds it.
- `Calculations`: ordered calculated fields with formula templates over the input keys.
- `Variants`: named alternatives with the sheet text that selects each.
- `ResultKey`: the field a Specification characteristic binds to.

## Definitions to build (instances in the study corpus)

**Gravimetric (W1, W2, W3 crucible weighings)**
- Loss on Drying (174), Loss on Ignition (8): `(W2 − W3) / (W2 − W1) × 100`.
- Sulfated Ash / Residue on Ignition (149), Total Ash: `(W3 − W1) / (W2 − W1) × 100`.
  Confirm each against the printed "= __ − __ × 100%" line; where the sheet prints a
  different arrangement, the printed one wins and is flagged.

**Assay by titration (88, plus 14 under plain "Assay")**
- Inputs: factor of volumetric solution; table Blank / Sample 1 / Sample 2 × weight
  taken, final volume, initial volume, titre (titre = final − initial).
- Constant: equivalence in mg, read from "1 mL of … is equivalent to N mg of …".
- Per sample: `(SampleTitre − BlankTitre) × Factor × Equiv × 100 / (Weight × (100 − LOD)) × 100`.
- Variants, selected by the printed numerator and denominator lines:
  - back titration: `(BlankTitre − SampleTitre)`;
  - no drying correction: denominator is the weight alone;
  - water instead of LOD: `(100 − Water)`;
  - extra factor: dilution factor, % purity or filled weight in the numerator.
- Result: average of the two samples.

**Assay by HPLC (11, plus 17 under plain "Assay")**
- Inputs: weight of standard, weight of sample (i) (ii), dilution; table Injection 1–5 ×
  Standard / Sample 1 / Sample 2 with Average, SD and RSD rows as calculated.
- Constants: the chromatographic-conditions block, kept as printed; % purity of standard
  is an Entry.
- Per sample: `(SamplePeak / StdPeak) × (StdConc / SampleConc) × Purity × 100 / (100 − Water)`.
  Variants: with or without the water/LOD correction.
- Result: average.

**Assay by UV (11, plus 5 under plain "Assay")**
- Inputs: weight of sample (i) (ii), dilution; table Sample 1 / Sample 2 (and Std when
  printed) × absorbance readings with mean.
- Constants: A(1%,1cm) and wavelength when printed.
- Variants:
  - specific absorbance: `(Abs × DilutionFactor × 100 × 100) / (A11 × Weight × (100 − LOD))`;
  - against a standard: `(SampleAbs / StdAbs) × StdConc × Purity …` as printed.
- Result: average.

**Physical constants**
- Relative Density / Specific Gravity / Weight per mL (33): pycnometer weights w1, w2,
  w3; `(w3 − w1) / (w2 − w1)`.
- pH (71), Refractive Index (24), Melting Point (24), Conductivity (24), Water by KF
  (58): two readings and their mean. Conductivity variant `C1 − 0.35 × C2` when printed.
- Specific Optical Rotation (54): weight, observed rotation (two readings), path length,
  concentration; `α × 100 / (l × c)`, with the dried-basis variant when the sheet prints
  `(100 − LOD)`.
- Acid Value (11): weight, titre; `Titre × 5.61 / Weight`, the numeric factor read from
  the sheet. Saponification and Iodine Value follow the same shape when printed.

**Observation and limit tests (no calculation)**
- Description (318), Odour (34), Taste: one observation.
- Solubility (263): one observation per printed solvent.
- Appearance of Solution (118), Acidity/Alkalinity (64), Reducing Sugars (9):
  weight, preparation, observation.
- Heavy Metals (68), Chlorides (46), Sulfates (46), Iron (14) and other limit tests:
  test solution, reference solution, observation, inference (Complies / Does not comply).
- Identity by IR (90): balance, weight of KBr, weight of sample, attached spectrum,
  observation.
- Identity by colour or reaction (87): preparation, observation.

**Capsule shells (24 sheets)**
- Cap colour, body colour, odour, printing details: observation.
- Disintegration time: time in minutes.
- Average weight: 20 weights; total and average.

## Recognizer changes

1. **Stacked fractions.** Add a primitive that joins `numerator / ----- / denominator`
   lines (and the blank "( – ) x x x" worked lines under them) into one printed-formula
   string, used both to pick a definition variant and as provenance.
2. **Nested reading tables** (absorbance, peak area, capsule weights) map onto the
   definition's `Table`; any other nested table is kept as a generic table, not dropped.
3. **Unnumbered tests.** A title row with an empty number cell followed by a content row
   starts a section (e.g. `244 - Potassium Iodide`, which returns 0 fields today).
4. **Cross-test references.** `LOD` / `Water` in an assay refers to this worksheet's own
   Loss on Drying or Water result field. When the worksheet has neither, add an Entry
   field for it and flag `DefinitionInputAdded`.
5. The dropped lines found in the study ("Observation", "Preparation", "Weight of
   sample", "Titre x 5.61", "1 mL of … is equivalent to …") must all land in a field.

## Tests

- One synthetic test per definition and per variant, always run.
- Study-corpus tests (`QC_RM_STUDY_DIR`): 0 crashes; every instance of a defined test
  produces its definition's inputs and a non-empty formula; `FormulaNeedsReview` falls
  from 196 to only undefined tests; `UnrecognizedContent` "was not turned into a field"
  falls to 0; `244 - Potassium Iodide` yields its tests.
- Formulas evaluate: for each definition, feed sample numbers through the QC formula
  evaluator and assert the expected result, so a definition can't ship a formula that
  doesn't parse.
- All existing QC tests keep passing, including the 7-pair corpus (`QC_RM_CORPUS_DIR`).

## Out of scope

- Template identity (file number vs Spec. No. vs material name) — separate brief.
- Legacy `.doc` files.
- Finished-product chemical worksheets.
