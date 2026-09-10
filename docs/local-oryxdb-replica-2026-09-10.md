# Local `oryxdb` replica verification — 2026-09-10

The existing remote `oryxdb` was copied read-only into the native local PostgreSQL
instance as `oryxdb` on `127.0.0.1:5432`.

## Controls

- Source: remote `oryxdb` at `164.90.142.68:5433`.
- Target: local `oryxdb`; the prior disposable target was replaced.
- The source was only read by `pg_dump`; no remote migration or write was run.
- The archive was validated with `pg_restore -l` before restore.
- Restore used `--no-owner --no-acl` so local ownership is independent of remote roles.

## Verification

Remote and local counts matched after restore:

| Check | Result |
| --- | ---: |
| EF migration history rows | 141 |
| `FormulaRevisions` rows | 0 |
| `Questions` rows | 278 |
| `Forms` rows | 1,971 |

The backend design-time context was then run with an environment-only connection
override and reported `ApplicationDbContext`, PostgreSQL provider,
database `oryxdb`, and data source `127.0.0.1:5432`.

The local API was subsequently started on `127.0.0.1:5299` with the same
connection override. Its normal startup migration step applied the four pending
local migrations, including `20260910063931_AddFormulaRevisionAuthoringPayload`;
the local migration history is now 145 entries. The remote source remains at its
original state.

For the local integration session, the governed formula worker is running on
`127.0.0.1:3101` with a temporary local workload token, and the API is running
with `FORMULA_RUNTIME_ENABLED=true` and that worker as its service base URL. The
frontend is running on `http://localhost:3000` with a runtime build configured to
call the local API at `http://127.0.0.1:5299`. Swagger, the formula worker
`/ready` endpoint, and the browser formula page all responded successfully.

The committed development connection settings were intentionally left unchanged;
start the local backend with the local `oryxdb` connection supplied through the
`connectionString` environment variable.
