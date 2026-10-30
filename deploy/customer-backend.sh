#!/usr/bin/env bash
# Customer-host pipeline: origin/main -> build -> backup -> migrate -> deploy.
set -Eeuo pipefail
umask 077
exec 3>&2
REPO=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
STATE=${ORYX_BACKEND_STATE:-$HOME/.local/state/oryx-backend}
ENV_FILE=${ORYX_BACKEND_ENV_FILE:-$REPO/.env}
HEALTH=${ORYX_BACKEND_HEALTH_URL:-http://127.0.0.1:8080/api/v1/user/authenticated}
OPERATION=${1:-status}
[[ "$STATE" = /* && "$STATE" != / && "$ENV_FILE" = /* ]] || { echo 'State and environment paths must be absolute.' >&2; exit 1; }
case "$OPERATION" in status|pause|resume|deploy|poll) ;; *) echo 'Use status, pause, resume, deploy [--retry], or poll.' >&2; exit 1;; esac
[[ ${2:-} = '' || ( "$OPERATION" = deploy && ${2:-} = --retry ) ]] || exit 1
mkdir -p "$STATE"/{releases,backups,logs,sources,blocked}
chmod 0700 "$STATE" "$STATE/releases" "$STATE/backups" "$STATE/logs" "$STATE/sources" "$STATE/blocked"
if [[ "$OPERATION" = status ]]; then
  for key in current pending last-result; do [[ ! -f "$STATE/$key" ]] || { printf '%s: ' "$key"; cat "$STATE/$key"; }; done
  [[ ! -f "$STATE/paused" ]] || echo 'Polling paused.'
  exit 0
fi
for command in docker git python3 flock curl sha256sum; do command -v "$command" >/dev/null || { echo "Missing prerequisite: $command" >&2; exit 1; }; done
exec 9>"$STATE/deploy.lock"
flock -n 9 || { echo 'Another backend deployment holds the lock.' >&2; exit 75; }
case "$OPERATION" in
  pause) touch "$STATE/paused"; echo 'Backend polling paused.'; exit 0;;
  resume) rm -f "$STATE/paused"; echo 'Backend polling resumed; failed releases still require deploy --retry.'; exit 0;;
  poll) [[ ! -f "$STATE/paused" ]] || exit 0;;
esac
[[ -f "$ENV_FILE" && ! -L "$ENV_FILE" ]] || { echo 'The existing backend .env file is required.' >&2; exit 1; }
MODE=$(stat -c '%a' "$ENV_FILE" 2>/dev/null || stat -f '%Lp' "$ENV_FILE")
[[ "$MODE" = 600 || "$MODE" = 400 ]] || { echo 'Protect the backend .env file with chmod 600 before deploying.' >&2; exit 1; }
[[ "$HEALTH" =~ ^http://127\.0\.0\.1:[0-9]+/api/v1/user/authenticated$ ]] || { echo 'Health URL must use the customer API loopback port.' >&2; exit 1; }
remote=$(git -C "$REPO" remote get-url origin)
case "$remote" in https://github.com/kumateck/oryx-backend.git|https://github.com/kumateck/oryx-backend|git@github.com:kumateck/oryx-backend.git) ;; *) echo 'origin must be kumateck/oryx-backend.' >&2; exit 1;; esac
export GIT_TERMINAL_PROMPT=0
export GIT_SSH_COMMAND='ssh -o BatchMode=yes -o StrictHostKeyChecking=yes'
git -C "$REPO" fetch --no-tags origin refs/heads/main:refs/remotes/origin/main
TARGET=$(git -C "$REPO" rev-parse refs/remotes/origin/main)
[[ "$TARGET" =~ ^[a-f0-9]{40}$ ]] || exit 1
RECOVERING=0
[[ ! -f "$STATE/pending" ]] || RECOVERING=1
if [[ -f "$STATE/pending" || -f "$STATE/blocked/$TARGET" ]]; then
  [[ ${2:-} = --retry ]] || { echo 'A backend deployment failed or was interrupted. Inspect protected logs, then use deploy --retry.' >&2; exit 1; }
fi
if [[ "$OPERATION" = poll && -f "$STATE/current" && $(cat "$STATE/current") = "$TARGET" ]]; then exit 0; fi
docker info >/dev/null
docker compose version >/dev/null
docker network inspect sail >/dev/null
PROJECT=$(docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' oryx_api)
SERVICE=$(docker inspect --format '{{index .Config.Labels "com.docker.compose.service"}}' oryx_api)
[[ "$PROJECT" =~ ^[a-z0-9][a-z0-9_-]*$ && "$SERVICE" = app ]] || { echo 'Expected the existing Compose-managed oryx_api app container.' >&2; exit 1; }
RELEASE="$STATE/releases/$TARGET"
SOURCE="$STATE/sources/$TARGET"
LOG="$STATE/logs/$TARGET-$(date -u +%Y%m%dT%H%M%SZ)-$$.log"
# Discord alerts for the deployments that actually run; unchanged polls stay silent.
source "$REPO/deploy/customer-backend-notify.sh"
NOTIFY_WEBHOOK=$(discord_webhook)
NOTIFY_ERROR_LOG="$STATE/logs/discord-last-error.log"
NOTIFY_HOST=${ORYX_BACKEND_NAME:-$(hostname -s 2>/dev/null || hostname)}
STAGE=preparing
STOPPED=0
MIGRATION_STARTED=0
cleanup() { [[ ! -d "$SOURCE" ]] || git -C "$REPO" worktree remove --force "$SOURCE" >>"$LOG" 2>&1 || true; }
failure() {
  local result=$?
  trap - ERR
  set +e
  touch "$STATE/blocked/$TARGET"
  printf 'failed %s %s\n' "$TARGET" "$STAGE" > "$STATE/last-result"
  # Before migration starts, the untouched original API can be restarted safely.
  if [[ "$STOPPED" = 1 && "$MIGRATION_STARTED" = 0 && "$RECOVERING" = 0 ]]; then docker start oryx_api >>"$LOG" 2>&1; fi
  if [[ "$MIGRATION_STARTED" = 1 ]]; then compose runtime stop app >>"$LOG" 2>&1; fi
  echo "Backend deployment failed during $STAGE. Log: $LOG" >&3
  if [[ "$MIGRATION_STARTED" = 1 ]]; then echo "The API is kept stopped for recovery; database backup: ${BACKUP:-not-created}. No schema downgrade was attempted." >&3; fi
  local api_state='previous API still running'
  if [[ "$MIGRATION_STARTED" = 1 ]]; then api_state='API STOPPED for recovery, backup kept'
  elif [[ "$STOPPED" = 1 && "$RECOVERING" = 0 ]]; then api_state='previous API restarted'
  elif [[ "$STOPPED" = 1 ]]; then api_state='API stopped (was recovering)'; fi
  notify "🔴 ORYX Backend deployment FAILED | $NOTIFY_HOST | main ${TARGET:0:8} | stage: $STAGE | $api_state | run: bash deploy.sh deploy --retry after checking the log"
  exit "$result"
}
trap failure ERR
trap cleanup EXIT
mkdir -p "$RELEASE"
if [[ -d "$SOURCE" ]]; then
  [[ $(git -C "$SOURCE" rev-parse HEAD) = "$TARGET" && -z $(git -C "$SOURCE" status --porcelain) ]] || { echo 'Interrupted build source is dirty or names another commit.' >&2; false; }
else
  git -C "$REPO" worktree add --detach "$SOURCE" "$TARGET" >>"$LOG" 2>&1
fi
# Freeze the Compose definition of the commit being deployed (not the installed checkout's, which only
# changes when the tooling is pulled) with this server's settings. Relative bind paths still resolve
# against the installed checkout.
cp "$SOURCE/docker-compose.yml" "$RELEASE/compose.yml"
cp "$ENV_FILE" "$RELEASE/runtime.env"
printf '{"services":{"app":{"image":"oryx-api-customer:%s","environment":{"Database__ApplyMigrationsOnStartup":"false"}}}}\n' "$TARGET" > "$RELEASE/runtime.json"
printf '{"services":{"app":{"image":"oryx-api-customer-migration:%s","environment":{"Database__ApplyMigrationsOnStartup":"false"}}}}\n' "$TARGET" > "$RELEASE/migration.json"
compose() {
  local overlay=$1; shift
  # Ambient Cron/shell variables cannot override the frozen customer configuration.
  env -i PATH="$PATH" HOME="$HOME" docker compose --project-name "$PROJECT" --project-directory "$REPO" \
    --env-file "$RELEASE/runtime.env" -f "$RELEASE/compose.yml" -f "$RELEASE/$overlay.json" "$@"
}
compose runtime config --format json > "$RELEASE/resolved.json" 2>>"$LOG"
DATABASE=$(python3 - "$RELEASE/resolved.json" <<'PY'
import json,re,sys
config=json.load(open(sys.argv[1]))
connection=config["services"]["app"]["environment"]["connectionString"]
# This is the installed Compose contract. Never dump a guessed or different database.
assert connection.startswith("Host=postgres_db;Port=5432;")
database=connection.rsplit(";Database=",1)[1]
assert re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*",database)
print(database)
PY
)
docker inspect postgres_db >/dev/null 2>>"$LOG"
# Email (password resets, new-user setup) needs these in the API container. Reported, never blocking.
MISSING_SETTINGS=$(python3 - "$RELEASE/resolved.json" <<'PY'
import json,sys
env=json.load(open(sys.argv[1]))["services"]["app"].get("environment") or {}
print(", ".join(n for n in ("SMTP_HOST","SMTP_USERNAME","SMTP_PASSWORD") if not str(env.get(n) or "").strip()))
PY
) || MISSING_SETTINGS='(settings check could not run)'
STAGE=building
START_NOTE=building
if [[ ${2:-} = --retry ]]; then START_NOTE='building (retry)'; fi
notify "🟡 ORYX Backend deployment STARTED | $NOTIFY_HOST | main ${TARGET:0:8} | $START_NOTE"
if [[ -n "$MISSING_SETTINGS" ]]; then
  echo "Warning: email will not work. Missing from $ENV_FILE: $MISSING_SETTINGS" >&3
  notify "🟠 ORYX Backend settings WARNING | $NOTIFY_HOST | email will not work: $MISSING_SETTINGS missing from the backend .env"
fi
STAGE=migration-history
cat > "$RELEASE/history.sql" <<'SQL'
SELECT to_regclass('public."__EFMigrationsHistory"') IS NOT NULL AS has_history \gset
\if :has_history
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
\endif
SQL
docker exec -i postgres_db sh -ceu 'export PGPASSWORD="${POSTGRES_PASSWORD:-}"; exec psql -X -qAt --set ON_ERROR_STOP=1 --username="${POSTGRES_USER:-postgres}" --dbname="$1" --file=-' sh "$DATABASE" < "$RELEASE/history.sql" > "$RELEASE/applied-migrations.txt" 2>>"$LOG"
python3 - "$SOURCE/INFRASTRUCTURE/Migrations" "$RELEASE/applied-migrations.txt" >>"$LOG" 2>&1 <<'PY'
import pathlib,re,sys
known=set()
for path in pathlib.Path(sys.argv[1]).glob('*.cs'):
    known.update(re.findall(r'\[Migration\("([^"]+)"\)\]',path.read_text(encoding='utf-8-sig')))
assert known, 'No migration metadata found in the selected main commit'
applied=set(pathlib.Path(sys.argv[2]).read_text().split())
assert not applied-known, 'Database contains migrations absent from this main commit: '+', '.join(sorted(applied-known))
PY
STAGE=building
docker build --file "$SOURCE/deploy.Dockerfile" --target runtime --build-arg "RELEASE_SHA=$TARGET" --tag "oryx-api-customer:$TARGET" "$SOURCE" >>"$LOG" 2>&1
docker build --file "$SOURCE/deploy.Dockerfile" --target migration --tag "oryx-api-customer-migration:$TARGET" "$SOURCE" >>"$LOG" 2>&1
STAGE=model-consistency
compose migration run --rm --no-deps --pull never -T --entrypoint dotnet app ef migrations has-pending-model-changes --no-build --configuration Release --project INFRASTRUCTURE/INFRASTRUCTURE.csproj --startup-project API/API.csproj >>"$LOG" 2>&1
echo "Built backend main $TARGET. Preparing $DATABASE for migration."
docker inspect --format '{{.Image}}' oryx_api > "$RELEASE/previous-image"
printf '%s\n' "$TARGET" > "$STATE/pending"
STAGE=stopping-api
compose runtime stop app >>"$LOG" 2>&1
STOPPED=1
STAGE=backup
BACKUP="$STATE/backups/$TARGET-$(date -u +%Y%m%dT%H%M%SZ)-$$.dump"
docker exec postgres_db sh -ceu 'export PGPASSWORD="${POSTGRES_PASSWORD:-}"; exec pg_dump --format=custom --username="${POSTGRES_USER:-postgres}" --dbname="$1"' sh "$DATABASE" > "$BACKUP" 2>>"$LOG"
[[ -s "$BACKUP" ]] || { echo 'Database backup is empty.' >&2; false; }
docker exec -i postgres_db pg_restore --list < "$BACKUP" >>"$LOG" 2>&1
sha256sum "$BACKUP" > "$BACKUP.sha256"
STAGE=migration
MIGRATION_STARTED=1
compose migration run --rm --no-deps --pull never -T app >>"$LOG" 2>&1
STAGE=deploying
compose runtime up --detach --no-deps --no-build --pull never app >>"$LOG" 2>&1
STAGE=health
healthy=0
for attempt in {1..45}; do
  code=$(curl --silent --show-error --max-time 5 --output /dev/null --write-out '%{http_code}' "$HEALTH" 2>>"$LOG") || code=000
  if [[ "$code" = 200 || "$code" = 401 || "$code" = 403 ]]; then healthy=1; break; fi
  sleep 2
done
[[ "$healthy" = 1 ]] || { echo 'The updated API did not pass its health probe.' >&2; false; }
[[ $(docker inspect --format '{{.Image}}' oryx_api) = $(docker image inspect --format '{{.Id}}' "oryx-api-customer:$TARGET") ]] || { echo 'The API container is running a different image.' >&2; false; }
printf '%s\n' "$TARGET" > "$STATE/current.tmp"
mv "$STATE/current.tmp" "$STATE/current"
rm -f "$STATE/pending" "$STATE/blocked/$TARGET"
printf 'success %s\n' "$TARGET" > "$STATE/last-result"
echo "Backend main $TARGET built, backed up, migrated and deployed. Backup: $BACKUP"
notify "🟢 ORYX Backend deployment SUCCESS | $NOTIFY_HOST | main ${TARGET:0:8} | API healthy | backup: $(basename "$BACKUP")"
