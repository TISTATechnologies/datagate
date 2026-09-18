#!/usr/bin/env bash
# Publishes every workflow definition in workflow/definitions to a running Elsa server.
# Usage: ELSA_URL=http://localhost:13000 ./tools/seed-workflows.sh
set -euo pipefail

ELSA_URL="${ELSA_URL:-http://localhost:13000}"
ELSA_API_KEY="${ELSA_API_KEY:-}"
ELSA_USER="${ELSA_USER:-admin}"
ELSA_PASSWORD="${ELSA_PASSWORD:-password}"

# ponytail: bash 3.2 + set -u treats an empty "${arr[@]}" as unbound
args=(-sS -X POST "${ELSA_URL}/elsa/api/workflow-definitions" -H "Content-Type: application/json")
if [ -n "$ELSA_API_KEY" ]; then
  args+=(-H "Authorization: ApiKey ${ELSA_API_KEY}")
else
  login=$(curl -sS -f -X POST "${ELSA_URL}/elsa/api/identity/login" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"${ELSA_USER}\",\"password\":\"${ELSA_PASSWORD}\"}")
  token=$(python3 -c 'import json,sys; print(json.load(sys.stdin)["accessToken"])' <<<"$login")
  args+=(-H "Authorization: Bearer ${token}")
fi

for f in workflow/definitions/*.json; do
  echo "Publishing $f"
  def_id=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["definitionId"])' "$f")
  body=$(python3 -c 'import json,sys; print(json.dumps({"model": json.load(open(sys.argv[1])), "publish": True}))' "$f")
  tmp=$(mktemp)
  code=$(curl "${args[@]}" -o "$tmp" -w "%{http_code}" --data-binary "$body")
  cat "$tmp"
  echo
  if [ "$code" -ge 400 ]; then
    echo "HTTP $code" >&2
    rm -f "$tmp"
    exit 1
  fi
  rm -f "$tmp"
  # ensure latest draft is the published version (seed can leave older published)
  pub_args=(-sS -X POST "${ELSA_URL}/elsa/api/workflow-definitions/${def_id}/publish")
  if [ -n "$ELSA_API_KEY" ]; then
    pub_args+=(-H "Authorization: ApiKey ${ELSA_API_KEY}")
  else
    pub_args+=(-H "Authorization: Bearer ${token}")
  fi
  curl "${pub_args[@]}" -H "Content-Type: application/json" -d '{}' || true
  echo
done
echo "Done. Open ${ELSA_URL} → Workflow Definitions (Instances list may stay empty on this image; use steward UI)."
