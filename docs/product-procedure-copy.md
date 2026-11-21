# Product procedure copying and ATR stages

Copying a saved procedure (to/from products in the same or another department)
retains each responsible user's and role's instruction, including ATR. The request
retains the source analytical raw data reference instead of clearing it. Before
replacing the target procedure, the server resolves each foreign ARD reference to
the target product's own ARD with the same test stage and analysis type.

Intermediate, Bulk and Finished selections therefore remain selected on the
copied procedure using the destination product's quality document. Chemical and
Microbial documents are matched separately. Copying does not duplicate or alter
analytical raw data, STPs, forms, approvals or test results.

If a source ATR has no selected ARD, its document is unavailable, or the target
has no matching stage (or multiple candidates), that product is reported as failed
and its existing procedure is preserved. Configure the missing target quality
document or resolve the ambiguous stage before retrying. Results are reported per
target; one failed target does not hide successful copies to other selected products.
The copy review shows the assigned users/roles, instructions and ATR stage labels.
The existing ATR editor requires the analytical raw data/stage selection.

The existing procedure-save endpoint is reused; no API shape change, new endpoint,
permission, database migration or data repair is required. Production pages use
Production procedure permissions and QA pages use QA procedure permissions. The
server reloads the actor's grants inside the replacement transaction immediately
before writing. Add/edit/delete requirements come from the actual target changes.
Procedure replacement and associated rows are saved atomically, so a failed write
cannot leave the destination with its previous procedure deleted.

Validation is covered by `ProductProcedureCopyTests`, existing product permission
and deleted-product tests, and the desktop procedure mapping, copy-hook and editor
selection tests. The optional PostgreSQL test uses
`ORYX_PROCEDURE_COPY_TEST_CONNECTION` to create/drop its own disposable database;
it executes the real stage-resolution query and deliberately fails an insert to
verify that the prior procedure and assignments survive rollback.

Verified on 21 November 2026: 26 focused backend/permission/deleted-product checks
passed; a second run including permission-catalog checks and the real PostgreSQL
rollback test passed all 22 checks. Desktop mapping/copy/editor suites passed all
17 checks, including both copy directions, denied writes, per-target stage errors
and the preselected Bulk document in the editor. TypeScript and API generation
checks passed. React Doctor reported no issues after correcting the review list's
stable keys. No application database was migrated and no deployment was performed.
