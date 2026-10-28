#!/usr/bin/env bash
# Distributed with migrations.sql + manifest.json; no SDK or source build required.
set -Eeuo pipefail
umask 077
exec 3>&2
PACKAGE=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
REPO=${1:?Pass the absolute path to the installed backend checkout}
STATE=${ORYX_BACKEND_STATE:-$HOME/.local/state/oryx-backend}
ENV_FILE=${ORYX_BACKEND_ENV_FILE:-$REPO/.env}
[[ "$REPO" = /* && "$STATE" = /* && "$STATE" != / ]] || exit 1
mkdir -p "$STATE/backups" "$STATE/logs"
chmod 0700 "$STATE" "$STATE/backups" "$STATE/logs"
exec 9>"$STATE/deploy.lock"
flock -n 9 || { echo 'Another backend deployment holds the lock.' >&2; exit 75; }
[[ ! -f "$STATE/pending" || ${2:-} = --retry ]] || { echo 'Resolve the pending deployment, then pass --retry to resume.' >&2; exit 1; }
[[ -f "$ENV_FILE" && ! -L "$ENV_FILE" ]] || exit 1
MODE=$(stat -c '%a' "$ENV_FILE" 2>/dev/null || stat -f '%Lp' "$ENV_FILE")
[[ "$MODE" = 600 || "$MODE" = 400 ]] || { echo 'Protect the backend .env with chmod 600.' >&2; exit 1; }
SOURCE_SHA=$(python3 - "$PACKAGE" <<'PY'
import hashlib,json,pathlib,re,sys
root=pathlib.Path(sys.argv[1]); manifest=json.loads((root/'manifest.json').read_text())
assert re.fullmatch(r'[a-f0-9]{40}',manifest['sourceSha'])
assert hashlib.sha256((root/'migrations.sql').read_bytes()).hexdigest()==manifest['sqlSha256'], 'SQL checksum mismatch'
assert manifest['migrations'] and all(re.fullmatch(r'[0-9]{14}_[A-Za-z0-9_]+',item) for item in manifest['migrations'])
print(manifest['sourceSha'])
PY
)
RUN="$STATE/logs/sql-$(date -u +%Y%m%dT%H%M%SZ)-$$"
mkdir -p "$RUN"
LOG="$RUN/apply.log"
STOPPED=0
STARTED=0
RECOVERING=0
[[ ! -f "$STATE/pending" ]] || RECOVERING=1
failure() {
  local result=$?
  trap - ERR
  set +e
  if [[ "$STOPPED" = 1 && "$STARTED" = 0 && "$RECOVERING" = 0 ]]; then docker start oryx_api >>"$LOG" 2>&1; fi
  printf 'migration-failed %s; inspect %s\n' "$SOURCE_SHA" "$LOG" > "$STATE/last-result"
  echo "Migration failed. Inspect $LOG. Once SQL has started, the API stays stopped for recovery." >&3
  exit "$result"
}
trap failure ERR
PROJECT=$(docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' oryx_api)
SERVICE=$(docker inspect --format '{{index .Config.Labels "com.docker.compose.service"}}' oryx_api)
[[ "$PROJECT" =~ ^[a-z0-9][a-z0-9_-]*$ && "$SERVICE" = app ]] || { echo 'Expected the installed Compose-managed API.' >&2; false; }
env -i PATH="$PATH" HOME="$HOME" docker compose --project-name "$PROJECT" --project-directory "$REPO" --env-file "$ENV_FILE" -f "$REPO/docker-compose.yml" config --format json > "$RUN/compose.json" 2>>"$LOG"
DATABASE=$(python3 - "$RUN/compose.json" <<'PY'
import json,re,sys
value=json.load(open(sys.argv[1]))['services']['app']['environment']['connectionString']
assert value.startswith('Host=postgres_db;Port=5432;')
database=value.rsplit(';Database=',1)[1]
assert re.fullmatch(r'[A-Za-z_][A-Za-z0-9_]*',database)
print(database)
PY
)
cat > "$RUN/history.sql" <<'SQL'
SELECT to_regclass('public."__EFMigrationsHistory"') IS NOT NULL AS has_history \gset
\if :has_history
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
\endif
SQL
psql_file() {
  docker exec -i postgres_db sh -ceu 'export PGPASSWORD="${POSTGRES_PASSWORD:-}"; exec psql -X -qAt --set ON_ERROR_STOP=1 --username="${POSTGRES_USER:-postgres}" --dbname="$1" --file=-' sh "$DATABASE" < "$1"
}
check_history() {
  psql_file "$RUN/history.sql" > "$RUN/history.txt" 2>>"$LOG"
  python3 - "$PACKAGE/manifest.json" "$RUN/history.txt" "$1" >>"$LOG" 2>&1 <<'PY'
import json,pathlib,sys
known=set(json.load(open(sys.argv[1]))['migrations']); applied=set(pathlib.Path(sys.argv[2]).read_text().split())
assert not applied-known, 'The database contains migrations absent from this package'
if sys.argv[3]=='complete': assert applied==known, 'The SQL did not install every migration'
PY
}
check_history preflight
printf 'migration-only %s\n' "$SOURCE_SHA" > "$STATE/pending"
docker inspect --format '{{.Image}}' oryx_api > "$RUN/previous-image"
docker stop oryx_api >>"$LOG" 2>&1
STOPPED=1
BACKUP="$STATE/backups/sql-$SOURCE_SHA-$(date -u +%Y%m%dT%H%M%SZ)-$$.dump"
docker exec postgres_db sh -ceu 'export PGPASSWORD="${POSTGRES_PASSWORD:-}"; exec pg_dump --format=custom --username="${POSTGRES_USER:-postgres}" --dbname="$1"' sh "$DATABASE" > "$BACKUP" 2>>"$LOG"
[[ -s "$BACKUP" ]] || { echo 'The database backup is empty.' >&2; false; }
docker exec -i postgres_db pg_restore --list < "$BACKUP" >>"$LOG" 2>&1
sha256sum "$BACKUP" > "$BACKUP.sha256"
STARTED=1
psql_file "$PACKAGE/migrations.sql" >>"$LOG" 2>&1
check_history complete
printf 'migrated %s; API remains stopped pending matching backend activation\n' "$SOURCE_SHA" > "$STATE/last-result"
echo "All package migrations installed in $DATABASE. Backup: $BACKUP"
echo "API remains stopped. Deploy backend $SOURCE_SHA (or a newer main containing these migrations) before restarting it."
