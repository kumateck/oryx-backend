# Re-checked purchase orders get a new approval round (2026-10-23)

## Problem

Checking a purchase order (`PUT procurement/purchase-order/check/{id}`) sets it to `Checked` and calls
`CreateInitialApprovalsAsync`. `CreatePurchaseOrderApprovals` returned early whenever the order already
had any approval rows.

A revised order has rows from its earlier round, all already decided. So checking it again created
no pending step: it never reached anyone's pending approvals and stayed at `Checked` forever.
Example: EPRC-69 on the testing site, checked from the Revised tab.

Keeping the old rows alongside new ones would not work either. `ApproveItem` approves the first
row of the current order that is not `Approved`, so it could pick an old `Rejected` row. And an old
required `Rejected` row means "all required approved" is never true.

## Change

`ApprovalRepository.CreatePurchaseOrderApprovals`:

- If the order has a **pending** row, its round is still running, so nothing changes. Checking twice
  still does nothing new.
- If every earlier row is decided, those rows are removed, `PurchaseOrder.Approved` is reset to false,
  and a fresh round is created from the current workflow. The first stage is activated.

Earlier decisions stay in `ApprovalActionLogs`, which is what the approval history shows. Dashboard
counts that read `PurchaseOrderApprovals` rows no longer include the replaced round.

## Tests

`tests/APP.Tests/Repository/PurchaseOrderRecheckApprovalTests.cs`:

- after an approved round, a re-check creates a new pending round for the approver;
- after a rejected round, the same happens;
- the new round can be approved: the order becomes `Approved`, and the history keeps both decisions;
- checking twice keeps the waiting round.

## Deploy

No migration. Orders already stuck at `Checked` with no pending row (like EPRC-69) are not repaired by
this change. The check page lists `PendingCheck` (New tab) and `Revised` (Revised tab) orders, so a stuck
order no longer appears there. To unstick one after deploying, set it back to `Revised` (status 10) and
check it again, or revise it again (an item update sets `Revised`).
