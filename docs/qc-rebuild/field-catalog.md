# Worksheet Field Catalog

Every field carries a **Mode**: `Constant` (a fixed method parameter set at template
authoring, e.g. "Apparatus: II (Paddle), Speed: 100rpm" — editable only via template
revision), `Entry` (analyst fills in), or `Calculated`. This was confirmed against a
real Dissolution worksheet, which prints method parameters in bold as fixed text
rather than blanks to fill in.

```
STRUCTURE       Section (optional linked Instrument)
                Table (ColumnDefinitions[]: each a small field def — label, type,
                    unit; rows repeat, fixed-count e.g. 20 tablets/6 vessels or
                    open-ended e.g. N sampling points; columns are heterogeneous —
                    a table can mix Measurement, GrowthObservation, CalculatedValue
                    columns in one grid)
                Instructions, Heading
BASIC           ShortText, LongText, Number, Date, Time, Select, MultiSelect, Checkbox
SCIENTIFIC      Measurement (value+unit)
                CalculatedValue (formula over any FieldKey in the same
                    WorksheetInstance — not just same-section — and/or a table-column
                    aggregate: Sum, Average, Min, Max, %RSD/StdDev)
                Result (renders Pass/Fail/OOS/Alert; binds to EITHER a Specification
                    Characteristic OR self-contained Constant-mode acceptance text/
                    range authored directly on the field — see refinement 1 below)
                Instrument (hard calibration gate), Reagent/ReferenceStandard (hard
                    expiry gate)
MICROBIOLOGY    Organism, Dilution, IncubationTemperature, IncubationPeriod,
                GrowthObservation (Growth/No Growth/Absent/Detected/TNTC),
                ColonyCount, CFUCalculation (= (ColonyCount x DilutionFactor) /
                PlatedVolume, unit-aware — confirmed verbatim against real worksheets)
INTEGRATION     ReferencedResult (cross-WorksheetInstance; resolved by
                    SourceWorksheetTemplateId + SourceFieldKey PLUS a runtime
                    resolution key — e.g. paired with a Reagent-type field capturing
                    a batch number — since many instances of the source template can
                    exist over time and the match has to be to the right one, not
                    just the right template; see refinement 2 below)
                Same-worksheet forward-link (lighter-weight UI jump, e.g.
                "Identification: refer to chromatogram from Assay" within one
                WorksheetInstance — not a cross-instance resolution)
```

`Result` and `ReferencedResult` are the two types the whole traceability chain runs
through — every other type is just data entry, but these two are what
`Specification.Characteristics` and cross-worksheet dependencies actually bind to.

## Precision refinements from a full template-coverage check

Prompted by the direct question "will this actually build every template reviewed?"
— going back through all of them against the catalog above surfaced six real
precision gaps, closed here:

1. **`Result` needed a path that doesn't require a `Specification`.** Media
   Qualification worksheets (Cetrimide, Xylose, Plate Count Agar) have pass/fail
   criteria baked into the method itself (pH 7.00–7.40, "difference in CFU not more
   than factor 2") — they're never tied to a COA-bound `Specification` at all, since
   Media Qualification produces no COA. `Result` now supports inline `Constant`-mode
   acceptance text/range as an alternative to `Characteristic` binding.
2. **`ReferencedResult` needed a runtime resolution key, not just a template
   pointer.** A Water worksheet references six different Media Qualification
   instances by medium name *and batch number* — many qualification records exist
   over time for e.g. "MacConkey Agar," and the field needs to resolve to the one
   matching the batch actually in use, not just any instance of that template.
3. **`Table` needed a formal column model.** Real tables are rows × a *fixed set of
   heterogeneous typed columns*, not one repeated field — confirmed by Uniformity of
   Weight (20 rows × 1 column), Dissolution (6 vessels × 4 columns), the
   growth-promotion comparison table (New Lot vs. Previous Lot, each with its own
   Plate1/Plate2/Avg/Colour), and Water's specified-organism table (N points × 4
   organism columns).
4. **`CalculatedValue` aggregates needed more than Average.** Uniformity of Weight
   needs Min/Max/Deviation%; a real Dissolution STP's system-suitability check needs
   repeatability (%RSD) across 6 injections.
5. **Formula references are worksheet-scoped.** A real Dissolution calc pulls
   `Avg Abs Spl` and `Avg Abs Std` from two different parts of the same worksheet —
   `CalculatedValue` can reference any `FieldKey` in the same `WorksheetInstance`,
   not just its own section.
6. **The header block auto-populates; it's never analyst-entered.** Every filled ARD
   reviewed prints Batch No, AR No, Spec No + Revision, STP No, Issue No, and four
   dates in a fixed header. The execution view renders this from `TestRequest` +
   `Specification` + `WorksheetTemplate.StpId` → `StandardTestProcedure` — it is not
   a set of worksheet fields at all, and should never be modeled as one.

**Not a gap, but worth being explicit about**: STP authoring is neither this field
catalog nor a single free-text blob — it's a third, distinct editor shape. The
field-palette builder exists because a worksheet *captures typed data values*; an
STP captures nothing; it's read-only instructions. So it isn't assembled from this
catalog. But it also isn't one giant rich-text field — the Amoxicillin STP document
is already numbered into five fixed sections (Purpose/Scope/Responsibility/
Accountability/Procedure), and Procedure is itself a numbered step list where
individual steps are frequently *just* a cross-reference ("5.7 Sulphated Ash: Refer
to QCD/SOP/053"). `StandardTestProcedure.Steps[]` (see
[domain-model.md](./domain-model.md)) gives that pattern real structure: an ordered
list of steps, each rich text plus an optional *structured* link to another STP/SOP
— not typed prose that breaks if a document gets renumbered. Purpose/Scope/
Responsibility/Accountability stay as four fixed rich-text fields, since they're
short boilerplate paragraphs with no comparable cross-reference problem to solve.
Phase 2 UI design needs three distinct editors, not two: this structured-document
editor for STPs, the field-palette builder for `WorksheetTemplate`, and ordinary
form inputs for `Specification`'s characteristics — none conflated into another.

## Confirmed against real worksheets

Reviewed: Environmental Monitoring worksheets and COAs (Tablet, Syrup, Ointment, Beta/
Non-Beta Warehouse, Microbiology Lab areas), Purified Water sampling-point worksheets,
culture-media growth-promotion worksheets (Cetrimide, Xylose, Plate Count Agar, and
others), and finished-product chemical assay worksheets (Uniformity of Weight,
Dissolution, Friability, Hardness, HPLC Identification/Assay for a dual-active tablet).

- **`WorksheetTemplate.Category` includes `MediaQualification`** as a third category
  alongside Chemical/Microbial. The growth-promotion worksheets (Cetrimide, Xylose,
  Plate Count Agar, etc.) are full worksheets in their own right — test strains
  (Pseudomonas aeruginosa ATCC 9027, E. coli ATCC 8739), comparison against the
  previously-approved lot, equipment/references/signoff — and other worksheets
  reference them by medium name + batch number.
- **A single worksheet can carry many independent `ReferencedResult` fields, not
  one.** The real Water microorganism-testing worksheet alone references six
  separate media-qualification instances (MacConkey Broth, MacConkey Agar,
  Rappaport, XLD Agar, Cetrimide, Mannitol Salt Agar) in one document, each shown
  today as a manual "Refer to Culture Media Batch Data sheet serial numbered:
  QCD/MIC/.../26/XXX" — exactly the gap `ReferencedResult` closes.
- **The CFU formula matches verbatim**: `Result (cfu/g) = Average Count x Dilution
  Factor`, with Plate 1/Plate 2 -> average as an intermediate step, exactly as printed
  on the real Amoxicillin finished-product worksheet.
- **Formulas can reference table aggregates, not just scalar fields.** A real
  Dissolution calculation — `% Dissolution = (Avg Abs Spl / Avg Abs Std) x (Std conc /
  Spl conc) x 100` — pulls from an "Average" row computed across a 6-vessel table
  (Ves 1–6 columns), combined with separately-entered concentration scalars.

  **Formula engine spike resolved (see
  [build-briefs/00-formula-engine-spike.md](./build-briefs/00-formula-engine-spike.md)):
  build a new, small local evaluator — do not reuse `FormulaWorksheetPreprocessor`.**
  Reading it in full showed it isn't a local evaluation core at all — it's a thin
  adapter (`internal`, not even accessible outside its own namespace without
  modifying it) that ships the actual expression evaluation to an **external formula
  microservice**, with its own versioned DSL, hash-locked definitions
  (`definitionHash`/`configurationHash`), and a `numericPolicyVersion` tuned for
  manufacturing batch-weight rounding. Taking a network dependency on that remote
  service for every QC field calculation is the wrong tradeoff for something that
  should feel instant in the Test Room, and no existing local expression-evaluation
  library (NCalc or similar) is already a dependency of the backend either — checked,
  not assumed.

  **Formula syntax**, derived directly from the real formulas already documented:
  `{field_key}` references a scalar field's value; `AVG({table_key.column_key})`,
  `SUM(...)`, `MIN(...)`, `MAX(...)`, `RSD(...)` (percent relative standard
  deviation) apply an aggregate over one column of a Table field, referenced by the
  Table's own `FieldKey` and the target column's key; standard `+ - * /` and
  parentheses, normal precedence. The CFU case becomes two `CalculatedValue` fields
  (matching how the real worksheet prints "Average count" as its own row before the
  final result): `average_count` = `({plate1} + {plate2}) / 2`, then `tamc_result` =
  `{average_count} * {dilution_factor}`. The Dissolution case:
  `AVG({dissolution_table.abs_spl}) / AVG({dissolution_table.abs_std}) *
  {std_conc} / {spl_conc} * 100`. A `CalculatedValue` field can reference another
  `CalculatedValue` field's `FieldKey` — this already falls out of "worksheet-scoped,
  not section-scoped" (refinement 5, above), no special case needed.
- **Attachments are embedded mid-worksheet**, not just at the end (e.g. "Attach
  printout of IR spectra" inside an Identification section) — `FileUpload` needs to be
  usable inline within any section.
- **One worksheet execution round legitimately covers many sampling points at
  once** — a real Tablet-area Environmental Monitoring worksheet covers ~90 rooms in
  a single round. This matches the `TestRequest`/`WorksheetInstance` nesting in
  [domain-model.md](./domain-model.md) (a round may cover multiple sampling
  points/subjects) rather than a naive one-`TestRequest`-per-point design.
- **Two flavors of "refer to" exist, not one.** Cross-worksheet references (media
  qualification, above) need the full `ReferencedResult` machinery. A same-worksheet,
  forward reference ("Identification: HPLC — refer to chromatogram from Assay," a
  later section of the *same* document) is a lighter-weight UI jump-link, not a
  cross-instance resolution — kept as a distinct annotation type rather than
  overloading `ReferencedResult`.
