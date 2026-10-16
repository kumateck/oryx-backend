# Approval delegation

A user can hand their approvals to another user for a period. Added 2026-10-15; table `ApprovalDelegations` (migration `AddApprovalDelegations`).

## Endpoints (`ApprovalController`)

| Endpoint | Does |
| --- | --- |
| `POST approval/delegate` `{ delegateUserId, startDate, endDate, reason? }` | Delegates the caller's approvals. Returns the delegation id. Replaces the old stub, which only checked that a user existed. |
| `GET approval/delegations/mine` | `{ given, received }`: the caller's delegations, including those ended or revoked in the last 30 days, each with its status (0 upcoming, 1 active, 2 ended, 3 revoked). |
| `DELETE approval/delegations/{id}` | Ends a delegation at once. Either the delegator or the delegate may end it; the record is kept. |

Any signed-in user may delegate their own approvals; there is no permission key for it, as there is none for deciding an approval assigned to you.

## Rules (`APP/Services/Approvals/ApprovalDelegationService.cs`)

- Not to yourself, not to a disabled or unknown user.
- Must end after it starts and in the future; a start in the past is moved to now, so decisions already taken stay the delegator's.
- At most 90 days. A longer absence is a change of approver: transfer approval rights instead.
- One delegation at a time per delegator (no overlapping periods unless revoked).
- No chaining: a delegate acts only with the delegator's own user and roles, never with approvals delegated to the delegator.
- The delegator keeps the right to decide their approvals.
- QC worksheet documents (STP, template, specification, worksheet, OOS case, COA) are never delegated: they are signed electronically and need the signer's own re-authentication.

## How it acts

The approval engine decides by (user, roles). The controller's queue (`my-pending`, `my-pending/stats`, `my-pending/document-counts`, `my-pending/{type}/{id}`) is the caller's own queue plus the queue of each user with an active delegation to them; a document only reachable through a delegation carries `onBehalfOf`. Approving or rejecting such a document runs with the delegator's user and roles, so each document type's own rules apply unchanged, and the comment is prefixed `Decided by <delegate> on behalf of <delegator> (delegated).` The request audit records the delegate as the caller. A document waiting for both is decided as the caller's own.

## Tests

`tests/APP.Tests/Repository/ApprovalDelegationTests.cs`: refused delegations, the start moved to now, overlap, roles used while active, revoke by either party only once, the merged queue with QC documents left out and own documents winning.
