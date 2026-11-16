# Document review (the "Checked by" step)

Date: 2026-11-16. Status: phase 1 built (purchase orders, purchase requisitions, stock requisitions). Not yet verified against a deployed server.

## What it is

A review is a first-level check of a prepared record before it goes for approval, so that typos and
wrong information are corrected before approvers see it. On prints and reports:

| Line | Who |
| --- | --- |
| Prepared by | the person who created the record |
| Checked by | the person who confirmed its review |
| Approved by | the person who gave the final approval |

The review has nothing to do with reports themselves. A report only reads who prepared, checked and
approved each record.

## Decisions (owner, 2026-11-16)

- Document types are the approval document types. Each type may have a review configuration; a type
  without one has no review step and behaves exactly as today.
- One review level per type. The configuration names any number of roles and users; any one of them
  may confirm.
- The reviewer confirms with a button and an optional comment. No password re-entry.
- The preparer may confirm their own record's review, so work is not stuck when no reviewer is
  available. (Approval segregation rules are unchanged.)
- Corrections: the reviewer may correct the record directly, or return it to the preparer with a
  comment; the preparer corrects it and sends it back for review.
- Reviewers find their work on a **Pending reviews** page next to Pending approvals. Each item opens
  the document's own page, which shows a Review panel.
- The purchase order's own check is this review from phase 1: checking an order is confirming its
  review, so its checker is recorded and printed. Later, the other type-specific review steps (the
  stock issue check, QC formula revision review) move onto this one implementation.

## How it works

Every document type starts its approvals through one method,
`ApprovalRepository.CreateInitialApprovalsAsync(modelType, modelId)`. The review is a gate there:

1. If the type has a review configuration and the record has no open or confirmed review for this
   submission, the method creates a `DocumentReview` (status **Awaiting review**) and returns. No
   approval stages are created, so approvers do not see it yet, and no automatic approval runs.
2. **Confirm** (reviewer or preparer, optional comment): the review becomes **Confirmed**, records
   who and when, and the original approval start runs in the same transaction. From here the record
   follows its approval workflow, or automatic approval when it has none, exactly as today.
3. **Return** (reviewer, comment required): the review becomes **Returned**. The preparer corrects
   the record and **sends it back for review** (status Awaiting review again).
4. While a review is Awaiting review or Returned, the record is editable through its normal edit
   page by anyone holding that type's edit key. Edits are recorded on the review history with who
   and when.
5. A returned record that is submitted again through its own flow goes back to review, not to
   approval. When an approver later rejects the record and it is resubmitted, it is reviewed again: each
   submission gets its own review. Prints show the review of the submission that was approved.

Tables:

- `ReviewConfigurations`: one row per document type, with `ReviewConfigurationAssignees`
  (exactly one of role or user each).
- `DocumentReviews`: model type, model id, submission number, status, prepared by, submitted at,
  reviewed by and at, comment. One open review per record (unique index).
- `DocumentReviewEvents`: Submitted, Edited, Returned, Resubmitted, Confirmed, with actor, time and
  comment. Append-only.

A per-type registry lists, for each review-enabled type: its label, its view key, its edit key, how
to find its creator, number and department, and its desktop page. Only registered types can be
configured. A type is registered only once its edit-during-review behaviour and its desktop Review
panel are built and tested.

## Purchase orders

A purchase order has always had to be checked before approval, so its review is always on, whether or
not reviewers are configured. Finalising an order (or revising one) opens its review; checking it
(`PUT purchase-order/check/{id}` or Confirm on the panel) confirms the review and then runs the
existing check: price units verified, status Checked, approvals started. Without a configuration,
anyone holding the check key for the order's supplier type may check it, as before; with one, only the
named reviewers or the preparer may. A returned order is corrected by finalising it again, which sends
it back for review. Orders already awaiting check on deploy get an open review from the migration.

## Permissions

Each task uses the key for the record's stored kind; the server checks it on the record.

| Key | Task | Granted by migration to |
| --- | --- | --- |
| `CanViewDocumentReviewSettings` | see review configurations | holders of `CanViewApprovals` |
| `CanEditDocumentReviewSettings` | create or change a configuration | holders of `CanEditApprovalWorkflow` |
| `CanDeleteDocumentReviewSettings` | remove a configuration | holders of `CanDeleteApprovals` |
| `CanViewPendingReviews` | see the Pending reviews page | holders of either purchase order check key |
| `CanReturnForeign/LocalPurchaseOrderCheck` | return an order awaiting check | holders of the matching check key |
| `CanEditRaw/PackageMaterialPurchaseRequisition` | correct a purchase requisition in review, send it back | holders of `CanCreateRaw/PackageMaterialRequisitions` |
| `CanReviewRaw/PackageMaterialPurchaseRequisition` | confirm a purchase requisition's review | nobody |
| `CanReturnRaw/PackageMaterialPurchaseRequisition` | return a purchase requisition | nobody |
| `CanReviewRaw/PackageMaterialStockRequisition` | confirm a stock requisition's review | nobody |
| `CanReturnRaw/PackageMaterialStockRequisition` | return a stock requisition | nobody |
| `CanReview/ReturnBillingSheet` | confirm or return a billing sheet | nobody |
| `CanReview/ReturnStaffRequisition` | confirm or return a staff requisition | nobody |
| `CanReview/ReturnOvertimeRequest` | confirm or return an overtime request | nobody |
| `CanReview/ReturnLeaveRequest` | confirm or return a leave request | nobody |
| `CanReview/ReturnJobRequest` | confirm or return a job request | nobody |
| `CanReview/ReturnPackingList` | confirm or return a packing list | nobody |

Confirming an order's check keeps `CanCheckForeign/LocalPurchaseOrder`. Sending a returned stock
requisition back uses `CanUpdateProductionStep` (it is corrected through its batch's Repair action);
a purchase order is sent back by finalising it (`CanCreateForeign/LocalPurchaseOrder`). Reading a
record's review uses its own view key. Being named in the configuration (or being the preparer, for
Confirm) is required as well as the key whenever the type is configured.

## Phases

1. **Engine and first documents** (built): tables, gate, configuration, Pending reviews, Review
   panel, Prepared / Checked / Approved on prints, for purchase orders (their check), purchase
   requisitions and stock requisitions (raw and packaging).
2. **Remaining approval documents**, a few at a time, each with its edit-during-review check and
   Review panel. First batch (built): billing sheets, staff requisitions, overtime requests and leave
   requests; second batch (built): job requests and packing lists (`AllocateProductionOrder`). They are
   corrected through their own edit endpoints, which record the edit on the review
   (a billing sheet's edit and add-charges are allowed while its review is open). Not yet: stock
   adjustments, customer quotations and proforma invoices (no edit endpoint, so a returned record
   could not be corrected), trial requisitions (no desktop page), production orders (created from a
   quotation that would already have been reviewed), shipment documents and discrepancies (raised by
   the system on arrival), payroll runs, performance reviews, payments and R&D documents.
3. **Move the remaining checks onto the engine** (built):
   - **Stock issue check.** Every issue waits for its check; confirming it checks the issue and signs the
     voucher. The issuer may check their own issue (owner's decision). A returned issue is corrected
     through the issued-material repair and sent back. The approval queue no longer carries issue
     checks; old stages remain as history. The migration names everyone from the old workflow's
     stages as reviewers, gives their roles `CanCheck/ReturnRaw/PackagingMaterialIssue` and Pending
     reviews, and opens reviews for checks still waiting.
   - **QC formula review.** Submitting a revision opens its review. "Record review" (definition hash
     and a reason of at least 10 characters) confirms it; when reviewers are named only they or the
     author may record it, and the reviewer's stored key is re-checked. A reviewer may return a
     revision to Draft (`CanReturnFormulaRevision`); the formula audit row is written in the same save
     the status trigger checks. Revisions waiting for review move from the approvals feed to Pending
     reviews. The migration names the workflow's review stage as reviewer and gives review-key roles
     the return key and Pending reviews.
