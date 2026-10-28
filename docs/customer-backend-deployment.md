# Customer-host backend migration and deployment

The customer server builds backend `origin/main` and applies migrations locally.
The desktop `entrance-server` pipeline updates the frontend only. The backend's
GitHub demo workflow updates its separate demo database.

## Trigger now

Merge these deployment changes into backend `main`. On the customer's Ubuntu
server, in the existing backend checkout on `main`, run:

```bash
git pull --ff-only origin main
chmod 600 .env
bash deploy.sh deploy
bash deploy.sh status
```

`bash deploy.sh` defaults to the same deployment. It replaces the old script
that only built and started containers. Running `docker compose up --build`
directly bypasses this migration sequence.

Requirements: Docker Engine/Compose v2, Git with read access to the repository,
Python 3, curl, util-linux (`flock`), and coreutils (`sha256sum`). No host .NET,
EF, Bun, Node or GitHub Actions runner is needed. Git SSH remotes must already
have a verified host key. Existing customer environment settings are reused.
The complete backend retains over 160 MB of migration snapshots. Local Docker
compilation exceeded an 8 GB Docker allocation, even with one MSBuild worker;
customer build capacity still needs acceptance. The standalone SQL delivery
below applies schema without compiling the backend on the customer machine.

The installed Compose-managed `oryx_api` service `app`, `postgres_db` container,
and external `sail` network must exist. The Compose project is discovered from
the API container's labels. The database name comes from the resolved API
connection string, which must retain the installed
`Host=postgres_db;Port=5432;...;Database=...` contract. The deployment never
guesses a database or creates a replacement database container.
Before building, migration history is compared with the selected main source.
An applied migration missing from main blocks deployment before stopping the
API, preventing an older API release from replacing a newer database version.

## Automatically follow main

After the first successful manual deployment, use `crontab -e` under the same
customer account. Substitute the actual checkout and home paths:

```cron
* * * * * /bin/bash /home/entrance/oryx-backend/deploy.sh poll >> /home/entrance/.local/state/oryx-backend/poll.log 2>&1
```

Each poll fetches `origin/main`. A new SHA triggers build → API maintenance
window → verified database backup → pending migrations → API activation →
health verification. An unchanged successfully deployed SHA does nothing.
Without an existing success marker, the first poll deploys current main. A
filesystem lock prevents overlapping runs. Only the API service is replaced;
the frontend, database, OnlyOffice and other services are not redeployed.

Builds use detached main worktrees, leaving the installed checkout and `.env`
intact. To update the stable deployment tooling itself:

```bash
bash deploy.sh pause
git pull --ff-only origin main
bash deploy.sh resume
```

## Standalone SQL delivery

For a customer machine that should not compile .NET, an exported release may
contain `migrations.sql`, `manifest.json`, and a copy of
`deploy/apply-customer-migrations.sh` named `apply.sh`. The manifest records the
backend source SHA, every migration ID, and the SQL checksum. Extract the
package on the customer server and run:

```bash
bash /path/to/extracted-package/apply.sh /absolute/path/to/oryx-backend
```

This verifies the checksum and database migration history, stops only the API,
creates/verifies a backup, runs the SQL with PostgreSQL `ON_ERROR_STOP`, and
verifies that every package migration is installed. It uses the existing
Compose configuration to identify the database; no host SDK or image build
is involved. It deliberately leaves the API stopped and a pending marker:
activate an API built from the package's source release or a newer main that
contains all its migrations. `bash deploy.sh deploy --retry` performs that
activation through the full pipeline once the deployment change is installed.
For interrupted SQL, inspect the log before using `apply.sh <checkout> --retry`.

Do not pair an ahead-of-main database with an older API. This package applies
schema only; it does not install the backend application or update the frontend.

## Migration and recovery behavior

Both images come from the same main SHA through `deploy.Dockerfile`. The
one-shot `migration` target pins EF 9.0.3 and runs
`dotnet ef database update --no-build --configuration Release` with the API's
environment and network. Its design-time factory reads `connectionString`
without starting HTTP services or seeders. EF applies migrations using its
migration history. API startup migration remains explicitly disabled.

The old API serves requests while images build. Before schema changes, only
the API stops, and the existing PostgreSQL container creates a custom-format
dump using its local administrative connection. `pg_restore --list` verifies
archive readability and a SHA-256 is retained. Migration cannot start after a
failed backup. A fresh attempt that fails before migration restarts the
untouched original API.

Migration errors stop deployment. After migration has started, a failure keeps
the API stopped for operator recovery rather than restarting an old binary
against potentially partially migrated schema. No automatic schema downgrade
or database restore occurs. The exact image and the local API's 200/401/403
reachability are verified before recording a successful SHA. Existing
application seeding follows successful API startup; migration does not approve
or release ERP records.

State defaults to `$HOME/.local/state/oryx-backend`: `current`, `pending`,
`last-result`, protected logs, backups/checksums, frozen release environments
and the previous image ID. These files may contain secrets or customer data;
keep them private and apply the customer's backup retention policy. No global
image pruning occurs. Archive readability is checked, but a full restore
rehearsal remains an operational acceptance check.

Failures block automatic retry of the failed SHA. Interrupted attempts leave
`pending`, which also blocks polling. Inspect `bash deploy.sh status` and its
reported private log, correct the cause, then run:

```bash
bash deploy.sh deploy --retry
```

Retry selects latest main and applies its pending migrations. Manual `deploy`
can also retrigger an unchanged main; success-marker skipping applies only to
`poll`. For database restore, pause polling and follow the customer's tested
restore process using the retained backup and previous runtime settings.

## Settings

| Variable | Default |
| --- | --- |
| `ORYX_BACKEND_ENV_FILE` | Absolute path to checkout `.env`, mode 600/400 |
| `ORYX_BACKEND_STATE` | `$HOME/.local/state/oryx-backend` |
| `ORYX_BACKEND_HEALTH_URL` | `http://127.0.0.1:8080/api/v1/user/authenticated` |

Set the actual loopback API port if different from 8080. Pass overrides in Cron
as well when used. Settings stay outside Git.

## Verification

```bash
bash -n deploy.sh deploy/customer-backend.sh
bun test tests/deployment/customer-backend.test.ts
docker build --target migration -f deploy.Dockerfile -t oryx-backend-customer-migration:verification .
```

The test fixture substitutes Git, Docker, curl and flock. It checks ordering,
same-SHA polling, pause/lock behavior, retry, rejected targets, and failures at
each stage. It never contacts the customer server. Before customer acceptance,
also check applying and repeating a migration against disposable PostgreSQL.

Local verification (2026-10-28): all 13 deployment/package regression tests
passed on Linux, shell syntax and strict TypeScript checks passed, and the SQL
for current main `e12ac2cf40f0f88ab13a13fdcc08457286dd8971` installed all 188
migrations through `20261027170000_ReportHistoryCapture` on disposable
PostgreSQL 16 and 18. Repeating the script succeeded and preserved a sentinel
row. The delivered package runner was additionally exercised against the
PostgreSQL 16 fixture with only API lifecycle/config discovery substituted;
real history queries, dump, archive verification and SQL execution succeeded.
The full Docker image build exceeded the local 8 GB Docker allocation and is
not claimed verified. No customer server was accessed or modified.

References: [EF migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
and [Compose one-shot jobs](https://docs.docker.com/reference/cli/docker/compose/run/).
