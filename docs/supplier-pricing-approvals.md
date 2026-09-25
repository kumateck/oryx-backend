# Supplier pricing agreement approvals

`SupplierPricingAgreement` is the approval document type for proposed supplier
pricing creates, revisions, and archives. Customer quotations retain their
existing `CustomerQuotation` approval workflow.

Apply migration `20260925071612_AddSupplierPricingAgreementApprovals` before
deploying the API. It adds proposal status, change kind, prior-agreement link,
and per-stage approval records. The migration marks all existing agreements
Approved so established effective prices remain available.

POST creates a pending proposal, PUT proposes replacement of an approved row,
and DELETE proposes an archive. Effective pricing reads return approved rows
only. A configured stage appears in the assigned user's pending approvals.
Final approval rechecks overlapping periods, then activates the proposed price
and closes or archives the prior row in one transaction. Rejection leaves the
prior price in force. Submission also verifies the material/UoM is linked to
the supplier. The proposer cannot approve their own change.

With no configured workflow or no stages, submission auto-approves and writes a
system `ApprovalActionLog`. The proposal detail endpoint is scoped by supplier
and permits only its proposer or an assigned approver to read the proposed
terms. Existing pricing audit headers remain required.

Focused coverage: `SupplierPricingTests` verifies no-workflow audit logging,
pending isolation and maker-checker review, and final revision activation;
`SupplierPricingAssociationTests` checks the supplier/material/UoM boundary.
