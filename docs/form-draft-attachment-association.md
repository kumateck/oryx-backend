# Form draft attachment association

## Concurrency correction (2026-10-25)

New FileUpload and Signature field answers are explicitly registered as `Added`
in the shared EF context before `FileRepository.SaveBlobItem` flushes it.
Assigning a stable UUID alone could make EF treat the new answer as an update,
which produced the observed zero-row concurrency exception for image drafts.
The request and response contract is unchanged. The regression test flushes
the context from the file repository stub to exercise the real save order.
Documentation updated: this note, `docs/services.md`, and `docs/workflows.md`.

FileUpload/Signature draft answers now receive an explicit stable FormResponse
ID before SaveBlobItem captures its model ID and blob path. Previously a newly
created field answer could still have Guid.Empty at that boundary, leaving the
attachment unrelated to the eventual persisted result field. Later uploads reuse
the existing field ID. Storage failures are propagated to the caller rather than
being ignored by the draft action.

The branch is extracted into APP/Services/Forms/FormDraftAttachments.cs. Existing
context, assignment, revision, finalization and approval checks in
SaveFormResponseDraft remain in place. Request bodies and the returned response
ID are unchanged; attachment reads still use their existing model/reference
paths. No status, inventory, release, or approval is inferred from a saved draft.

Frontend fixes preserve attachments in stored answer models and acknowledge the
submitted file fingerprint, allowing the saved check to update before refetch
and attachment links to survive reopening. Final submission skips acknowledged
file drafts. Both backend and frontend deployment are required; no migration is
needed.

Read-only production diagnostics found five existing attachments with empty model
IDs among 15 active FormResponse attachment records. No live data was repaired.
Recovering those records requires confirming their batch/test/field destination;
this change does not guess associations or reassign regulated result evidence.

Tests cover first and subsequent FileUpload/Signature draft field IDs, storage
failure propagation and existing response-context/revision guards. Frontend
regressions cover immediate acknowledgment, no duplicate submit upload, persisted
links and edits during upload. Documentation updated: this note, services and
workflows in both backend and frontend repositories.

## Follow-up investigation (2026-10-24)

The supplied GRN batch URL resolves to Maize Starch, BT/2310/001, whose form has
no FileUpload or Signature questions. The recent orphan PDF instead belongs to
Diclofenac Sodium, BT/2310/002, under the same GRN. Its destination is the
"Attach Print Out" field (019e2b47-c922-72a1-8605-f93e230b38e1), answer
01a16d32-a8fa-7d98-8a1e-0f9f14ebbad0.

Request audit entries confirm uploads at 09:16:39 and 09:16:53 UTC to batch
01a16ca6-bdac-7bf2-8e71-39a99d1b93e6. The original orphan PDF and the correctly
linked 09:16:53 copy are identical: 211,549 bytes, SHA-256
9d8ce451b9419e4ef6aa226be453a53b40bde14d0e212fb8138ab656bebb7f21.
A further linked copy exists at 09:19:01. These existing evidence links avoid
any need to reassign the orphan for this incident. No live records, objects,
workflow states or approvals were changed. Frontend attachment preservation is
still required for the linked files to appear in the result-entry interface.

Audit-log reads used the existing MongoDB _id index after a bounded timestamp
query timed out; queued log insertion time can differ from request CreatedAt.
File contents and request bodies were not printed; byte counts and checksums
were compared in memory.
