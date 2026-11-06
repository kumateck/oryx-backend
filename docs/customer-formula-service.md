# Customer formula calculation service

The authoritative runtime is owned by **oryx-live-desktop**, in
`services/formula-calculation`. It uses that repository's existing formula core.
The obsolete Next application is not built, pulled or contacted. The API remains
the authority for user permissions, governed revisions, evaluation records and
audit. No HTTP API or desktop bridge contract changes are required.

## Prepare the release

After reviewing/merging the desktop change, obtain the `oryx-formula-<desktop-SHA>`
artifact from its customer release workflow (testing or entrance-server), or on a
build machine with Docker and Bun **1.3.14**, in the released desktop checkout:

```bash
bun install --frozen-lockfile
bun run formula:package /absolute/path/to/new-formula-release
```

The directory contains `manifest.json` and `image.tar`. The manifest pins the
Docker image identity, archive SHA-256, engine hash, numeric policy, source SHA
and source-content hash. It also records whether the source checkout was dirty;
production release bundles should come from clean, reviewed source. Keep this
bundle as a release artifact; transfer the whole directory using your approved
file-transfer/offline-media process. No registry login or Node installation is
needed on the customer server. Do not manually substitute an image/hash or edit
the manifest to make an old bundle pass. This package currently targets Linux
amd64, matching the customer server.

Backend changes are prepared from `demo`, reviewed into `desmond-latest`, and
must reach backend `main` before these customer commands can deploy them. Nothing
in these instructions pushes, merges, SSHs or activates a server automatically.

## Enable an existing customer installation

Use the existing backend deployment account, checkout and state directory. Do
not change HOME or run the whole deployment under sudo. Replace the paths and
`<desktop-SHA>` below with the delivered release's values. First pause the existing
poller; disable the legacy redeploy job as described in
[backend deployment](customer-backend-deployment.md) if it still owns the API.

```bash
cd /absolute/path/to/oryx-backend
bash deploy.sh pause
git pull --ff-only origin main
chmod 600 .env

mkdir -p "$HOME/.local/share/oryx-formula/releases/<desktop-SHA>"
# Copy the delivered manifest.json and image.tar into that directory before continuing.
chmod 700 "$HOME/.local/share/oryx-formula/releases/<desktop-SHA>"
chmod 600 "$HOME/.local/share/oryx-formula/releases/<desktop-SHA>/manifest.json" "$HOME/.local/share/oryx-formula/releases/<desktop-SHA>/image.tar"
ln -s "releases/<desktop-SHA>" "$HOME/.local/share/oryx-formula/current.new"
mv -Tf "$HOME/.local/share/oryx-formula/current.new" "$HOME/.local/share/oryx-formula/current"

sudo bash deploy/customer-formula.sh init-secret
bash deploy.sh deploy
bash deploy/customer-formula.sh verify
bash deploy.sh status
bash deploy.sh resume
```

The `current` path is a symlink to the selected immutable bundle directory. If an
older deployment is already blocked/pending, inspect its protected log first and
use `bash deploy.sh deploy --retry` instead of `deploy`. Do not erase recovery
markers to bypass migration protection. A pre-existing real directory at `current`
must be moved to a retained release directory before installing the symlink.

`init-secret` creates `/opt/oryx/secrets/formula-service-token` once: root-owned,
mode 0600, in a root-owned 0700 directory. Repeating it validates and retains the
same secret. Invalid existing files fail instead of rotating. Back it up securely.
The networkless one-shot helper copies it into a Docker volume as UID 10001,
mode 0400; only that helper has CHOWN/DAC_OVERRIDE capabilities. Both running
containers mount the volume read-only. The unprivileged formula container drops
**all** capabilities and retains read-only root filesystem, restricted tmpfs,
no-new-privileges, PID/memory/CPU limits and no host-published port. Never put the
secret in `.env`, command arguments, renderer storage or logs. Do not remove the
secret volume on upgrades or use `compose down --volumes`.

The backend deployer verifies the archive checksum and exact image ID before
maintenance, freezes the bundle manifest and Compose files, and archives the
image independently of Docker's cache. It sets `FORMULA_RUNTIME_ENABLED=true`
and the secret file path in the API. A release-specific dependency must pass
`/ready` before the API stops; the old dependency is untouched during preparation.
The backend's existing backup/migration/recovery gates remain in effect.

Default bundle: `$HOME/.local/share/oryx-formula/current`. Optional overrides are
`ORYX_FORMULA_RELEASE_DIR` (absolute bundle directory) and
`ORYX_FORMULA_SECRET_FILE` (absolute path ending in `formula-service-token`). Use
consistent overrides in manual commands and Cron. For custom secret paths, pass
the override explicitly through sudo when initializing. Existing backend state,
environment and health-port overrides still apply.

## Verify and diagnose

```bash
bash deploy/customer-formula.sh verify
bash deploy.sh status
docker ps --filter label=com.docker.compose.service=formula-calculation
```

Verification executes `dotnet API.dll --formula-readiness` **inside the live API
container**, using the same settings loader, HTTP client registration and
`IsReadyAsync` call as `/api/v1/formula-runtime/status`. It fails nonzero and the
deployment fails loudly if disabled, unreachable or `/ready` is non-success. It
starts no HTTP server, migrations, database connection, seeding or telemetry.
It needs no user password, forged token or anonymous endpoint. The normal status
endpoint still requires sign-in and `CanViewQuestions`; it should report
`ready: true, authorityEnabled: true` for an authorized user. Finish acceptance by
clicking **Validate on the server** on a known formula in Oryx Live Desktop.

A preparation/health failure leaves the current API running. A failure after
migration starts keeps the API stopped for recovery, preserves the database
backup, and never silently falls back to an old binary/schema. Read the protected
log reported by `status`; do not paste environments or secret material into chat.
Install/fix the bundle or secret, resolve the cause and use `deploy --retry`.
`/health` alone is not sufficient: it also succeeds on a disabled service.

## Upgrade and roll back

To upgrade, retain the old bundle, install a new delivered bundle under a new
release directory, switch `current` with the symlink commands above, and run
`bash deploy.sh deploy` (even when backend main has not changed). Polling skips
unchanged backend SHAs; changing only the bundle does not trigger it. Retained
formula snapshots are keyed by release identity, including the bundle hash.
The same secret is reused; no formula approvals or stored results are rewritten.

For formula-only rollback, pause polling and select a retained **formula release
ID** shown by `status`/the protected state directory:

```bash
bash deploy.sh pause
bash deploy/customer-formula.sh rollback <retained-formula-release-ID>
bash deploy/customer-formula.sh verify
bash deploy.sh status
```

Rollback starts and health-checks the retained exact image/hash before switching
the **current backend binary** to it. It does not restore or downgrade a database.
If activation/readiness fails, it attempts to restore the previous formula
configuration; failed recovery stops the API and retains `formula-pending`.
An interrupted rollback can be retried with the same command after inspection.
Do not resume polling until you have selected the corresponding retained bundle
as `current`, resolved any pending state, and validated the formula screens.
Successful rollback leaves backend `current` unchanged and updates only
`formula-current`. Keep state/release snapshots and `formula-images` archives;
missing Docker images can be restored from those verified archives offline.
Full backend/database rollback still follows the existing tested restore process.

## Local verification

```bash
bash -n deploy/customer-backend.sh deploy/customer-formula.sh
bun test tests/deployment/customer-backend.test.ts --timeout 30000
dotnet test tests/APP.Tests/APP.Tests.csproj --filter 'FullyQualifiedName~FormulaCalculationClientTests|FullyQualifiedName~SettingsContractTests'
# On a development machine only, with a built API image containing --formula-readiness:
bash tests/deployment/customer-formula-smoke.sh /absolute/path/to/formula-bundle <local-API-image>
```

The smoke test creates uniquely named, disposable local networks/volumes and no
published ports. It checks secret reuse, service hardening/authentication, real
validation/evaluation, backend readiness, disabled/unreachable/stopped failures
and restart. It selects no customer server or database. Deployment fixtures cover
failed prerequisites, migrations/health, same-backend formula upgrades, rollback,
restoration after failed rollback, failed recovery and interrupted activation.

Verified locally on 2026-11-06: API compilation; 11 focused .NET tests; 34
customer deployment/recovery tests; strict deployment TypeScript and shell syntax;
34 desktop service tests and 63 shared-core tests; 24 release-workflow tests and
the pinned offline actionlint check. The final desktop image/bundle built and the
disposable Docker smoke passed, including real validation and evaluation. The
full production backend Docker/migration build and complete web/Electron build
were not run. No customer server was contacted or changed.
