#!/usr/bin/env bash
# Install .NET SDK matching global.json major if missing (Harness Cloud: 6/8/9 only).
# Installs into $DOTNET_ROOT or $HOME/.dotnet. Caller should prepend that to PATH.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

INSTALL_DIR="${DOTNET_ROOT:-$HOME/.dotnet}"
export DOTNET_ROOT="$INSTALL_DIR"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

WANT="$(jq -r '.sdk.version // empty' global.json 2>/dev/null || true)"
if [[ -z "$WANT" ]]; then
  echo "ensure-dotnet-sdk: no global.json sdk.version — skip"
  exit 0
fi
MAJOR="${WANT%%.*}"

if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -qE "^${MAJOR}\."; then
  echo "ensure-dotnet-sdk: OK major ${MAJOR} ($(dotnet --list-sdks | grep -E "^${MAJOR}\." | head -1))"
  exit 0
fi

echo "ensure-dotnet-sdk: installing channel ${MAJOR}.0 → ${INSTALL_DIR}"
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel "${MAJOR}.0" --install-dir "$INSTALL_DIR"

export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
if ! dotnet --list-sdks 2>/dev/null | grep -qE "^${MAJOR}\."; then
  echo "ensure-dotnet-sdk: major ${MAJOR} still missing after install" >&2
  dotnet --list-sdks >&2 || true
  exit 1
fi
echo "ensure-dotnet-sdk: ready ($(dotnet --version))"
