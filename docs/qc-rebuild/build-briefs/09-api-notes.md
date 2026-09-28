# Brief 09: API notes for the frontend (backend branch `qc-rm-chemical-import`)

This note covers what changed on the wire for raw-material chemical worksheets and
Specification documents. JSON is camelCase and **enums are numbers**. Nothing changed
in the existing endpoints' routes or permissions.

## New enum values

| Enum | New values |
|---|---|
| `ArdFamily` (`family`) | `RawMaterialChemical = 6` (the worksheet), `RawMaterialSpecification = 7` (the Specification document) |
| `WorksheetFieldType` | `FileUpload = 25`: an inline attachment ("Attach Print Out"). Its value is whatever attachment reference the client stores, and it is an Entry field, so submit requires it like any other Entry field. The Test Room and template builder need to render it. |
| `RawMaterialPairingStatus` (new) | `Missing = 0`, `InUpload = 1`, `ExistingTemplate = 2` |

`SpecificationAppliesTo.RawMaterial = 0` and `SpecificationAnalysisType.Chemical = 0` already existed.

## `POST qc/worksheets/templates/import`: `WorksheetImportProposal`

There is one new property, **`rawMaterial`**. It is null for the microbiology families.

```json
"rawMaterial": {
  "pairingKey": "012",                 // NNN: from the file-name prefix, else the printed Spec./SPC number
  "templateCode": "RM-012",
  "materialName": "CIPROFLOXACIN HCL",  // worksheet: "Raw Material Name"; spec: the title after the SPC number
  "specificationCode": "QCD/SPC/RM/012",// spec documents only
  "revision": "05",                     // spec documents only
  "pairing": 1,                         // spec documents only: RawMaterialPairingStatus
  "pairedFileName": "012 - Ciprofloxacin HCl.docx", // when pairing = InUpload
  "pairedTemplateId": null,             // when pairing = ExistingTemplate
  "pairedTemplateCode": null
}
```

### Worksheet file (`family 6`)

- `template` has `category 0` (Chemical), `code "RM-NNN"`, and name
  "Raw Material Analytical Worksheet – {material}". Ask the reviewer to confirm the code
  (see the `MissingMetadata` flag).
- `specificationProposals` is always empty. The characteristics come from the spec file.
- When its spec document is in the same upload, the worksheet may carry **extra sections
  at the end** that were added for Specification tests the sheet does not record (locked
  decision 2). Each one has one or more `LongText` Entry result fields and a final
  `FileUpload` field labelled "Attach print out". Each added section is flagged
  `AddedForSpecification`. If the reviewer removes one, the spec characteristics bound to
  its fields lose their `sourceFieldKey`.
- A calculated field can arrive with `formulaExpression: null` (flag `FormulaNeedsReview`).
  Template save refuses an empty formula, so the reviewer must enter one first.

### Specification file (`family 7`)

- `template` is **null**. There is no template save for this file.
- `specificationProposals[]` has the existing shape plus **`reference`** (string: "BP 2025",
  "USP", "In-House"). Values per row:
  - `testName` is the printed test ("Related Substances"). A sub-test or impurity is in
    `analyte` ("Impurity E", "IR", "TAMC"). Levofloxacin's procedures read like
    "N-Desmethyl levofloxacin (Procedure 1)".
  - `acceptanceCriteria` = `actionLimit` = `printedCriteria` = the printed text. Superscripts
    are written as `^`, so "10³ CFU/g" reads "10^3 CFU/g".
  - **`groupName` is the COA heading, `"CHEMICAL"` or `"MICROBIAL"`.** For this family it is
    *not* a sampling-point limit tier.
  - `stage` is null. `productName` / `specificationCode` come from the SPC header.
  - `sourceFieldKey` is the bound worksheet field (see pairing below).
    `sourceWorksheetTemplateId` is set only when the pairing is `ExistingTemplate`.
- Pairing, shown on the import screen from `rawMaterial.pairing`:
  - `InUpload (1)`: bound to the worksheet in this upload (`pairedFileName`). **Save that
    worksheet first**, then POST this file's proposal set with the new template's id.
  - `ExistingTemplate (2)`: bound to the saved RM-NNN (`pairedTemplateId`), which is never
    modified. Unmatched rows have `sourceFieldKey: null` plus a `FieldNotOnTemplate` flag.
  - `Missing (0)`: flag `WorksheetNotFound` ("import NNN's worksheet first"). No row is bound.

## New flag codes (`flags[].code`)

| Code | On | Suggested label |
|---|---|---|
| `AddedForSpecification` | worksheet and spec | "Section added for a Specification test" |
| `FormulaFromPrint` | worksheet | "Formula read from the printed sheet – confirm" (Medium confidence) |
| `FormulaNeedsReview` | worksheet | "Formula not printed – enter it before saving" (field confidence Low) |
| `FieldNotOnTemplate` | spec | "No matching field on the saved template – revise the template" |
| `WorksheetNotFound` | spec | "Import this material's worksheet first" |

`UnrecognizedContent` is also raised on a spec row whose sub-tests and criteria lines can't
be paired (for example, Ciprofloxacin's "IR / Chlorides" over a single sentence). That row
is kept as one characteristic.

## `qc/specification-proposals` (brief 08 contract)

- `POST /` now accepts `family: 7`. Send `worksheetTemplateId` = the saved `RM-NNN`
  template (just saved from this upload, or `rawMaterial.pairedTemplateId`),
  `productName` = `rawMaterial.materialName`, and `specificationCode` =
  `rawMaterial.specificationCode`. Post `specificationProposals` through unchanged.
  `samplingPointGroupProposals` / `samplingPointCodes` are empty.
- `POST /draft` for family 7 (exactly one set, otherwise `QcSpecificationProposal.ProductSingleSet`):
  - `appliesTo 0` (RawMaterial), `stage null`, `code` = the SPC number, `name` = the material;
  - one worksheet link with `analysisType 0` (Chemical);
  - characteristics in document order. Each row's `groupName` is `CHEMICAL` or
    `MICROBIAL` as proposed. `samplingPointGroupName` is always null. `groups []`.
  - MICROBIAL rows bind to the sections added to the same chemical template, and the
    Specification keeps its single Chemical link.
- The proposals page needs a "Raw material" family group. The review screen should hide
  Stage when `appliesTo = 0`.
- The `FamilyNotSupported` / `ProductSingleSet` messages now mention raw-material documents.
  The codes are unchanged.
