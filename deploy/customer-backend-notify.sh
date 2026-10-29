#!/usr/bin/env bash
# Discord alerts for the customer backend pipeline. Sourced by customer-backend.sh.
# Alerts are best effort: a missing webhook or a failed post never changes a deployment's outcome.

# Prints the Discord webhook URL, or nothing when alerts are not configured.
# Reads ORYX_BACKEND_NOTIFY_CONFIG (default ~/.config/oryx-backend/notify.env), falling back to the
# Oryx Live customer configuration so one webhook can serve both pipelines. Files are parsed, never
# sourced, and must be protected (mode 600 or 400) like the backend .env.
discord_webhook() {
  local file mode value
  for file in "${ORYX_BACKEND_NOTIFY_CONFIG:-$HOME/.config/oryx-backend/notify.env}" "$HOME/.config/oryx-live/customer.env"; do
    [[ -f "$file" && ! -L "$file" ]] || continue
    mode=$(stat -c '%a' "$file" 2>/dev/null || stat -f '%Lp' "$file")
    if [[ "$mode" != 600 && "$mode" != 400 ]]; then
      echo "Ignoring $file for Discord alerts: protect it with chmod 600." >&2
      continue
    fi
    value=$(sed -n 's/^[[:space:]]*\(export[[:space:]][[:space:]]*\)\{0,1\}DISCORD_WEBHOOK_URL=//p' "$file" | tail -n 1)
    value=${value%$'\r'}
    value=${value%\"}; value=${value#\"}; value=${value%\'}; value=${value#\'}
    if [[ "$value" =~ ^https://(discord\.com|discordapp\.com)/api/webhooks/[A-Za-z0-9_/-]+$ ]]; then
      printf '%s' "$value"
      return 0
    fi
  done
  return 0
}

# Posts one message to Discord. The webhook stays out of argv and logs.
# Uses NOTIFY_WEBHOOK, NOTIFY_ERROR_LOG and file descriptor 3 (the operator's stderr).
notify() {
  [[ -n "${NOTIFY_WEBHOOK:-}" ]] || return 0
  local payload
  payload=$(python3 -c 'import json,sys; print(json.dumps({"content": sys.argv[1]}))' "$1") || return 0
  if ! curl --fail --silent --show-error --connect-timeout 5 --max-time 15 --retry 1 \
    --config <(printf 'url = "%s"\n' "$NOTIFY_WEBHOOK") -H 'Content-Type: application/json' \
    --data-binary "$payload" >/dev/null 2>"${NOTIFY_ERROR_LOG:-/dev/null}"; then
    echo 'Discord notification failed; see the notification error log.' >&3
  fi
  return 0
}
