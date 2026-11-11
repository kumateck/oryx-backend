# Stock transfer and ARD validation

## Stock transfer issuance

The issue preview and the server write use the same shelf eligibility. The
picker reads the requested material from the supplying warehouse, uses the
effective validity date, and counts free stock after active production
reservations. An available lot with a valid retest extension can be picked.
Expired lots, frozen lots, and retired shelf records cannot be picked.

The server checks the source department, the current raw or packaging issue
permission, approved status, and the complete quantity before deducting any
stock. If a selected lot has become unavailable, reload the transfer and
review the new picks. The desktop shows the effective validity date and uses
the server's expired flag where available. The existing API contract and
permission keys cover this change.

Only the supplying department can approve or reject a transfer, using the
corresponding incoming transfer permission. A decision is accepted while the
request is awaiting approval. Issuance is accepted once after approval.
Approval, rejection, and issue use the same database transaction lock.
An existing issue timestamp or holding record also blocks a repeated issue
on older records, even when the recorded status is stale.
New transfer requests require an active unit of measure. Replace an older
unissued request that has no saved unit before issuing stock against it.

**Issued batches** shows the lot and quantity recorded when stock left the
supplying warehouse. Apply the `LinkStockTransferIssueSource` migration
before deploying the desktop change. It links each new issue to its exact
transfer source and enforces a single linked issue per source. For older
records, history is available when one existing issue matches the transfer
and supplying and receiving departments. Ambiguous or missing history is
shown as unavailable.

## Creating analytical raw data records

Product and material ARD creation validate shared sections and the sections
for the requested analysis. A draft section reserved for another analysis
does not block creation. Applicable sections still need an active saved
question. An incomplete section error identifies the linked template and
section position in plain text. Open the exact template linked to the product
or material specification, save the missing question in the reported section,
and retry.

No schema migration or customer data rewrite is needed.

## Verification

28 focused backend tests passed. The PostgreSQL test verified effective
validity and reserved stock selection. The issue regression verified that a
stale lot and a caller without the issue permission leave stock untouched.
The ARD regression created a bulk product ARD from a saved formula while an
empty section for another analysis remained a draft.

Four desktop stock transfer tests and the desktop TypeScript check passed.

## Test result approval workflow

In **Settings → Approvals**, create or edit **Test results (Response)**. Add
the two roles and mark the role that must sign as **Required approval**.
Either role can be optional. The workflow requires at least one required
stage. Optional approvers can sign while the round is open, regardless of
where they appear in the workflow. Required approvers still sign in their
configured order. If an approver holds both roles, their decision applies
to the active required stage. Once all required stages approve, the round closes.

The server routes material and product test results through the existing
`Response` document key. The old route-specific labels are retained for
recognizing saved configurations. They are omitted from the list of new
workflow types. If a Response workflow already exists, edit that one.
Changes to workflow assignments apply when a new round starts. The migration
activates eligible pending stages in open rounds using their existing
assignments. It preserves saved approval decisions and activation times.

The existing `CanCreateNewApproval` and `CanEditApprovalWorkflow` permissions
cover workflow changes. The existing decision permissions and assignee checks
cover approval. The server rejects configurations with only optional stages
and refuses further decisions after a Response is approved or rejected.

The combined regression run passed 75 backend tests with PostgreSQL enabled.
It checked both stage orders and verified that the required role can complete
the round without the optional role. The desktop approval and transfer
regressions passed 26 tests.

The follow-up passed 71 backend tests with PostgreSQL enabled. It verified
that simultaneous requests produce one issue, processed holding records
still provide the issued quantities, ambiguous legacy matches are omitted,
and optional approvers can sign in either position. The migration test
also checked that closed rounds and saved activation times are preserved.
