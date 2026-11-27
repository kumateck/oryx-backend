# STP revisions (structured STP content)

Date: 2026-11-27. The client's standard test procedures (Entrance QC, "STANDARD TEST PROCEDURE" Word files) as structured content with revisions. The approved revision is the STP's content of record. The model and flow copy the [approved vendor list](approved-vendor-list.md).

## Model

- `StpRevisions`: kind (`RawMaterial` 0, `PackagingMaterial` 1, `Product` 2), STP number, revision number, Supersedes, the revision it was revised from (`RevisedFromId`), status, `Version`, reason for change, the running-header fields (title, area, reference, effective / review / issue dates), the source (an imported file, a document version, or "Revision n"), and the deletion reason.
  - A revision belongs to the STP **number**. One number can cover several materials or products (`MaterialStandardTestProcedure` / `ProductStandardTestProcedure` rows share it). The number must exist on an STP record of the same kind; for materials the kind comes from the material.
  - Status is Draft (4), Pending (0: in review or awaiting approval), Approved (1: current), Superseded (2) or Rejected (3).
  - Filtered unique indexes per (kind, STP number): one Approved (`IX_StpRevisions_Current`) and one in preparation, Draft or Pending (`IX_StpRevisions_InPreparation`).
  - `Version` goes up on every save. Saving a draft, submitting it and correcting it in review all require the version the caller read. A stale version is refused (`StpRevision.Stale`).
- `StpRevisionSections`: Purpose, Scope, Responsibility, Accountability (1.0 to 4.0), Reagents (one reagent per line), and ProcedureNotes (unnumbered text between the reagents and 5.1, such as buffer preparation or "refer to QCD/SOP/003").
- `StpRevisionTests`: the 5.N tests in order. Each has a printed number, a title and a **`TestKey`**: lower-case words and dashes, unique within the revision. A test keeps its key when it is renamed or renumbered, and a revision copies the keys. A new test gets a key from its title that no earlier revision of the STP used for another test. Step 4 links specification tests to these keys.
- `StpRevisionTestSteps`: in order, each a `Step` (5.N.M, with its printed number), a `Conditions` block (heading such as "Chromatographic Conditions" and one condition per line) or a `Note`. A step may name the SOP or STP it refers to (`ReferenceCode`).
- `StpRevisionApprovals`: approval stages.

## Flow

1. **Import** (`POST stp-revisions/import?kind=`, multipart `file`) reads a .docx into a proposal. **Nothing is saved.** `POST stp-revisions/import/document` (`{ kind, stpId }`) reads the current version of the STP's controlled Word document (`StpDocument`: the effective version, otherwise the latest draft) the same way. The document itself is not changed.
2. **Create** (`POST stp-revisions`) starts the first revision of an STP number as a draft, blank or filled from a proposal. It is refused if the number already has an approved revision or a revision in preparation.
3. **Revise** (`POST stp-revisions/revise`, `{ kind, stpNumber, revisionNumber, reasonForChange }`) starts a draft from the approved revision with a copy of its content and test keys. The revision number must be higher, Supersedes is the current revision number, and a reason is required. The approved revision stays current until the new one is approved.
4. **Save draft** (`PUT stp-revisions/{id}/draft`) replaces the header and content.
5. **Submit** (`POST stp-revisions/{id}/submit`) needs a title, sections 1.0 to 4.0 and at least one test, each with a step. The revision becomes Pending and `ApprovalRepository.CreateInitialApprovalsAsync` opens the document review ("Checked by"). An STP revision is always reviewed.
6. **Correct** (`PUT stp-revisions/{id}`) while the review is awaiting review or returned. The edit is noted on the review. A returned revision goes back for review through `document-reviews/.../resubmit` with the edit key.
7. Confirming the review starts the approval engine (`StpRevisionApprovalHandler`). Without a configured "StpRevision" workflow the revision is approved automatically. The final approval makes it Approved and the revision it came from Superseded. If the approved revision changed in the meantime, the approval is refused (`StpRevision.Current`). A rejected revision stays rejected; prepare a new one.
8. **Delete** (`DELETE stp-revisions/{id}?reason=`) works for any status. A draft needs a reason. The revision leaves lists and queues; its content, reason, actor, review and approval history are kept. Deleting a revision in preparation frees the slot.

Reads: `GET stp-revisions?kind=&stpNumber=` (revisions, newest first), `GET stp-revisions/numbers?kind=` (every STP number of the kind with its current and in-preparation revision), `GET stp-revisions/{id}` (content, versions, Prepared / Checked / Approved by).

## Import

`StpDocxImportService.Propose` reads the file with `DocxDocumentReader` (tables as grids with merged cells resolved, page-split tables stitched, cell paragraphs kept as lines, nested tables) and `StpDocxContentReader` interprets it. It keeps the parser spike's rules: the metadata is in a running-header part (any of them), there is no space after a section number ("1.0Purpose :"), and headings are matched on the text, not on fixed positions.

- Header: STP No. (the code only), title (the text after the code), Revision No., Supersedes, Area, Reference, Effective / Review / Issue Date.
- The body is a two-column table (number | text). The number column decides what a row is: 1.0 to 5.0 a section, 5.N a test, 5.N.M (and deeper) a step. An unnumbered row inside a test is split at label lines ending in ":" into conditions blocks (chromatographic, mobile phase, apparatus, column, gradient) and notes. Loose paragraphs with a leading number are read the same way.
- Reagents: the lines after "Reagents" up to a label, a "refer to" line, a "Label: sentence" line or a long line. Several reagents printed on one line are split at the dashes (Medium confidence).
- Every header field, section, test and step carries a confidence (High / Medium / Low) and a flag saying what to check: a missing header value, a section not found, reagents split at dashes, unnumbered text before 5.1, conditions inferred from wording, a nested table read as lines, a step number printed without its point ("5.73" read as 5.7.3), a repeated step number, a step numbered under another test, and skipped test numbers (a warning).
- Against what is stored: an STP number not known for the kind is Low; a test whose title matches a test of the approved revision takes that test's key.
- A file without the 1.0 to 5.0 layout (an analytical worksheet that only cites an STP number) is refused with a reason.

Fixtures: nine of the client's STPs in `tests/APP.Tests/StpRevisions/Fixtures` (footer text and document properties blanked: they held staff names). The four other files in the client's "entrance stps" folder are analytical worksheets, not STPs, and are not committed (they carry staff names); a synthetic worksheet covers that case.

## Permissions

One key per task and kind; the kind of an existing revision is read from the stored record. `StpRevisionPermissionAttribute` checks the key before the action, and the service checks it again, from current grants, before each write. All are registered under Quality Control → Material STP (raw and packaging) or Product STP. **All are granted to nobody** by the migration; assign them explicitly.

| Task | Raw material | Packaging material | Product | Endpoints |
| --- | --- | --- | --- | --- |
| View | `CanViewRawMaterialStpRevisions` | `CanViewPackagingMaterialStpRevisions` | `CanViewProductStpRevisions` | list, numbers, read; review read |
| Create | `CanCreateRawMaterialStpRevision` | `CanCreatePackagingMaterialStpRevision` | `CanCreateProductStpRevision` | create |
| Import | `CanImportRawMaterialStpRevision` | `CanImportPackagingMaterialStpRevision` | `CanImportProductStpRevision` | import, import from document |
| Save draft | `CanEditRawMaterialStpRevisionDraft` | `CanEditPackagingMaterialStpRevisionDraft` | `CanEditProductStpRevisionDraft` | save draft |
| Submit | `CanSubmitRawMaterialStpRevision` | `CanSubmitPackagingMaterialStpRevision` | `CanSubmitProductStpRevision` | submit |
| Correct | `CanEditRawMaterialStpRevision` | `CanEditPackagingMaterialStpRevision` | `CanEditProductStpRevision` | correct; review resubmit |
| Revise | `CanReviseRawMaterialStpRevision` | `CanRevisePackagingMaterialStpRevision` | `CanReviseProductStpRevision` | revise |
| Review | `CanReviewRawMaterialStpRevision` | `CanReviewPackagingMaterialStpRevision` | `CanReviewProductStpRevision` | review confirm |
| Return | `CanReturnRawMaterialStpRevision` | `CanReturnPackagingMaterialStpRevision` | `CanReturnProductStpRevision` | review return |
| Approve | `CanApproveRawMaterialStpRevision` | `CanApprovePackagingMaterialStpRevision` | `CanApproveProductStpRevision` | approval decide (checked in `StpRevisionApprovalHandler`) |
| Reject | `CanRejectRawMaterialStpRevision` | `CanRejectPackagingMaterialStpRevision` | `CanRejectProductStpRevision` | approval decide |
| Delete | `CanDeleteRawMaterialStpRevision` | `CanDeletePackagingMaterialStpRevision` | `CanDeleteProductStpRevision` | delete |

`ApprovalController.DecisionKey("StpRevision")` is null because the key depends on the stored kind; the handler checks it for the deciding user.

## `StpDocument`

The controlled Word document (ONLYOFFICE, SHA-256 versions, signed review and approval) is unchanged and read-only from here: it is history and an import source. Whether to retire its signed workflow for STPs that have structured content is still open.

## Not in this step (step 4)

- Word export (`GET {id}/docx`) and print (`GET {id}/print`), with Export and Print keys; storing the approved revision's generated .docx with its SHA-256.
- The specification ↔ STP test link (`FormSection.StpTestKey`) and showing it on the ARD.
- The import → export → import round-trip test.
