#!/usr/bin/env bash
# Optional: fire a PlayerZero API Trigger (non-ServiceNow incident sources).
# Prefer ServiceNow → PlayerZero for incidents — see docs/playerzero-harness.md
# Docs: https://playerzero.ai/docs/features/additional-features/api-triggers
#
# Requires: PLAYERZERO_TRIGGER_TOKEN (token segment only).
# Optional: PLAYERZERO_TRIGGER_BASE (default https://sdk.playerzero.app/workflow/start)
# Skips cleanly when the token is unset (normal path).

set -euo pipefail

if [[ -z "${PLAYERZERO_TRIGGER_TOKEN:-}" ]]; then
  echo "playerzero-notify: PLAYERZERO_TRIGGER_TOKEN unset — skip"
  exit 0
fi

BASE="${PLAYERZERO_TRIGGER_BASE:-https://sdk.playerzero.app/workflow/start}"
URL="${BASE%/}/${PLAYERZERO_TRIGGER_TOKEN}"
SHA="$(git rev-parse --short HEAD 2>/dev/null || echo unknown)"
BRANCH="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo unknown)"
WHEN="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

BODY=$(cat <<EOF
{
  "message": "DataGate deploy completed",
  "service": "datagate",
  "gitSha": "${SHA}",
  "branch": "${BRANCH}",
  "deployedAt": "${WHEN}",
  "smokeHint": "POST /api/app/promotion/simulate clean → gold; dirty → pending"
}
EOF
)

echo "playerzero-notify: POST ${BASE%/}/<token>"
HTTP=$(curl -sS -o /tmp/pz-notify-body.json -w "%{http_code}" \
  -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d "$BODY")

cat /tmp/pz-notify-body.json 2>/dev/null || true
echo
if [[ "$HTTP" != "202" ]]; then
  echo "playerzero-notify: expected 202, got ${HTTP}" >&2
  exit 1
fi
echo "playerzero-notify: accepted"
