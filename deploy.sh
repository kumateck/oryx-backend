#!/usr/bin/env bash
set -Eeuo pipefail
if [[ $# = 0 ]]; then set -- deploy; fi
exec bash "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/deploy/customer-backend.sh" "$@"
