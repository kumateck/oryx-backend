#!/usr/bin/env bash
# Local disposable Docker test only; no existing server/container is selected.
set -Eeuo pipefail
umask 077
REPO=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
BUNDLE=${1:?Pass the desktop-owned formula bundle directory}
PROBE_IMAGE=${2:-oryx-formula-api-probe:local}
STATE=$(mktemp -d)
PROJECT="oryx-formula-smoke-$$"
SOURCE=$REPO
TARGET=0000000000000000000000000000000000000000
RELEASE="$STATE/backend"
ORYX_FORMULA_RELEASE_DIR=$BUNDLE
mkdir -p "$RELEASE"
source "$REPO/deploy/customer-formula.sh"
source_volume="$PROJECT-master"
network="$PROJECT-network"
cleanup() {
  result=$?
  trap - EXIT
  if [[ -n ${FORMULA_RELEASE:-} && -f "$FORMULA_RELEASE/formula.json" ]]; then
    formula_compose "$FORMULA_RELEASE" down >/dev/null 2>&1 || true
  fi
  docker volume rm "$source_volume" "$PROJECT-formula-secrets" >/dev/null 2>&1 || true
  docker network rm "$network" >/dev/null 2>&1 || true
  rm -rf "$STATE"
  exit "$result"
}
trap cleanup EXIT
image=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["imageId"])' "$BUNDLE/manifest.json")
docker image load --input "$BUNDLE/image.tar" >/dev/null
docker volume create "$source_volume" >/dev/null
docker network create "$network" >/dev/null
# A root-only master directory inside Docker's Linux VM (Mac hosts cannot chown host files to root).
docker run --rm --platform linux/amd64 --network none --user 0 --read-only --cap-drop ALL --mount "type=volume,src=$source_volume,dst=/source" --entrypoint node "$image" -e '
const fs=require("node:fs"); fs.chmodSync("/source",0o700); fs.writeFileSync("/source/formula-service-token",require("node:crypto").randomBytes(32).toString("hex"),{mode:0o600});'
ORYX_FORMULA_SECRET_FILE="$(docker volume inspect --format '{{.Mountpoint}}' "$source_volume")/formula-service-token"
formula_prepare
formula_provision_secret "$PROBE_IMAGE"
formula_prepare # Reuse the existing secret and retained image; must not rotate or fail.
formula_provision_secret "$PROBE_IMAGE"
python3 - "$FORMULA_RELEASE/formula.json" "$network" <<'PY'
import json,pathlib,sys
path=pathlib.Path(sys.argv[1]); config=json.loads(path.read_text()); config['networks']['sail']['name']=sys.argv[2]; path.write_text(json.dumps(config))
PY
formula_start "$FORMULA_RELEASE"
container=$(formula_compose "$FORMULA_RELEASE" ps --quiet formula-calculation)
docker inspect "$container" > "$STATE/inspect.json"
python3 - "$STATE/inspect.json" <<'PY'
import json,sys
container=json.load(open(sys.argv[1]))[0]; host=container['HostConfig']
assert container['State']['Health']['Status']=='healthy'
assert container['Config']['User']=='formula'
assert host['ReadonlyRootfs'] and 'ALL' in host['CapDrop']
assert host['Memory']==256*1024*1024 and host['PidsLimit']==64 and host['NanoCpus']==1000000000
assert 'no-new-privileges:true' in host['SecurityOpt']
assert not host['PortBindings']
assert all(not mount['RW'] for mount in container['Mounts'] if mount['Destination']=='/run/secrets')
PY
# Authenticated request is accepted by the service; unauthenticated request is rejected.
docker exec "$container" node -e '
(async()=>{const fs=require("node:fs"); const url="http://127.0.0.1:3101/internal/formula/v1/validate"; const bad=await fetch(url,{method:"POST",headers:{"content-type":"application/json"},body:"{}"}); if(bad.status!==401)process.exit(1); const good=await fetch(url,{method:"POST",headers:{"content-type":"application/json",authorization:"Bearer "+fs.readFileSync("/run/secrets/formula-service-token","utf8").trim()},body:"{}"}); const body=await good.json(); if(good.status!==200||body.schemaVersion!=="oryx-formula-service-response-v1"||body.engineBuildHash!==process.env.FORMULA_SERVICE_ENGINE_BUILD_HASH)process.exit(1)})().catch(()=>process.exit(1))'
# Known production-policy definition and evaluation must work through real bundled workers.
docker exec -i "$container" node -e '
(async()=>{const fs=require("node:fs");const requests=JSON.parse(fs.readFileSync(0,"utf8"));const token=fs.readFileSync("/run/secrets/formula-service-token","utf8").trim();for(const operation of ["validate","evaluate"]){const response=await fetch("http://127.0.0.1:3101/internal/formula/v1/"+operation,{method:"POST",headers:{"content-type":"application/json",authorization:"Bearer "+token},body:JSON.stringify(requests[operation])});const body=await response.json();if(response.status!==200||body.status!==5)throw new Error("Authoritative "+operation+" failed: "+body.statusName);if(operation==="evaluate"&&body.evaluation.results[0].roundedResult!=="0.50")throw new Error("Unexpected evaluation result");}})().catch(error=>{console.error(error.message);process.exit(1)})' < "$REPO/tests/deployment/formula-service-requests.json"
alias=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["services"]["app"]["environment"]["FORMULA_SERVICE_BASE_URL"])' "$FORMULA_RELEASE/formula-app.json")
probe() {
  docker run --rm --network "$network" --env "FORMULA_RUNTIME_ENABLED=$1" \
    --env "FORMULA_SERVICE_BASE_URL=$2" --env FORMULA_SERVICE_AUTH_SECRET_FILE=/run/secrets/formula-service-token \
    --mount "type=volume,src=$PROJECT-formula-secrets,dst=/run/secrets,readonly" --entrypoint dotnet "$PROBE_IMAGE" API.dll --formula-readiness
}
probe true "$alias"
if probe false "$alias"; then echo 'Disabled backend incorrectly passed readiness.' >&2; exit 1; fi
if probe true http://unreachable.invalid:3101/; then echo 'Unreachable backend dependency incorrectly passed readiness.' >&2; exit 1; fi
formula_compose "$FORMULA_RELEASE" stop formula-calculation >/dev/null
if probe true "$alias"; then echo 'Stopped service incorrectly passed readiness.' >&2; exit 1; fi
formula_start "$FORMULA_RELEASE"
probe true "$alias"
echo 'Scratch formula smoke passed: secret reuse, hardening, auth, authoritative validation/evaluation, backend readiness, disabled/unreachable/stopped failures and restart.'
