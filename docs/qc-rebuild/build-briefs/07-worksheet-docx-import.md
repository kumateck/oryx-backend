# Build Brief 07 — Worksheet DOCX Import

Status: **ready to build.** Decisions locked 2026-09-24. The feasibility analysis this
brief is based on parsed the raw Word XML of all 49 real documents in the lab's
microbiology corpus. Its findings are summarised in "Corpus facts" below. Read that
section before writing any recognizer.

Follow-on: **Specifications work comes right after this brief.** The importer only
*proposes* Specification characteristics (Phase B). Creating and approving Specifications
from those proposals belongs to that next phase, not this one.

## Objective

Upload one or more ARD (Analytical Raw Data) `.docx` worksheets and get a reviewable
proposal for each file:

- a Draft `WorksheetTemplate`;
- proposed `SamplingPoint`s, for Environmental Monitoring (EM) and water sheets;
- proposed Specification characteristics (carried forward to the Specifications phase);
- equipment and reagent matches;

each with a per-field confidence level and flags. A reviewer corrects the proposal,
previews it in the Test Room, and saves. Nothing is ever approved automatically.

## Locked decisions

1. **EM and water produce a template for a single subject.** In the built system (M3/M6),
   every room or sampling point is its own `TestRequestSubject` with its own
   `WorksheetInstance`. The printed room/point list therefore becomes **proposed
   SamplingPoints**, never template rows. The template holds only one subject's fields:
   - EM: an airborne-viables result in CFU/4Hrs;
   - water: CFU/100 mL, CFU/mL, and four pathogen choices (Absent / Detected).
2. **Model support first** (Phase A below).
3. **Media revisions: the newer format wins.** The newer format has Batch No. / Issue No. /
   Issued By / Format No. / Medium Code. The older Lot No. / Mfg / Expiry / Date Received
   format is reported as `SupersededFormat` and is not imported as a separate template.
4. **Scope is the template plus proposals** (SamplingPoints and Specification
   characteristics).

## Corpus facts (design constraints, not guesses)

- **Families.** Product microbiology ARD (6 files), culture-media qualification (15
  distinct media), EM worksheet (8), purified water (2), and EM COA (8).
- **COAs are completed outputs.** Reject them as a template source, with the flag
  `CompletedOutputNotTemplate`.
- **There are no form controls at all:** 0 content controls, checkboxes, form fields,
  images, text boxes or nested tables. Inputs have to be inferred from:
  - empty cells;
  - dotted or underscored leaders (`……`, `____`);
  - known "X / Y" choice phrases.
- **Merged cells are heavy** (`vMerge`, `gridSpan`), and many tables have two-row headers.
- **Tables are split at page breaks with the header repeated.** Six EM result tables are
  really one list. Stitch consecutive tables whose header rows match.
- **Metadata location differs by family.** Product and EM sheets keep it in the Word
  running header (`word/header*.xml`, any index). Media and water sheets keep it in a body
  table.
- **The files are issued copies that contain run data.** Examples are issue/AR/batch
  numbers, "Issued by", "Sampled by" and dates. This data must **never** become a Constant.
- **Specifications are printed in the sheets**, e.g. "NMT 2000 cfu/g" and "Rooms NMT 100 /
  Dispensing Booth NMT 5". They become proposals, not template content.
- **Formulas are almost never written out.** Only 3 textual formulas exist, and one is
  incomplete. Calculations come from the formula library below, not from the document
  text.
- **`/` is ambiguous.** It separates choices, codes (QCD/EQT), units (cfu/mL), drug names
  (Ampicillin/Sulbactam) and labels (Room/Area). Only phrases in the curated choice
  dictionary become choices.
- **The files are noisy.** There are stray `[` / `[[` paragraphs, stale "Page x of y"
  headers, hyphenation splits ("Pseudo-monas", "Incu-bationtemp."), and label and blank
  joined in one cell ("Plate 1:_____cfu").

## What already exists — reuse, don't rebuild

- `APP/Services/QcWorksheets/StpDocxImportService.cs` already uses the
  `DocumentFormat.OpenXml` SDK, reads all `word/header*.xml` parts, validates uploads
  (20 MB cap) and supports batch import. It shows the house pattern for an importer.
- **Table row headers** (commit `4a3da5ba`, `APP/Repository/QcWorksheets/WorksheetRowHeaders.cs`):
  - A column in `ColumnDefinitions` with `"rowHeader": true` or a `"fixedValues": [...]`
    array is template-owned.
  - `SaveValues` rejects writes to it, and formula aggregates exclude it.
  - The fixed row count equals the number of labels.
  - **Any** column with `fixedValues` is template-owned, so organism name, strain code and
    per-row fixed cells (e.g. incubation period "18 hours" / "72 hours") can each be their
    own fixed column. No schema change is needed for this.
- `QcEquipment.EquipmentId` holds codes like `QCD/EQT/BAL/006`, so equipment can be matched
  by code. `Reagent` has only Name and Description, so reagents are matched by name.
- `SamplingPoint` / `SamplingPointGroup` (M2/M6), with `CanManageSamplingPoints` and
  `CanViewSamplingPoints`.
- `QcFormulaEvaluator` / `QcWorksheetCalculator`: calculated fields are evaluated **and
  persisted** at submit (M3 follow-up).

## Phase A — model support (backend, then frontend)

### A1. Select options (the only real schema gap)

- Add `OptionsJson` (nullable text holding a JSON string array) to `WorksheetField` **and**
  to `WorksheetFieldRevision`. Mirror it through every DTO that carries `ColumnDefinitions`
  today (template detail, instance detail, the revision history, and copy/new-version).
- Validation at template create/update:
  - `Select`, `MultiSelect` and `GrowthObservation` fields require at least 2 distinct,
    non-blank options.
  - Other types must not carry options.
  - Error: `QcWorksheetTemplate.OptionsRequired` / `OptionsNotAllowed`, naming the
    `FieldKey`.
- Validation at `SaveValues` **and** submit:
  - a Select value must be one of the options;
  - each MultiSelect value must be one;
  - error: `QcWorksheetInstance.ValueNotAnOption`, naming the field.
- Table columns can declare `options` inside their `ColumnDefinitions` entry. Apply the
  same value check to table cells.
- Migration: one additive column on each of the two tables. Audit it the same way as
  M1–M6: no `AlterColumn` / `Drop*` against anything else.

### A2. Formalize the fixed-column contract (no schema change)

- Document it in `docs/qc-rebuild/field-catalog.md`: multiple fixed columns are allowed,
  and one of them may be marked `rowHeader` for display.
- Add template validation: every `fixedValues` column in a table has the **same length**
  (that length is the row count). A table that mixes fixed columns and open-ended rows is
  rejected. Error: `QcWorksheetTemplate.FixedRowCountMismatch`.
- Add an optional `group` string on a column definition. Columns that share a group
  render under one spanning header (e.g. "New Batch" → Plate 1 / Plate 2 / Av. /
  Colour). This is display only. Keys stay flat, e.g. `newBatch_plate1`.

### A3. Frontend (oryx-next)

- Template builder:
  - edit the options list for Select, MultiSelect and GrowthObservation fields and for
    table columns;
  - a table-column editor with "fixed values per row" and "row header" flags plus a
    `group` name, **replacing the raw JSON textarea** that M1 left for `columnDefinitions`.
- Test Room:
  - Selects render their options;
  - fixed columns render read-only;
  - grouped columns render a two-row header;
  - `ValueNotAnOption` is shown against the specific field, using `shared/qc-problem.ts`.

### A4. Tests

- Options required, allowed and not-allowed.
- A value not in the options is refused on both save and submit, and nothing is
  persisted.
- A fixed-column length mismatch is refused.
- Column groups round-trip.
- Revision history carries the options.
- Existing templates without options still load (the new column is nullable).

## Phase B — import engine (backend)

### B1. Service

Add a new `APP/Services/QcWorksheets/WorksheetDocxImport/` folder (the house rule is
250 lines per file). Split it by stage:

1. **`DocxDocumentReader`**
   - Produces a normalized model: ordered blocks (Heading, Paragraph, Table) plus the
     running-header text.
   - Resolves each table into a rectangular grid: `gridSpan` is expanded, and `vMerge`
     continuations point at their origin cell.
   - Strips noise paragraphs (only brackets or punctuation).
   - Repairs hyphenation inside known words (organisms and dictionary labels).
   - Stitches consecutive tables whose header rows match.
2. **`ArdFamilyClassifier`** assigns `ProductMicro`, `CultureMedia`, `EnvironmentalMonitoring`,
   `PurifiedWater`, `CompletedCertificate` or `Unknown`. Markers:
   - "ANALYTICAL RAW DATA – MICROBIOLOGY";
   - "ENVIRONMENTAL MONITORING RAW DATA";
   - "ANALYTICAL WORKSHEET" + "WATER";
   - "Culture Medium Name";
   - "CERTIFICATE OF ANALYSIS".
   A certificate or an unknown file stops here with a flag.
3. **Shared primitives** (one class each, each unit-tested against corpus snippets):
   - `ParameterTable` — a 2-column label | value table. A filled value becomes a Constant,
     unless its label is in the run-data dictionary. An empty value becomes an Entry field.
   - `RunDataLabels` — dictionary: Batch No, Lot No, A.R. No / AR Number, Issue No, Issue
     Date, Issued by, Sampled On, Sampled by, Mfg Date, Exp Date, Date Received, Date
     Opened, Medium Batch no, Previously approved batch/lot, Analysis Start/End Date.
     These are always Entry fields or header data, and their values are discarded.
   - `ChoicePhrases` — the curated dictionary of every phrase found in the corpus:
     - Complies / Does not comply;
     - Comply / Do not comply;
     - Absent / Detected;
     - Presence of E. coli / Absence of E. coli;
     - There was / was no growth;
     - did / did not inhibit;
     - was / was not more than factor 2;
     - did / did not produce green pigmentation.

     A match becomes a Select (or GrowthObservation) with those options. Anything else
     containing `/` is text.
   - `DataGrid` — a table with header row(s):
     - fixed-label columns become `fixedValues`;
     - empty cells become Entry columns;
     - a two-row header becomes column `group`s;
     - units come from the header text.
   - `PlateAverage` — the columns Plate 1 / Plate 2 / Av. become two Entry columns plus a
     Calculated `AVG` column. Also the formula library: `cfu = avg × dilutionFactor`.
     The reviewer confirms each.
   - `EquipmentTable` / `ReagentTable` — become Instrument / Reagent fields. Equipment is
     matched on `EquipmentId`; reagents are matched by normalized name. Anything unmatched
     is flagged `UnmatchedEquipment` / `UnmatchedReagent`.
   - `MediaReference` — the phrase "Medium Batch no" + "Refer to Culture Media Batch Data
     sheet serial numbered …" + Remark becomes a `ReferencedResult` pointing at the
     MediaQualification template for that medium, with the batch number as the resolution
     key. If that media template doesn't exist yet, flag `MediaTemplateMissing`: import
     media first.
   - `SignOffTable` — "Analysed By / Checked By / Date" is dropped (review and approval
     cover it).
   - `PrintedSpecification` — "Specification: …", "NMT …", and AREA | SPECIFICATION tables
     become Specification-characteristic proposals.
   - `SamplingPointList` — room/point grids (code + name columns) become SamplingPoint
     proposals (code, name, area = the EM area or water system).
4. **Family recognizers** — `CultureMediaRecognizer`, `ProductMicroRecognizer`,
   `PurifiedWaterRecognizer`, `EnvironmentalMonitoringRecognizer`. Each composes the
   primitives. The media recognizer also detects the old vs new format and emits
   `SupersededFormat` for the old one.
5. **Output** — `WorksheetImportProposal` per file:
   - `family`;
   - `template`: a create-template request shape, ready to POST unchanged;
   - `samplingPointProposals[]`;
   - `specificationProposals[]`;
   - `flags[]`: code, message, location;
   - `fieldProvenance[]`: fieldKey, source location (block/table/row/col), confidence
     High/Medium/Low, reason.

### B2. Endpoint

- `POST api/v{version}/qc/worksheets/templates/import`: multipart with multiple files.
  Returns one proposal per file and **writes nothing**.
- Saving goes through the existing create-template endpoint, then the SamplingPoint
  endpoint for accepted proposals. Specification proposals are returned for the
  Specifications phase.
- Permission: a new dedicated key `CanImportQcWorksheetTemplates`. Follow the granular
  rule and mirror `CanImportQcStp`, including the `SubmoduleFor` routing and the
  coexistence key-list test.

### B3. Golden corpus tests

- Copy the 49 files into `tests/APP.Tests/QcWorksheets/Fixtures/ArdCorpus/`, keeping the
  family subfolders.
- Snapshot-test the proposal for one representative file per family, and assert
  invariants across **all** files:
  - no run-data value appears in any Constant;
  - every Select has options;
  - every COA is rejected;
  - every old-format media file is flagged;
  - EM/water templates contain no room/point rows, and those rows appear as SamplingPoint
    proposals instead;
  - a per-file coverage report is printed (percentage of fields at High confidence).

## Phase C — import review screen (frontend)

- Route `qc/worksheets/templates/import`, following the STP import screen:
  multi-file drop, then one result card per file.
- The review view has the source grid on the left (rendered from the reader's normalized
  model, returned as part of the proposal) and the proposed template on the right, in the
  existing builder. Every field shows its confidence level and flags. Low-confidence and
  flagged fields are listed first.
- **Test Room preview** with sample values, including calculated fields, before saving.
- Save creates the Draft template (normal approval lifecycle). Accepted SamplingPoint
  proposals are created behind `CanManageSamplingPoints`.
- Gated on `CanImportQcWorksheetTemplates`. The sidebar entry sits under QC Worksheets.

## Build order

1. Phase A backend.
2. Phase A frontend.
3. Phase B, in this order: reader + classifier → media → product → water → EM.
4. Phase C.

Media comes first because product and water sheets reference media templates.

## Do not touch

The live Material/Product/Packaging QC system. Existing approval semantics. The
migrations of M1–M6, except for the single additive `OptionsJson` migration in Phase A.
