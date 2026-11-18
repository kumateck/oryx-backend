# Template repair and QC follow-ups — 18 November 2026

Prepared in the existing isolated `claude/template-repair` worktrees. Main checkouts and live customer data were not changed.

## Template repair

ARDs reference their template Form directly. Worksheet visibility and saving are restricted to FormFieldAssignee rows created when the test was assigned. Adding a template field later does not create these rows, so nobody can enter the new field on an existing worksheet.

The Templates row menu now opens **Repair template**. Select missing fields on open product, raw-material GRN or packaging/retest worksheets; choose an active analyst already on each test; preview the exact additions and saved-answer counts; enter a reason; apply. Existing assignments and answers remain unchanged.

The server rebuilds the reviewed plan inside a serializable transaction and write locks. The preview hash includes selected assignments, live field/question metadata, worksheet state and saved-answer hashes. A stale preview is refused. Exact retries return the original receipt. Repair evidence and assignment history are append-only in EF and PostgreSQL.

Closed, checked, approved, rejected, submitted and governed worksheets are blocked. Superseded material samples and inactive analysts are blocked. Product labels are read without department joins that would otherwise hide another department's ATR from QC.

## Permissions and endpoints

New keys are registered once under **Quality Control / Testing**:

| Key | Endpoints | Initial grants |
| --- | --- | --- |
| `CanViewTemplateRepair` | GET `template-repairs/forms/{formId}`, POST `.../preview`, GET `.../history` | Granted to nobody |
| `CanApplyTemplateRepair` | POST `template-repairs/forms/{formId}/apply` | Granted to nobody |

Each selected test additionally needs its existing `CanReassignProductTest`, `CanReassignRawMaterialTest` or `CanReassignPackagingMaterialTest` key, chosen from the stored record kind. The desktop route, menu, visibility and immediate action checks use the same server keys. No role-name bypass or automatic grant is added.

Partial reassignment has three named-permission endpoints: POST `test-field-reassignments/{assignmentId}/product`, `/raw-material` and `/packaging-material`, requiring the corresponding existing reassignment key. The request identifies each assignment row, its reviewed current owner and its new analyst. Transfers are atomic; unchanged questions stay with their analysts; questions with saved answers cannot transfer. New analysts must be active and have the specific test-start permission. Product ATR analyst lists are synchronized. The existing request audit records the reason and reviewed old/new owners.

Product COA generation explicitly requires `CanCheckProductTest`; grants are reloaded inside the transaction before later writes. Packaging authoring retains its existing Production/QA per-section Add/Edit/Delete keys and compares substitute quantities, including full decimal precision, when deciding which task permissions are needed. It rechecks current grants before deleting/replacing the list and before saving.

## QC review and certificates

The specification/COA summary uses the stored authoritative average when one exists; otherwise it preserves the available recorded results. The detailed worksheet retains all sample results, question instructions and saved formula inputs, including weights, manual values and reading tables. Governed results are not recalculated for display.

Choice questions now participate in the summary. A saved short-answer value can match a later dropdown option (including a value containing commas); the matching option is shown and restored in the entry form. Nonmatching historical values are still displayed rather than erased.

The product worksheet and COA now use `TestedAt` for the analysis date; the former sampling-date fallback caused receipt to appear later than analysis. COA generation requires recorded sampling/receipt/analysis dates, refuses future calendar dates and enforces sampling <= receipt <= analysis. This does not rewrite historical dates.

Certificate signatures identify the saved answer authors as **Analysed by**, `Response.CheckedBy` (the COA generator) as **Released by**, and the final required approver from the current approved round as **Approved by**. Previewing does not create a signature or approve anything.

## Packaging substitutes and allowance

A packaging substitute can now store its own positive full-batch `PrescribedQuantity`, in its department stock unit. Production requirements and material choices use that quantity. Existing null quantities keep the legacy main-line quantity until explicitly edited. Raw-material substitutes retain their existing independent quantities. Both editable lists and product detail lists show substitute names and quantities.

Packing allowance controls/columns are hidden. Per the final user instruction, new and edited rows save allowance and loose quantity as zero; untouched saved rows preserve their prior allowances. No mass update of existing allowances is performed.

## Deployment and checks

Apply `20261118115522_AddTemplateRepairs` and `20261118123610_AddPackagingSubstituteQuantities` and deploy the backend before activating the desktop. Assign the two new repair permissions explicitly to intended users. Neither migration grants them automatically.

The companion desktop imports only the seven new operations, owned request/response schemas and the additive read-only provenance/substitute fields. The contract regression removes exactly these additions and reproduces its approved predecessor. Existing release fingerprints remain accepted; source compatibility acceptance is not a deployment.

Focused server permission/business tests, PostgreSQL migration/locking/history tests, UI permission/retry/display/quantity tests, route coverage, API generation checks and the web production build are recorded in the completion report. The PostgreSQL fixture uses `ORYX_TEMPLATE_REPAIR_TEST_CONNECTION` and creates/drops only its own disposable databases. No customer migration or repair was executed.

Verified locally: 72 focused backend tests plus the PostgreSQL migration/lock/history test; 46 focused UI regressions; 30 API-contract/cache tests; 14 module/route tests and 408 route coverage/access tests. Web production build, strict TypeScript, reproducible generated APIs and EF pending-model check passed. The final dropdown/parser cleanup passed another 20 focused tests. React Doctor reported no issues on the changed files. Certificate fixtures were inspected at 1280×720 and 1366×768 with no page overflow. These are focused checks, not a claim that the entire repository test suites were run. Temporary verification servers were stopped.
