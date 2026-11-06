#!/usr/bin/env bash
# Sourced by the customer backend deployer; also provides init-secret, verify and rollback.
# Master secret stays root:root 0600. Only a one-shot, networkless helper reads it.
formula_secret_path() { printf '%s\n' "${ORYX_FORMULA_SECRET_FILE:-/opt/oryx/secrets/formula-service-token}"; }

formula_init_secret() {
  [[ $EUID = 0 ]] || { echo 'Run init-secret with sudo; the master secret must be root-owned.' >&2; return 1; }
  python3 - "$(formula_secret_path)" <<'PY'
import os,pathlib,secrets,stat,sys
path=pathlib.Path(sys.argv[1])
assert path.is_absolute() and path.name=='formula-service-token', 'Use an absolute formula-service-token path'
assert path.resolve()==path, 'Secret path must not contain symlinks'
path.parent.mkdir(mode=0o700,parents=True,exist_ok=True)
parent=path.parent.stat()
assert parent.st_uid==0 and stat.S_IMODE(parent.st_mode)==0o700, 'Secret directory must be root-owned mode 0700'
try:
    fd=os.open(path,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
except FileExistsError:
    info=path.lstat()
    assert stat.S_ISREG(info.st_mode) and info.st_uid==0 and stat.S_IMODE(info.st_mode)==0o600, 'Existing secret must be a root-owned regular file mode 0600'
    assert 32<=len(path.read_text().strip())<=512, 'Existing secret invalid; refusing to rotate it'
    print('Existing root-only formula secret retained.')
else:
    with os.fdopen(fd,'w') as file:
        file.write(secrets.token_hex(32)+'\n'); file.flush(); os.fsync(file.fileno())
    print('Created root-only formula secret. Keep it backed up securely.')
PY
}

formula_compose() {
  local release=$1; shift
  local project
  project=$(cat "$release/formula-project") || return
  env -i PATH="$PATH" HOME="$HOME" docker compose --project-name "$project" \
    --project-directory "$REPO" --env-file "$release/formula.env" -f "$release/formula.json" "$@"
}

formula_prepare() {
  local secret image platform volume
  secret=$(formula_secret_path)
  [[ "$secret" = /* && "${secret##*/}" = formula-service-token && "$secret" != *','* ]] || { echo 'Invalid formula secret path.' >&2; return 1; }
  local bundle=${ORYX_FORMULA_RELEASE_DIR:-$HOME/.local/share/oryx-formula/current}
  [[ "$bundle" = /* && -f "$bundle/manifest.json" && -f "$bundle/image.tar" ]] || { echo "Formula release bundle missing: $bundle. Install the desktop-owned formula bundle first." >&2; return 1; }
  local image_key
  image_key=$(python3 -c 'import json,re,sys; value=json.load(open(sys.argv[1]))["imageId"]; assert re.fullmatch("sha256:[a-f0-9]{64}",value); print(value[7:])' "$bundle/manifest.json")
  FORMULA_ID="$TARGET-$image_key"
  FORMULA_RELEASE="$STATE/formula-releases/$FORMULA_ID"
  mkdir -p "$FORMULA_RELEASE"
  local RELEASE=$FORMULA_RELEASE
  if [[ -f "$RELEASE/formula-release.json" ]]; then
    cmp -s "$bundle/manifest.json" "$RELEASE/formula-release.json" || { echo "Formula release identity already has a different manifest; refusing to overwrite it." >&2; return 1; }
  else cp "$bundle/manifest.json" "$RELEASE/formula-release.json"; fi
  cp "$SOURCE/deploy/customer-formula.compose.yml" "$RELEASE/formula-template.yml"
  python3 - "$RELEASE" "$TARGET" "$PROJECT" <<'PY'
import json,pathlib,re,sys
root=pathlib.Path(sys.argv[1]); sha=sys.argv[2]; project=sys.argv[3]
release=json.loads((root/'formula-release.json').read_text())
assert release['schemaVersion']=='oryx-formula-release-v1'
assert release['sourceRepository']=='kumateck/oryx-live-desktop'
assert re.fullmatch('[a-f0-9]{64}',release['sourceHash'])
assert re.fullmatch('[a-f0-9]{64}',release['archiveSha256'])
assert re.fullmatch('sha256:[a-f0-9]{64}',release['imageId'])
assert re.fullmatch('[a-f0-9]{40}',release['sourceSha'])
assert release['image']=='oryx-formula-calculation:'+release['sourceHash']
assert release['platform']=='linux/amd64', 'Approve a new platform/image release before changing architecture'
assert re.fullmatch('[a-f0-9]{64}',release['engineBuildHash'])
assert release['numericPolicyVersion']=='oryx-decimal-v1-half-up'
assert release.get('sourceDirty') is False, 'Refusing a formula bundle built from uncommitted source'
alias='oryx-formula-'+sha[:16]+'-'+release['imageId'][7:27]
volume=project+'-formula-secrets'
values={'FORMULA_SERVICE_IMAGE':release['imageId'],'FORMULA_SERVICE_PLATFORM':release['platform'],
        'FORMULA_SERVICE_ENGINE_BUILD_HASH':release['engineBuildHash'],
        'FORMULA_SERVICE_NUMERIC_POLICY_VERSION':release['numericPolicyVersion'],
        'FORMULA_SERVICE_ALIAS':alias,'FORMULA_SERVICE_SECRET_VOLUME':volume}
(root/'formula.env').write_text(''.join(f'{key}={value}\n' for key,value in values.items()))
(root/'formula-project').write_text(project+'-formula-'+sha[:16]+'-'+release['imageId'][7:27]+'\n')
(root/'formula-image-id').write_text(release['imageId']+'\n')
(root/'formula-secret-volume').write_text(volume+'\n')
(root/'formula-archive-sha256').write_text(release['archiveSha256']+'\n')
(root/'formula-app.json').write_text(json.dumps({'services':{'app':{
    'environment':{'FORMULA_RUNTIME_ENABLED':'true','FORMULA_SERVICE_BASE_URL':'http://'+alias+':3101/',
                   'FORMULA_SERVICE_AUTH_SECRET_FILE':'/run/secrets/formula-service-token',
                   'FORMULA_SERVICE_AUTH_SECRET':'','FORMULA_SERVICE_CLIENT_TIMEOUT_MS':'2500'},
    'volumes':['customer-formula-secrets:/run/secrets:ro']}},
    'volumes':{'customer-formula-secrets':{'external':True,'name':volume}}}))
PY
  env -i PATH="$PATH" HOME="$HOME" docker compose --project-name "$(cat "$RELEASE/formula-project")" \
    --project-directory "$REPO" --env-file "$RELEASE/formula.env" -f "$RELEASE/formula-template.yml" config --format json > "$RELEASE/formula.json"
  # Retain the exact archive independently of Docker pruning, for offline upgrades/rollback.
  local archive_hash archive
  archive_hash=$(cat "$RELEASE/formula-archive-sha256")
  archive="$STATE/formula-images/$archive_hash.tar"
  mkdir -p "$STATE/formula-images"
  [[ $(sha256sum "$bundle/image.tar" | cut -d ' ' -f 1) = "$archive_hash" ]] || { echo 'Formula image archive checksum mismatch.' >&2; return 1; }
  if [[ ! -f "$archive" ]]; then cp "$bundle/image.tar" "$archive.tmp"; mv "$archive.tmp" "$archive"; fi
  printf '%s\n' "$archive" > "$RELEASE/formula-archive"
  formula_restore_image "$RELEASE" "$archive_hash"
}

# Copy the root-only master secret into the release's Docker volume. The one-shot helper runs the
# locally built API image (reviewed backend source), not the delivered formula image, so an
# untrusted bundle never executes with the master secret mounted.
formula_provision_secret() {
  local helper_image=$1 secret volume
  secret=$(formula_secret_path)
  volume=$(cat "$FORMULA_RELEASE/formula-secret-volume") || return
  docker volume create "$volume" >/dev/null
  docker run --rm --network none --user 0 --read-only --cap-drop ALL --cap-add CHOWN --cap-add DAC_OVERRIDE \
    --security-opt no-new-privileges:true --pids-limit 32 --memory 64m --cpus 1 \
    --entrypoint /bin/sh \
    --mount "type=bind,src=${secret%/*},dst=/source,readonly" \
    --mount "type=volume,src=$volume,dst=/target" "$helper_image" -ceu '
      test "$(stat -c %u:%a /source)" = 0:700
      test ! -L /source/formula-service-token
      test -f /source/formula-service-token
      test "$(stat -c %u:%a /source/formula-service-token)" = 0:600
      test ! -L /target/formula-service-token
      token=$(tr -d "[:space:]" < /source/formula-service-token)
      test "${#token}" -ge 32 && test "${#token}" -le 512
      if test -e /target/formula-service-token; then
        existing=$(tr -d "[:space:]" < /target/formula-service-token)
        test "$existing" = "$token" || { echo "Secret mismatch; refusing automatic rotation." >&2; exit 1; }
      fi
      chown 0:0 /target
      chmod 0711 /target
      if test ! -e /target/formula-service-token; then
        umask 077
        rm -f /target/.token-new
        cat /source/formula-service-token > /target/.token-new
        chmod 0400 /target/.token-new
        chown 10001:10001 /target/.token-new
        mv /target/.token-new /target/formula-service-token
      fi
      test "$(stat -c %u:%a /target/formula-service-token)" = 10001:400
    '
}

# A present image is verified by its content-addressed ID; the archive is only hashed when it must be
# loaded (pass the already-verified hash to skip re-reading a freshly checked archive).
formula_restore_image() {
  local release=$1 verified=${2:-} image archive expected
  image=$(cat "$release/formula-image-id") || return
  if ! docker image inspect "$image" >/dev/null 2>&1; then
    archive=$(cat "$release/formula-archive") || return
    expected=$(cat "$release/formula-archive-sha256" 2>/dev/null || python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["archiveSha256"])' "$release/formula-release.json") || return
    if [[ "$verified" != "$expected" ]]; then
      [[ $(sha256sum "$archive" | cut -d ' ' -f 1) = "$expected" ]] || { echo 'Retained formula archive checksum mismatch.' >&2; return 1; }
    fi
    docker image load --input "$archive" || return
  fi
  [[ $(docker image inspect --format '{{.Id}}' "$image") = "$image" ]] || { echo 'Formula image identity mismatch.' >&2; return 1; }
}
# Keep the active and previous formula releases plus the newest ORYX_FORMULA_KEEP (default 5);
# remove older release snapshots, their stopped containers and archives nothing still references.
formula_prune() {
  local keep=${ORYX_FORMULA_KEEP:-5} dir id
  [[ "$keep" =~ ^[0-9]+$ && "$keep" -ge 2 ]] || keep=5
  local -a protected=("$@")
  [[ -d "$STATE/formula-releases" ]] || return 0
  local index=0
  while IFS= read -r dir; do
    index=$((index + 1))
    id=${dir##*/}
    [[ "$id" =~ ^[a-f0-9]{40}-[a-f0-9]{64}$ ]] || continue
    [[ $index -le $keep ]] && continue
    [[ " ${protected[*]} " = *" $id "* ]] && continue
    if [[ -f "$dir/formula.json" ]]; then formula_compose "$dir" rm --force --stop formula-calculation >/dev/null 2>&1 || true; fi
    rm -rf -- "$dir"
  done < <(ls -1dt "$STATE/formula-releases"/*/ 2>/dev/null | sed 's#/$##')
  local archive referenced
  referenced=$(cat "$STATE/formula-releases"/*/formula-archive 2>/dev/null || true)
  for archive in "$STATE/formula-images"/*.tar; do
    [[ -f "$archive" ]] || continue
    grep -Fxq -- "$archive" <<<"$referenced" || rm -f -- "$archive"
  done
}

formula_start() { formula_restore_image "$1" && formula_compose "$1" up --detach --no-build --pull never --wait --wait-timeout 90 formula-calculation; }
formula_verify() { docker exec oryx_api dotnet API.dll --formula-readiness; }

# Formula rollback keeps the CURRENT backend binary/schema, selects only the retained formula pin.
formula_rollback() {
  local selected=$1 current active previous log image api_image_overlay
  [[ "$selected" =~ ^[a-f0-9]{40}-[a-f0-9]{64}$ ]] || { echo 'Pass a retained formula release ID.' >&2; return 1; }
  current=$(cat "$STATE/current")
  [[ ! -f "$STATE/pending" && -f "$STATE/formula-current" ]] || { echo 'Resolve pending backend recovery before formula rollback.' >&2; return 1; }
  active=$(cat "$STATE/formula-current")
  [[ "$current" =~ ^[a-f0-9]{40}$ && "$active" =~ ^[a-f0-9]{40}-[a-f0-9]{64}$ ]] || { echo "Invalid retained release pointer." >&2; return 1; }
  previous="$STATE/formula-releases/$active"
  local chosen="$STATE/formula-releases/$selected" backend="$STATE/releases/$current"
  [[ -f "$chosen/formula.json" && -f "$chosen/formula-app.json" ]] || { echo 'Retained formula release is missing.' >&2; return 1; }
  log="$STATE/logs/formula-rollback-$(date -u +%Y%m%dT%H%M%SZ)-$$.log"
  formula_start "$chosen" >>"$log" 2>&1 || { echo "Formula rollback dependency failed; active API unchanged. Log: $log" >&2; return 1; }
  image=$(docker inspect --format '{{.Image}}' oryx_api) || return
  [[ "$image" =~ ^sha256:[a-f0-9]{64}$ ]] || { echo 'Invalid current API image identity.' >&2; return 1; }
  api_image_overlay="$log.api-image.json"
  printf '{"services":{"app":{"image":"%s"}}}\n' "$image" > "$api_image_overlay"
  # Persist intent so a killed rollback cannot be mistaken for successful activation.
  printf '%s\n' "$selected" > "$STATE/formula-pending"
  formula_activate_api() {
    env -i PATH="$PATH" HOME="$HOME" docker compose --project-name "$PROJECT" --project-directory "$REPO" \
      --env-file "$backend/runtime.env" -f "$backend/compose.yml" -f "$backend/runtime.json" \
      -f "$1/formula-app.json" -f "$api_image_overlay" up --detach --force-recreate --no-deps --no-build --pull never app
  }
  formula_check_api() {
    local health=${ORYX_BACKEND_HEALTH_URL:-http://127.0.0.1:8080/api/v1/user/authenticated} code
    [[ "$health" =~ ^http://127\.0\.0\.1:[0-9]+/api/v1/user/authenticated$ ]] || return 1
    for attempt in {1..45}; do
      code=$(curl --silent --show-error --max-time 5 --output /dev/null --write-out '%{http_code}' "$health") || code=000
      if [[ "$code" = 200 || "$code" = 401 || "$code" = 403 ]]; then
        [[ $(docker inspect --format '{{.Image}}' oryx_api) = "$image" ]] && formula_verify
        return
      fi
      sleep 2
    done
    return 1
  }
  if formula_activate_api "$chosen" >>"$log" 2>&1 && formula_check_api >>"$log" 2>&1; then
    printf '%s\n' "$selected" > "$STATE/formula-current.tmp"
    mv "$STATE/formula-current.tmp" "$STATE/formula-current"
    rm -f "$STATE/formula-pending"
    if [[ "$active" != "$selected" ]]; then formula_compose "$previous" stop formula-calculation >>"$log" 2>&1 || true; fi
    echo "Formula release selected: $selected. Backend binary unchanged. Log: $log"
  else
    if formula_activate_api "$previous" >>"$log" 2>&1 && formula_check_api >>"$log" 2>&1; then
      rm -f "$STATE/formula-pending"
      if [[ "$active" != "$selected" ]]; then formula_compose "$chosen" stop formula-calculation >>"$log" 2>&1 || true; fi
      echo "Formula rollback failed; previous formula configuration restored. Log: $log" >&2
    else
      docker stop oryx_api >>"$log" 2>&1 || true
      echo "Formula rollback and recovery failed; API stopped. Resolve formula-pending. Log: $log" >&2
    fi
    return 1
  fi
}

if [[ "${BASH_SOURCE[0]}" = "$0" ]]; then
  set -Eeuo pipefail
  umask 077
  REPO=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
  STATE=${ORYX_BACKEND_STATE:-$HOME/.local/state/oryx-backend}
  case "${1:-}" in
    init-secret) formula_init_secret;;
    verify) formula_verify;;
    rollback)
      [[ "$STATE" = /* && "$STATE" != / ]] || exit 1
      exec 9>"$STATE/deploy.lock"
      flock -n 9 || { echo 'Another backend deployment holds the lock.' >&2; exit 75; }
      PROJECT=$(docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' oryx_api)
      [[ "$PROJECT" =~ ^[a-z0-9][a-z0-9_-]*$ ]] || exit 1
      formula_rollback "${2:?Pass a retained formula release ID}";;
    *) echo 'Use init-secret, verify, or rollback <retained-formula-release-ID>.' >&2; exit 1;;
  esac
fi
