# Material retest list and detail read failure

The logs supplied on 2026-10-28 show `MaterialRetestService.ListAsync` failing
with `The Include path 'Material->Batches' results in a cycle`. Both list and
detail reads included the request's batch and its material, while the model
automatically includes every batch on a material. That inverse relationship
creates a cycle which EF Core refuses in a no-tracking query.

The two read queries now use `IgnoreAutoIncludes()` alongside `AsNoTracking()`.
They retain their explicit batch/material and retest-event includes, all DTO
fields, batch filtering, newest-first ordering, chronological event history and
the missing-detail response. The change is local to these two queries; global
material loading and retest writes keep their existing behavior. Endpoint
permissions and the approval, sampling, testing and certificate gates are unchanged.
This fix requires no API contract change or database migration.

`MaterialRetestReadTests` reproduces the original failure using the real context
model, then verifies list and detail results, batch scope, stored values, event
history and lack of tracked entities. Its PostgreSQL test uses only a dedicated
loopback database named `oryx_material_retest_test`, configured through
`ORYX_RETEST_READ_TEST_CONNECTION`. Run it alongside the existing
`MaterialRetestAcceptanceTests` and `MaterialRetestCreationTests`.

The reported Stock approval-configuration warning and the inventory ledger
transaction-type mismatch are separate issues. This query fix does not repair
saved approval stages or alter inventory ledger storage.
