# Build brief 09: Raw-material chemical worksheets and Specification documents

This brief extends the brief 07 importer and the brief 08 Specification proposals to the
raw-material chemical documents. The corpus is 7 pairs in `~/Downloads/entrance chemical/`
(set by `QC_RM_CORPUS_DIR`; do not commit it, because it contains staff names):

- a worksheet, `NNN - <Material>.docx`, with running header "RAW MATERIAL ANALYTICAL WORKSHEET";
- a Specification document, `NNN <Material> spec.docx`, with running header "SPECIFICATION"
  plus "SPC No.: QCD/SPC/RM/NNN" (Xmox uses "RAW MATERIAL SPECIFICATION" / "Specification No.:").

The numbers are 012, 017, 021, 024, 167, 179 and 193. `NNN` is the **pairing key**.

## Locked decisions (2026-09-26)

1. **Build both** the worksheet recognizer and a Specification-document importer, paired
   by `NNN`.
2. **Specification tests with no worksheet section** (for example Related Substances by
   HPLC, Appearance of Solution, Chlorides, or the capsule shell's microbial tests) get an
   **auto-added section** in the template. The section has a Result field (Entry,
   LongText) plus a FileUpload labelled "Attach print out", and the characteristic binds
   to its Result field. The reviewer can remove it. Each one is flagged
   `AddedForSpecification`.
3. Both importers keep the existing rules: they only propose Drafts, never write run data
   as constants, and require a mandatory review.

## Backend (oryx-backend)

### Classification

- `ArdFamily.RawMaterialChemical = 6`: header contains "RAW MATERIAL ANALYTICAL WORKSHEET".
- `ArdFamily.RawMaterialSpecification = 7`: header contains "SPECIFICATION" and an
  "SPC No." / "Specification No." with `/RM/`.
- Check both **before** the brief 07 "ANALYTICAL WORKSHEET (chemical)" Unknown fallback.
  Keep that fallback for finished-product chemical worksheets, which are still
  unsupported.

### Worksheet recognizer (`RawMaterialChemicalRecognizer`)

- **Template:** `Category = Chemical` and `Code = "RM-NNN"`. `NNN` comes from the filename
  prefix or the A.R./Spec number, and the reviewer confirms it.
- **Sections:** one per numbered test row ("1.", "2." …). Title the section with the test
  name, stripped of trailing "Instrument ID:" / "Balance ID:" / "Equipment ID:".
- **Instrument fields:** each "Instrument/Balance/Equipment ID" label becomes an
  Instrument field, matched by the existing equipment matcher.
- **Label/blank lines:** "Weight of … taken: ____ g" becomes Number with unit g.
  "Preparation:", "Observation:", "Inference:", "Test solution:" and "Reference
  solution:" become LongText Entry fields. The Solubility "Water: / Ethanol:" parts
  become one ShortText Entry each.
- **"Attach Print Out"** becomes a FileUpload.
- **Printed formulas.** The "W1/W2/W3" crucible pattern (Sulfated Ash, Loss on Drying)
  becomes three Number entries plus a Calculated result, using the printed formula
  (`(W2-W3)/(W2-W1)*100` or as printed). Flag `FormulaFromPrint` with Medium confidence.
- **Titration grid** (Blank / Sample 1 / Sample 2 × Wt taken, Final vol, Initial vol,
  Titre):
  - It becomes a Table with fixed row labels.
  - Titre is a per-row calculated column, final − initial.
  - "Factor of Volumetric Solution" becomes a Number Entry.
  - "Equivalence = …" becomes a Constant.
  - The Assay result becomes a Calculated field with an **empty formula** and the flag
    `FormulaNeedsReview`. It is never guessed.
- **Unprinted calculations**, where "Calculation:" is left blank (optical rotation,
  HPLC assay): add a Calculated field with an empty formula and `FormulaNeedsReview`.
- **Sign-off rows** ("Analysed by / checked by") are dropped.
- **Header run data** is dropped, using the brief 07 run-data label dictionary.

### Specification-document importer (`RawMaterialSpecificationRecognizer`)

It produces **no template**, only `SpecificationProposals` plus header metadata:
- `SpecificationCode` = the SPC number;
- `ProductName` = the material name;
- `Revision`;
- `Reference` per row (BP 2025 / USP / In-House).

Rows:
- **Top-level row:** one characteristic, with `TestName`, `AcceptanceCriteria` = the
  specification text, and `ActionLimit` = the same text.
- **Rows with sub-tests** (Identification → IR / Chlorides / Optical rotation; Related
  Substances → named impurities; Microbial Contamination → TAMC / TYMC / organisms):
  one characteristic each. The parent is the `GroupName`-free test prefix and the child
  is the `Analyte`. For example: TestName "Related Substances", Analyte "Impurity E",
  criteria "NMT 0.30%".
- **Microbial rows** get `GroupName = "MICROBIAL"`; everything else gets `"CHEMICAL"`.
- **Layout robustness:** handle the numbered-column layout (Xmox), the three-column
  layout, and Levofloxacin's second table. Anything it can't split is flagged
  `UnrecognizedContent` and kept as one row.

### Pairing and binding

When a spec and its worksheet are in the same upload, or a template with code `RM-NNN`
already exists:
- Bind each characteristic to the worksheet field whose section test name matches,
  using case-, spacing- and punctuation-insensitive matching with a small synonym list
  (Sulphated/Sulfated, Identity/Identification, Loss on drying/LOD).
- Bind to the section's Result or Calculated field.
- Unmatched characteristics trigger decision 2: the recognizer adds the Result + Attach
  section to **the worksheet proposal in the same upload**, and the spec characteristic
  gets that field key.
- If the template already exists, don't modify it. Flag `FieldNotOnTemplate` instead,
  since the reviewer must revise the template.
- A spec with no paired worksheet anywhere is flagged `WorksheetNotFound` ("import
  NNN's worksheet first").

### Brief 08 integration

- Allow family `RawMaterialSpecification` in `POST specification-proposals`.
  `worksheetTemplateId` is the paired `RM-NNN` template.
- The draft plan for this family gives:
  - `AppliesTo = RawMaterial` and `Stage = null`;
  - Code and Name from the spec header;
  - a Chemical link to the RM template;
  - characteristics as proposed, in document order.
- One set per plan, as with products.
- Characteristics with `GroupName = "MICROBIAL"` bind to the auto-added sections in the
  same chemical template. The Specification keeps a single Chemical link.

### Tests

- Classification of all 14 files.
- Per-worksheet section and field snapshot assertions: test count, instruments, titration
  table, the W1/W2/W3 calculated field.
- No run data in constants.
- Spec parsing for all 7, including the Levofloxacin impurities, the Maize microbial
  sub-rows and the Xmox numbered layout.
- Pairing and binding.
- Auto-added sections.
- The draft plan for `RawMaterialSpecification`.
- Synthetic fixtures when `QC_RM_CORPUS_DIR` isn't set.

## Frontend (oryx-next)

- **Import screen:**
  - Show the two new families.
  - For a spec file, show its characteristics and the pairing status (paired to the
    worksheet in this upload, paired to an existing `RM-NNN`, or missing).
  - Save the worksheet(s) first, then post the spec's proposal set with the resolved
    template id.
  - A spec file has no template save of its own.
- **Proposals page:** add a "Raw material" family group. The review screen hides Stage
  when `appliesTo = RawMaterial`.
- **Flags:** `AddedForSpecification`, `FormulaNeedsReview` and `FormulaFromPrint` get
  readable labels.

## Out of scope

- Finished-product chemical worksheets.
- Linking a Specification to the materials master.
- Guessing HPLC or titration assay formulas.
