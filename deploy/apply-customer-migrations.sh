#!/usr/bin/env bash
# Distributed with migrations.sql + manifest.json; no SDK or source build required.
set -Eeuo pipefail
umask 077
exec 3>&2
PACKAGE=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
REPO=${1:?Pass the absolute path to the installed backend checkout}
STATE=${ORYX_BACKEND_STATE:-$HOME/.local/state/oryx-backend}
API_CONTAINER=${ORYX_BACKEND_API_CONTAINER:-oryx_api}
DB_CONTAINER=${ORYX_BACKEND_DB_CONTAINER:-postgres_db}
[[ "$REPO" = /* && "$STATE" = /* && "$STATE" != / ]] || exit 1
[[ -d "$REPO" ]] || { echo 'The backend checkout directory does not exist.' >&2; exit 1; }
[[ "$API_CONTAINER" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ && "$DB_CONTAINER" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ ]] || { echo 'Invalid container name.' >&2; exit 1; }
mkdir -p "$STATE/backups" "$STATE/logs"
chmod 0700 "$STATE" "$STATE/backups" "$STATE/logs"
exec 9>"$STATE/deploy.lock"
flock -n 9 || { echo 'Another backend deployment holds the lock.' >&2; exit 75; }
[[ ! -f "$STATE/pending" || ${2:-} = --retry ]] || { echo 'Resolve the pending deployment, then pass --retry to resume.' >&2; exit 1; }
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
  if [[ "$STOPPED" = 1 && "$STARTED" = 0 && "$RECOVERING" = 0 && ${WAS_RUNNING:-false} = true ]]; then docker start "$API_CONTAINER" >>"$LOG" 2>&1; fi
  printf 'migration-failed %s; inspect %s\n' "$SOURCE_SHA" "$LOG" > "$STATE/last-result"
  if [[ "$STARTED" = 1 ]]; then
    echo "Migration failed after SQL started. Keep the API stopped for recovery. Inspect $LOG." >&3
  else
    echo "Migration preparation failed; no migration SQL was executed. Inspect $LOG." >&3
  fi
  exit "$result"
}
trap failure ERR
# Private snapshots can contain credentials. Never print them or ask users to paste them.
docker inspect --format '{{json .}}' "$API_CONTAINER" > "$RUN/api.json" 2>>"$LOG"
docker inspect --format '{{json .}}' "$DB_CONTAINER" > "$RUN/database.json" 2>>"$LOG"
python3 - "$RUN" <<'PY'
import json,pathlib,re,sys
root=pathlib.Path(sys.argv[1]); api=json.loads((root/'api.json').read_text()); db=json.loads((root/'database.json').read_text())
values=[entry.partition('=')[2] for entry in api['Config'].get('Env',[]) if entry.partition('=')[0].casefold()=='connectionstring']
assert len(values)==1, 'The selected API must expose its configured connectionString'
connection=values[0]; fields={}; position=0
while position<len(connection):
    if connection[position] in '; \t\r\n': position+=1; continue
    end=connection.find('=',position); assert end>=0, 'Invalid connection string key'
    key=connection[position:end].strip().casefold(); position=end+1
    key={'initial catalog':'database','server':'host'}.get(key,key)
    while position<len(connection) and connection[position].isspace(): position+=1
    value=''
    if position<len(connection) and connection[position] in "\"'":
        quote=connection[position]; position+=1; closed=False
        while position<len(connection):
            character=connection[position]; position+=1
            if character==quote:
                if position<len(connection) and connection[position]==quote: value+=quote; position+=1
                else: closed=True; break
            else: value+=character
        assert closed, 'Unterminated connection string value'
        while position<len(connection) and connection[position].isspace(): position+=1
        assert position==len(connection) or connection[position]==';', 'Invalid quoted connection string value'
    else:
        end=connection.find(';',position)
        if end<0: end=len(connection)
        value=connection[position:end].strip(); position=end
    fields[key]=value
database=fields.get('database',fields.get('initial catalog',''))
host=fields.get('host',fields.get('server','')); port=fields.get('port','5432')
assert database and '\x00' not in database and '\n' not in database, 'Database name is missing or invalid'
assert port=='5432', 'The selected database must use its internal PostgreSQL port 5432'
api_networks=api['NetworkSettings']['Networks']; db_networks=db['NetworkSettings']['Networks']
hosts=set()
for name in set(api_networks)&set(db_networks):
    network=db_networks[name]
    hosts.update(network.get('Aliases') or [])
    hosts.update(network.get('DNSNames') or [])
    hosts.update([db.get('Name','').lstrip('/'),db['Config'].get('Hostname',''),network.get('IPAddress','')])
assert host and host.casefold() in {item.casefold() for item in hosts if item}, 'The selected database container does not match the API connection host on a shared network'
assert db['State']['Running'], 'The database container is stopped'
(root/'target.json').write_text(json.dumps({'database':database,'wasRunning':bool(api['State']['Running'])}))
PY
DATABASE=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["database"])' "$RUN/target.json")
WAS_RUNNING=$(python3 -c 'import json,sys; print(str(json.load(open(sys.argv[1]))["wasRunning"]).lower())' "$RUN/target.json")
cat > "$RUN/history.sql" <<'SQL'
SELECT to_regclass('public."__EFMigrationsHistory"') IS NOT NULL AS has_history \gset
\if :has_history
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
\endif
SQL
psql_file() {
  docker exec -i "$DB_CONTAINER" sh -ceu 'export PGPASSWORD="${POSTGRES_PASSWORD:-}"; exec psql -X -qAt --set ON_ERROR_STOP=1 --username="${POSTGRES_USER:-postgres}" --dbname="$1" --file=-' sh "$DATABASE" < "$1"
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
docker inspect --format '{{.Image}}' "$API_CONTAINER" > "$RUN/previous-image"
docker stop "$API_CONTAINER" >>"$LOG" 2>&1
STOPPED=1
BACKUP="$STATE/backups/sql-$SOURCE_SHA-$(date -u +%Y%m%dT%H%M%SZ)-$$.dump"
docker exec "$DB_CONTAINER" sh -ceu 'export PGPASSWORD="${POSTGRES_PASSWORD:-}"; exec pg_dump --format=custom --username="${POSTGRES_USER:-postgres}" --dbname="$1"' sh "$DATABASE" > "$BACKUP" 2>>"$LOG"
[[ -s "$BACKUP" ]] || { echo 'The database backup is empty.' >&2; false; }
docker exec -i "$DB_CONTAINER" pg_restore --list < "$BACKUP" >>"$LOG" 2>&1
sha256sum "$BACKUP" > "$BACKUP.sha256"
STARTED=1
psql_file "$PACKAGE/migrations.sql" >>"$LOG" 2>&1
check_history complete
printf 'migrated %s; API remains stopped pending matching backend activation\n' "$SOURCE_SHA" > "$STATE/last-result"
echo "All package migrations installed in $DATABASE. Backup: $BACKUP"
echo "API remains stopped. Deploy backend $SOURCE_SHA (or a newer main containing these migrations) before restarting it."
