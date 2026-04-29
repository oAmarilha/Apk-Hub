#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

python3 -m venv .venv
# shellcheck source=/dev/null
source .venv/bin/activate
python -m pip install --upgrade pip
python -m pip install -e '.[build]'
python -m PyInstaller --clean --noconfirm ApkHub.spec

cat > dist/ApkHub.sh <<'RUNNER'
#!/usr/bin/env bash
set -euo pipefail
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$DIR/ApkHub" "$@"
RUNNER
chmod +x dist/ApkHub.sh

echo "Created dist/ApkHub and dist/ApkHub.sh"
