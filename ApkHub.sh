#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

if [[ "${OSTYPE:-}" == linux* ]]; then
    if ! "$ROOT/scripts/ensure_adb_linux.sh"; then
        echo "Warning: adb verification/install failed; continuing startup."
    fi
fi

if [[ -x "$ROOT/dist/ApkHub" ]]; then
    exec "$ROOT/dist/ApkHub" "$@"
fi

if [[ ! -d "$ROOT/.venv" ]]; then
    python3 -m venv "$ROOT/.venv"
fi

# shellcheck source=/dev/null
source "$ROOT/.venv/bin/activate"
python -m pip install --upgrade pip >/dev/null
python -m pip install -e "$ROOT" >/dev/null
exec python -m apk_hub "$@"
