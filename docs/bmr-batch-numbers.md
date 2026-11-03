# BMR batch number uniqueness

Assigning or editing a BMR number rejects any other BMR with the same number,
including another product or department. Surrounding spaces and letter case do
not create a different number. A record may keep its own number. Saves trim the
number and keep the BMR, paired BPR and schedule batch consistent. Unnumbered
placeholders created when production starts are permitted; QA must supply a
number before issue.

A rejected save reports `Bmr.DuplicateBatchNumber` and “Batch number … already
exists”, without changing the documents or production step. The client retains
the entered details and does not call issue after a rejected save. Issue rechecks
uniqueness and cannot issue a retired BMR.

Reuse is permitted only for a retired BMR whose production is cancelled and has
an active, matching full material return note with full-return lines and no
partial-return lines. This is the **full return activated** state; warehouse
receipt need not already be completed. Cancellation/deletion alone, an empty
return draft or a partial return does not release the number. The previous BMR
and return history remain intact. Once reassigned, the new BMR holds the number.

Creation, editing, issue and full-return activation share the production
transaction's PostgreSQL write locks, including BMR/BPR and return-line tables.
The duplicate check and write are atomic across independent sessions. No schema
migration or automatic renumbering is performed; existing legacy duplicates
remain visible in history and must be reconciled before they can be issued.
