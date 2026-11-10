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
stage. A required approver can act while earlier optional stages are
pending. Once all required stages approve, the round closes.

The server routes material and product test results through the existing
`Response` document key. The old route-specific labels are retained for
recognizing saved configurations. They are omitted from the list of new
workflow types. If a Response workflow already exists, edit that one.
Saved approval decisions are retained. This configuration applies when a new
round starts, and it does not replace the assigned approvers in an open round.

The existing `CanCreateNewApproval` and `CanEditApprovalWorkflow` permissions
cover workflow changes. The existing decision permissions and assignee checks
cover approval. The server rejects configurations with only optional stages
and refuses further decisions after a Response is approved or rejected.

The combined regression run passed 75 backend tests with PostgreSQL enabled.
It checked both stage orders and verified that the required role can complete
the round without the optional role. The desktop approval and transfer
regressions passed 26 tests.
