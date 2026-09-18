#!/usr/bin/env bash
# Deploy (or refresh) the local DataGate demo stack.
# Usage:
#   ./tools/deploy-local.sh              # up --build, wait, seed Elsa
#   RUN_SMOKE=1 ./tools/deploy-local.sh  # also run gate-smoke after seed
#   SEED=0 ./tools/deploy-local.sh       # skip workflow seed
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

COMPOSE=(docker compose --env-file .env -f deploy/docker-compose.yml)
API_BASE="${API_BASE:-http://localhost:5001}"
ELSA_BASE="${ELSA_BASE:-http://localhost:13000}"
SEED="${SEED:-1}"
RUN_SMOKE="${RUN_SMOKE:-0}"

if [ ! -f .env ]; then
  echo "Creating .env from .env.example"
  cp .env.example .env
fi

echo "==> Building and starting stack"
"${COMPOSE[@]}" up -d --build

echo "==> Waiting for API ${API_BASE}"
ok=0
for i in $(seq 1 90); do
  if curl -sf "${API_BASE}/api/app/promotion/pending-list" >/dev/null; then
    echo "API healthy (attempt $i)"
    ok=1
    break
  fi
  sleep 5
done
if [ "$ok" -ne 1 ]; then
  echo "API did not become healthy" >&2
  "${COMPOSE[@]}" logs --no-color api | tail -80 >&2 || true
  exit 1
fi

echo "==> Waiting for Elsa ${ELSA_BASE}"
ok=0
for i in $(seq 1 60); do
  if curl -sf "${ELSA_BASE}/" >/dev/null; then
    echo "Elsa up (attempt $i)"
    ok=1
    break
  fi
  sleep 5
done
if [ "$ok" -ne 1 ]; then
  echo "Elsa did not become ready (seed may fail); continuing" >&2
fi

if [ "$SEED" = "1" ] && [ "$ok" = "1" ]; then
  echo "==> Seeding Elsa workflow definitions"
  ./tools/seed-workflows.sh || {
    echo "Seed failed — retry once after Elsa settles" >&2
    sleep 10
    ./tools/seed-workflows.sh
  }
else
  echo "==> Skipping seed (SEED=${SEED})"
fi

if [ "$RUN_SMOKE" = "1" ]; then
  echo "==> Post-deploy gate smoke"
  ./tools/gate-smoke.sh
fi

cat <<EOF

======== DataGate deployed ========
  Steward UI     ${API_BASE}
  Elsa Studio    ${ELSA_BASE}   (admin / password)
  Pending API    ${API_BASE}/api/app/promotion/pending-list

  Post a run / approve in the steward UI tabs.
  Tear down:  ${COMPOSE[*]} down -v
===================================
EOF
