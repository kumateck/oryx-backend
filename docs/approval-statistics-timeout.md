# Pending approval statistics timeout (2026-10-30)

## Incident and diagnosis

`GET /api/v1/approval/my-pending/stats` returned 500 after approximately 30 seconds at version `c501bf9922f8b5122a7767f8dfe990683660e90c`. The Npgsql reader timed out during an asynchronous EF query. Comparing the reported revision with local `main` found no change to the controller, approval repository, delegation service or context that fixes this path.

The controller called `ApprovalDelegationService.Pending`, which materialized every pending document through `ApprovalRepository.GetEntitiesRequiringApproval`, then grouped those documents in memory. The repository loaded approval stages, users and other navigation properties and synchronously queried approval action logs separately for each document. The same work repeated for each active delegator.

Model-level `AutoInclude` expands these reads beyond the explicit includes: users load departments and their warehouse/location/rack/shelf hierarchy; purchase orders load items and revisions; materials load batches, events, reservations and shelf assignments. Some queue queries use split queries, while staff requisitions, production orders and payments can load collections in a single query. Counts never need these graphs or audit histories. Queue size, related collection sizes, historical material data and active delegator count amplify this work. This is ordinary valid customer data, not malformed input.

The supplied stack does not name the failing SQL command or provide a query plan, blocking information or server/network telemetry. It establishes a read timeout, but cannot prove which document type or environmental condition caused that particular timeout. The fix removes the demonstrated avoidable work; it does not claim to resolve all database or network outages.

## Change

Statistics read only `(ModelType, Id)` keys through the same query predicates used by the queue list. Shared predicates preserve assignment, activation, response round, document state and soft-delete rules. Payments retain the recorder exclusion. Delegated queues are merged by `(ModelType, Id)` and electronically signed QC documents remain non-delegable.

The stats controller, the document-counts controller's ERP counts and the repository's existing statistics helper use the lightweight path. Full queue and detail endpoints retain their presentation and audit fields. No command timeout increase, automatic retry, permission bypass, write or schema migration is introduced.

## Related paths

The repository statistics helper and the document-counts controller had the same full-queue counting mistake and are changed too. `my-pending`, pending detail authorization, and approval/rejection preflight also call the full queue builder. Those paths remain candidates for separately scoped key-membership and detail-loading optimizations; their business behavior is retained in this patch. Global AutoInclude configuration is unchanged because other callers may depend on it.

## Verification and rollout

The endpoint regression checks document-level counting with duplicate matching stages and requires zero domain entity materializations/tracking for stats. Additional coverage checks PostgreSQL SQL translation and omission of unrelated joins, queue filters, requisition types, response rounds, payments, and delegation deduplication. These tests verify query shape and behavior rather than asserting a machine-dependent 30-second threshold.

Verified the endpoint regression fails against the original compiled backend because it materializes the document and both matching stages. With the fix, the filtered approval/pending/payment/response suite built and passed: 199 passed, 2 skipped (one disposable PostgreSQL test and one external corpus fixture). All 25 key-query projections translate through Npgsql. Static analyzers were disabled for this build; existing compiler warnings remain. No live customer database or production-scale timing test was run.

```sh
dotnet test tests/APP.Tests/APP.Tests.csproj --no-restore -m:1 -nodeReuse:false \
  -p:RunAnalyzers=false -p:NuGetAudit=false \
  --filter 'FullyQualifiedName~Approval|FullyQualifiedName~Pending|FullyQualifiedName~Payment|FullyQualifiedName~Response'
```

After deployment, check endpoint duration and database command logs on the affected deployment using a representative approver, including active delegation. If timeouts persist, capture the exact SQL and an execution plan, check blocking, connection health and database load, and evaluate indexes against that evidence.

## Customer data

This endpoint only reads data. The failure does not create partial approvals, inventory/financial events or missing audit writes. No data repair or approval reset is indicated by this incident. Do not delete historical material batches, warehouse data or audit logs to reduce query cost. No production data was accessed or changed during this fix.
