# Issued BMR records: typo corrections and date revisions

The dedicated list contains only BMRs with a recorded issue date, by product batch.
It is separate from the BMR/BPR request-and-issue page. Search by batch number,
product name or code. Retired/cancelled history remains visible but cannot be edited.
Production actors are scoped to their own department; other assigned departments
still need the explicit grants below. Current permissions are checked for every
read/write and rechecked before sensitive writes.

| Permission | Allows |
| --- | --- |
| `CanViewIssuedBmrRecords` | List issued records and read their correction/revision history |
| `CanCorrectIssuedBmrBatchNumber` | Correct batch-number text, with required reason |
| `CanRequestIssuedBmrRevision` | Propose manufacturing/expiry date changes, with required reason |
| `CanApproveIssuedBmrRevision` | Approve or reject another person's pending date revision, with review reason |

All actions require list access too. There is no implicit administrator bypass,
and requesting and approving the same revision is forbidden. Grants are published
under Quality Assurance / Issued BMR Records; they are not assigned automatically.

Typo correction changes only the batch number. Dates always require a revision;
requesting one leaves all live dates unchanged. Approval updates the batch, while
rejection changes only the revision history. One pending revision per BMR is
allowed. Date revisions retain previous values and must still match the live
dates when approved. The legacy issue-screen update endpoint refuses already
issued BMRs, so it cannot bypass these controls or change their quantities.

An applied change updates the BMR, live paired BPR, schedule product, related
analytical-request dates, live return-note number, requisition-linked bin-card
reference, QC subject reference, linked OOS batch reference, matching work-order
reference and unissued draft certificate headers. Finished-goods distributions,
transfers, product bin cards and batch reports read the corrected BMR through
their existing IDs. No stock quantity, test result, release/approval status or
issue date/issuer is changed. No unrelated batch is matched by batch number alone.

Issued/superseded certificates retain their frozen headers and results. The user
explicitly selected their existing certificate revision workflow for corrections;
previously printed/exported artifacts remain historical copies.

Before/after values, actor/time/reason and affected record IDs are stored in
`IssuedBmrChanges`. Requested dates, requester, reviewer, decision and reasons are
retained in `IssuedBmrRevisions`. Propagation, revision decision and audit save in
one transaction under the production write locks. A failed audit insert rolls
back all linked changes; concurrent approval can apply a revision only once.

Deploy migration `20261103160129_AddIssuedBmrCorrections` before the matching API/UI.
It creates only correction-audit and revision tables. Do not remove these tables
after use: they retain the evidence supporting live batch corrections. This work
does not apply the migration or alter any customer stock. White Placebo is excluded.

PostgreSQL correction tests use `ORYX_ISSUED_BMR_CORRECTION_TEST_CONNECTION` and
accept only a loopback database named `oryx_issued_bmr_test`. That disposable
database is separate from the historical report and material-repair fixtures.
