# Live quality workflow compatibility — 2026-10-27

Deploy this backend alongside the Live workflow completion described in `oryx-live-desktop/docs/workflow-hardening-2026-10-27.md`. No new schema migration is introduced; existing QC/retest/rework/discrepancy migrations are prerequisites.

- OOS investigation endpoints now enforce their action-specific permission keys and load current grants before proceeding. The catalogue registers the same four OOS keys used by Oryx Next. Verify the intended role grants before rollout.
- Initiation requires one subject, eligible record state and current rejected results. Live supplies optional `ResponseId` to distinguish chemical/microbial tests and refuse superseded cycles. Existing callers can omit it. Duplicate active investigations, reused certificate references, final disposal and permanent rejection are refused.
- Submission requires meaningful findings. QA approval checks findings and existing QC readiness; authors and submitters cannot review their own investigations. Writes retain their actor and timestamps.
- Material retests can create and link an unscheduled testing request in a serializable transaction. Kind, exact batch and acknowledged state are verified before creation. Existing linking remains available. A retest-specific certificate read returns all linked revisions without a capped global search.
- Rework exposes its pinned field membership and validation metadata through `GET qc/rework-tasks/{id}/form`. Certificate-generation endpoints accept optional `expectedResponseId` and refuse a different current cycle. They also enforce current material-kind/product checking permissions.
- Worksheet evidence uses authenticated instance-scoped read/upload routes. Uploads require the assigned analyst, track permission, an in-progress instance and a new reference, and serialize against instance transitions. Generic file routes cannot read, replace or delete this governed evidence.
- Controller response annotations now expose strict retest/rework/discrepancy DTOs to generated clients. Existing business endpoints are retained.

Export metadata without starting the application or seeding data:

```sh
dotnet build API/API.csproj --no-restore --disable-build-servers -m:1
cd API
dotnet bin/Debug/net9.0/API.dll --export-openapi /absolute/path/backend.json
```

Import this snapshot in Live with `ORYX_OPENAPI_FILE=/absolute/path/backend.json bun run api:export`, regenerate hooks, and check the reviewed customer-server contract fingerprint before activation. Exporting does not prove the deployed backend matches the local source.

The affected acceptance suite passed 94 tests covering OOS initiation/review, current permission refusal, separate response cycles, retest creation/issuance, worksheets, stock reconciliation and shipment shortages. Databases were isolated test stores; real PostgreSQL concurrency, external blob storage and customer deployment/Windows acceptance still require staging validation. No production data was changed or deployment performed.

The existing AutoMapper 14.0.0 package still produces NU1903 for [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x). The advisory lists patched versions 15.1.1 and 16.1.1. Resolve the backend-wide upgrade and its compatibility/licensing requirements before treating this release as fully production approved; these workflow repairs do not remove that dependency warning.
