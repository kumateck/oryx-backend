# Production board query

`GET /api/v1/production-schedule/activity/operation-grouped` scopes batches by their product's department. The existing EF product visibility filters still apply. An omitted department parameter does not disable those filters.

The response keeps the existing `ProductionActivityGroupResultDto` contract and configured empty columns. It also includes operations currently used by visible running batches, even if an operation is shared or belongs to another operation catalog. Existing runs retain their step IDs, assignments, timestamps, and audit history.

The former query loaded every activity, every step, and every step's responsible people before grouping against the department catalog. A batch disappeared if its current operation was absent from that catalog. Batch-number fallback also made synchronous lookups while mapping each row.

The board now filters cancelled/completed runs and batches in the database, then projects only batch identity, product name/code/unit, schedule code, current operation/status/timing, and that step's assigned people. Only the first unfinished step is selected. Product/step automatic includes and EF change tracking are disabled for this projection; resources, work centers, forms, activity logs, and unrelated user fields are not loaded. A correlated database subquery resolves BMR batch-number fallback without one application/database round trip per batch. Column grouping uses a dictionary keyed by operation ID, and columns have deterministic ordering.

`tests/APP.Tests/Repository/ProductionActivityBoardTests.cs` covers missing catalog operations, product department isolation, duplicate/empty columns, terminal batches, current-step projection, BMR fallback, existing product visibility, and PostgreSQL query translation without loading full user or step records.

Run:

```sh
dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~ProductionActivityBoardTests
```

No database migration or production data rewrite is required. Deploy the backend build to update the live endpoint; a frontend build alone will not apply this fix. Live response latency and execution plans must be measured on the deployed database; SQL translation and in-memory regression tests are not a production benchmark.
