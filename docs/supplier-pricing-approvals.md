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

## Positive prices below 0.01 (2026-10-24)

Supplier pricing API model validation now accepts every strictly positive decimal instead of imposing a 0.01 minimum. This matches the repository and frontend positive-price rule, preserving legitimate per-unit fractional prices. Zero/negative prices still fail. Previously the API rejected these requests before any proposal/stage could be created, so even configured approvers saw an empty queue. Approval configuration, assignment, maker/checker guards and audit behavior remain unchanged. This is a backend deployment change; it does not create/retry failed live agreements automatically.

Verification: 15 supplier-pricing tests pass, including API attribute validation for 0.0001/0.009/0.01, rejection of zero/negative values, and fractional-price auto-approval versus assigned-stage review. The PostgreSQL agreement amount column is unrestricted numeric, so no database migration is needed for this validation correction. Rebuild/deploy the backend before resubmitting the rejected proposal. No live prices, approval settings or decisions were changed. Documentation updated: supplier pricing approvals, services and workflows.
