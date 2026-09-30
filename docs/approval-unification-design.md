# Unified pending approvals

Date: 2026-09-29. Status: implemented read model and selected no-workflow transitions; deployment verification pending.

## Scope

`My Pending Approvals` currently merges the legacy `GET /api/v1/approval/my-pending`
queue with `GET /api/v1/qc/worksheets/approvals/my-pending`. The following
revision lifecycles must also be represented: formula review and approval;
Full Procedures question, section, form, activity, workflow and Procedure
revision review and publication/approval. A queued item must link to the
resource's own action page. The queue is a read model; each resource service
remains responsible for authorization, state transition and audit entries.

## Assignment and visibility

Return an item only when the current user can perform its current action.
Evaluate global permission keys, area owner/grant role access, assignment and
segregation of duties on the server. Exclude the author from independent review
and approval, and exclude the reviewer from approval where the configured policy
requires three people. Formula review and approval queues currently return
role-visible work rather than a user-specific `my-pending` projection; the new
projection must apply the resource's action eligibility before merging rows.
Deduplicate each (resource type, revision ID, action) key. A failed source query
must be surfaced as an error, not displayed as an empty queue.

## Workflow configuration

Existing Full Procedures area review policy counts as a configured workflow:
keep its independent reviewer and publication/approval gates. A generic
`Approval` configuration with at least one stage is also configured. Configured
workflows create actionable pending stages and never auto approve. If a flow
has no configured workflow or has zero stages, submission must complete its
normal release checks, record an explicit system automatic-approval audit event,
and advance to the appropriate approved/published state atomically. Do not
infer auto approval from an empty queue. Formula revisions have no current
shared Approval configuration and need an explicit configuration type and
transition policy before this fallback is enabled.

The QC worksheet module currently refuses submission when no approval chain is
configured, and the generic approval processor explicitly rejects QC automatic
approval. Changing that policy requires resource-specific automatic completion
for STPs, worksheet templates, specifications, worksheet instances and OOS
cases. Each path must preserve its existing validation, status sequencing,
dependency checks and immutable audit trail. COA supersession is a separate signed withdrawal action rather than a standard
multi-stage approval chain. The requested fallback includes COA supersession;
it must record an explicit automatic withdrawal audit event and retain the
existing certificate trace. OOS disposition may auto approve only after a
human has preselected a valid outcome; the automatic path must apply that exact
outcome and preserve the batch disposition transaction.

## API and UI contract

Prefer one server-owned `GET /api/v1/approval/my-pending` response with a
stable action kind, resource type, resource ID, revision ID, display label,
request time and resource route. Keep legacy and QC rows in the response during
migration without dropping existing clients. The frontend should render this
single feed and navigate to each resource's own detailed reviewer page.
Every resource in the feed needs such a page, including Procedure revisions,
which do not currently have a frontend detail route. The page must show the
exact version and content hash, the proposed changes, supporting evidence,
author and prior review history, the approval stages or automatic policy, and
the meaning of the requested action before any approval control is available.
Approval and rejection remain resource-specific, with a reason and the existing
reauthentication rules when configured. A status or configuration change must
invalidate the feed; the page must distinguish an
empty queue from a failed query.

## Audit and deployment

A system automatic approval needs an explicit action name, triggering actor,
configuration snapshot/version, reason, content hash or revision identity,
time and correlation ID. It must not impersonate a human signature or populate
human approver fields. Approvals and business state changes must commit in one
transaction. Do not enable a resource's fallback until its automatic completion
path and read model are implemented and reviewed. Deployment must apply any
schema migration before the API version that depends on it.

Documentation summary: this note records the requested queue scope, the user's
choices that an area review policy is configured, COA supersession may use
automatic approval, OOS may auto approve a preselected outcome only, and every
queued approval requires a detailed review page. Backend and frontend implementation is described in the service and workflow notes.
The live deployment and end-to-end behavior have not been verified.

## Implemented contract note

The shipped queue currently uses a third endpoint,
`GET /api/v1/approval/my-pending/revisions`, merged in the frontend with the
legacy and QC endpoints. It does not yet consolidate these into one backend
response. The revision endpoint checks action permissions, formula stage
assignment, area grants, and segregation of duties.
