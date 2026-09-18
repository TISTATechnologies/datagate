#!/usr/bin/env bash
# Run the same CI stages locally (see docs/cicd-automation.md).
# Usage:
#   ./tools/ci-local.sh              # full pipeline; tears down compose unless KEEP_UP=1
#   ./tools/ci-local.sh validate     # workflow JSON only
#   ./tools/ci-local.sh unit         # build + domain tests
#   ./tools/ci-local.sh security     # trivy fs if installed; else nuget vulnerable list
#   ./tools/ci-local.sh smoke        # compose up + gate-smoke (keeps stack if already up)
#   ./tools/ci-local.sh deploy       # lasting local deploy (compose + seed; leaves stack up)
#   ./tools/ci-local.sh sdlc         # pack docs + last TestResults into artifacts/sdlc-pack
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

STAGE="${1:-all}"
KEEP_UP="${KEEP_UP:-0}"
COMPOSE=(docker compose --env-file .env -f deploy/docker-compose.yml)
mkdir -p artifacts TestResults

need_env() {
  if [ ! -f .env ]; then
    echo "Creating .env from .env.example"
    cp .env.example .env
  fi
}

stage_validate() {
  echo "======== validate-workflows ========"
  shopt -s nullglob
  files=(workflow/definitions/*.json)
  if [ ${#files[@]} -eq 0 ]; then
    echo "No workflow definitions found" >&2
    exit 1
  fi
  for f in "${files[@]}"; do
    echo "checking $f"
    jq -e 'has("definitionId") and has("name") and has("root")' "$f" >/dev/null
  done
  echo "All workflow definitions valid."
}

stage_unit() {
  echo "======== build + unit tests ========"
  ./tools/bootstrap.sh
  dotnet test test/DataGate.Domain.Tests --no-build -c Release \
    --logger trx --results-directory TestResults
}

stage_security() {
  echo "======== security ========"
  if command -v trivy >/dev/null 2>&1; then
    trivy fs --severity HIGH,CRITICAL --exit-code 0 --format table -o artifacts/trivy-fs.txt .
    echo "Wrote artifacts/trivy-fs.txt"
  else
    echo "trivy not installed — skipping fs scan (CI uses aquasecurity/trivy-action)"
  fi
  if command -v dotnet >/dev/null 2>&1; then
    dotnet restore >/dev/null
    dotnet list package --vulnerable --include-transitive > artifacts/nuget-vulnerable.txt 2>&1 || true
    echo "Wrote artifacts/nuget-vulnerable.txt"
  fi
}

stage_smoke() {
  echo "======== gate-smoke ========"
  need_env
  "${COMPOSE[@]}" up -d --build
  ./tools/gate-smoke.sh
  if [ "$KEEP_UP" != "1" ] && [ "$STAGE" = "all" ]; then
    echo "Tearing down stack (KEEP_UP=1 to leave running)"
    "${COMPOSE[@]}" down -v
  elif [ "$STAGE" = "smoke" ]; then
    echo "Stack left running (smoke-only stage). Tear down: ${COMPOSE[*]} down -v"
  fi
}

stage_deploy() {
  echo "======== deploy ========"
  need_env
  RUN_SMOKE="${RUN_SMOKE:-0}" ./tools/deploy-local.sh
}

stage_sdlc() {
  echo "======== sdlc pack ========"
  PACK="artifacts/sdlc-pack"
  rm -rf "$PACK"
  mkdir -p "$PACK/docs" "$PACK/tests"
  cp docs/architecture.md docs/status.md docs/cicd-automation.md "$PACK/docs/" 2>/dev/null || true
  cp -R TestResults/* "$PACK/tests/" 2>/dev/null || true
  {
    echo "# DataGate SDLC pack"
    echo "Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
    echo
    echo "Contents: architecture, status, cicd-automation, unit test TRX (if present)."
  } > "$PACK/README.md"
  (cd artifacts && tar -czf sdlc-pack.tgz sdlc-pack)
  echo "Wrote artifacts/sdlc-pack.tgz"
}

case "$STAGE" in
  validate) stage_validate ;;
  unit) stage_unit ;;
  security) stage_security ;;
  smoke) KEEP_UP=1; stage_smoke ;;
  deploy) stage_deploy ;;
  sdlc) stage_sdlc ;;
  all)
    stage_validate
    stage_unit
    stage_security
    stage_smoke
    stage_sdlc
    echo "======== ci-local all passed ========"
    echo "Tip: lasting demo stack → ./tools/deploy-local.sh  (or ./tools/ci-local.sh deploy)"
    ;;
  *)
    echo "Unknown stage: $STAGE (validate|unit|security|smoke|deploy|sdlc|all)" >&2
    exit 1
    ;;
esac
