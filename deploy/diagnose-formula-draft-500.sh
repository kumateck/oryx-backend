#!/usr/bin/env bash
# Capture the API container logs around one customer-side formula draft save.
set -Eeuo pipefail
umask 077

API_CONTAINER=${ORYX_BACKEND_API_CONTAINER:-oryx_api}
STATE=${ORYX_BACKEND_STATE:-$HOME/.local/state/oryx-backend}
OUT_DIR="$STATE/diagnostics"

command -v docker >/dev/null || { echo 'Docker is required.' >&2; exit 1; }
docker inspect "$API_CONTAINER" >/dev/null 2>&1 || {
  echo "API container not found: $API_CONTAINER" >&2
  echo 'Set ORYX_BACKEND_API_CONTAINER if this server uses another container name.' >&2
  exit 1
}

mkdir -p "$OUT_DIR"
chmod 0700 "$STATE" "$OUT_DIR"
START_UTC=$(date -u +%Y-%m-%dT%H:%M:%SZ)
STAMP=$(date -u +%Y%m%dT%H%M%SZ)
LOG="$OUT_DIR/formula-draft-500-$STAMP.log"

echo "Capture is ready (UTC start: $START_UTC)."
echo 'Now reproduce the problem once: open the formula, save it as a draft, and wait for the error.'
echo 'After the 500 appears, return here and press Enter to collect the API logs.'
read -r _

if ! docker logs --since "$START_UTC" --timestamps "$API_CONTAINER" >"$LOG" 2>&1; then
  echo 'Could not read container logs. Try running this script with sudo.' >&2
  rm -f "$LOG"
  exit 1
fi
chmod 0600 "$LOG"

echo
echo "Saved restricted log: $LOG"
echo 'Relevant formula/error lines follow. Check the TraceId against the 500 response X-Trace-Id if available:'
if command -v rg >/dev/null; then
  rg -i -C 4 'formula|Unhandled exception|HTTP 5[0-9]{2}|Exception|fail|TraceId' "$LOG" || true
else
  grep -i -E -C 4 'formula|Unhandled exception|HTTP 5[0-9]{2}|Exception|fail|TraceId' "$LOG" || true
fi

echo
echo 'The full log may contain customer data. Share it only through the approved private support channel.'
