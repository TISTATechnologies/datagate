#!/usr/bin/env bash
# Shared gate smoke: API healthy + clean → gold + dirty → pending.
# Assumes compose stack is already up. Used by CI and tools/ci-local.sh.
set -euo pipefail

API_BASE="${API_BASE:-http://localhost:5001}"
ELSA_BASE="${ELSA_BASE:-http://localhost:13000}"

echo "==> Wait for API ${API_BASE}"
ok=0
for i in $(seq 1 90); do
  if curl -sf "${API_BASE}/api/app/promotion/pending-list" >/dev/null; then
    echo "API is up (attempt $i)"
    ok=1
    break
  fi
  sleep 5
done
if [ "$ok" -ne 1 ]; then
  echo "API did not become healthy" >&2
  exit 1
fi

echo "==> Wait for Elsa ${ELSA_BASE} (best-effort)"
for i in $(seq 1 30); do
  if curl -sf "${ELSA_BASE}/" >/dev/null; then
    echo "Elsa is up"
    break
  fi
  sleep 2
done

TS=$(date +%s)
CLEAN_ID="ci-clean-${TS}"
DIRTY_ID="ci-dirty-${TS}"

echo "==> Clean simulate (${CLEAN_ID})"
CLEAN_RESP=$(curl -sS -X POST "${API_BASE}/api/app/promotion/simulate" \
  -H 'Content-Type: application/json' \
  -d "{\"runId\":\"${CLEAN_ID}\",\"table\":\"silver.claims\",\"rowCount\":1048221,\"rowCountDriftPct\":0.4,\"nullRatePct\":0.2,\"newColumns\":[],\"financialExposure\":40000,\"containsSensitiveData\":false,\"isRegulatoryReporting\":false}")
echo "$CLEAN_RESP" | head -c 500
echo

echo "$CLEAN_RESP" | python3 -c '
import json,sys
d=json.load(sys.stdin)
code=d.get("statusCode") or d.get("StatusCode")
req=d.get("request") or d.get("Request") or {}
status=req.get("status") if isinstance(req, dict) else None
# AutoPromoted == 1; or HTTP 200 without requiresHuman
ok = code == 200 or status == 1 or (isinstance(req, dict) and req.get("requiresHuman") is False)
if not ok and code == 200:
    ok = True
if not ok:
    raise SystemExit(f"clean simulate unexpected: statusCode={code} status={status}")
print("clean OK")
'

echo "==> Assert clean on gold-list"
curl -sS "${API_BASE}/api/app/promotion/gold-list" | python3 -c '
import json,sys
run=sys.argv[1]
xs=json.load(sys.stdin)
ids=[x.get("runId") or x.get("RunId") for x in xs]
if run not in ids:
    raise SystemExit(f"{run} not in gold-list: {ids[:10]}")
print("gold-list OK")
' "$CLEAN_ID"

echo "==> Dirty simulate (${DIRTY_ID})"
DIRTY_RESP=$(curl -sS -X POST "${API_BASE}/api/app/promotion/simulate" \
  -H 'Content-Type: application/json' \
  -d "{\"runId\":\"${DIRTY_ID}\",\"table\":\"silver.claims\",\"rowCount\":412008,\"rowCountDriftPct\":61.7,\"nullRatePct\":14.3,\"newColumns\":[\"member_ssn\"],\"financialExposure\":3000000,\"containsSensitiveData\":true,\"isRegulatoryReporting\":true}")
echo "$DIRTY_RESP" | head -c 500
echo

echo "$DIRTY_RESP" | python3 -c '
import json,sys
d=json.load(sys.stdin)
code=d.get("statusCode") or d.get("StatusCode")
req=d.get("request") or d.get("Request") or {}
# AwaitingApproval == 2
status=req.get("status") if isinstance(req, dict) else None
ok = code == 202 or status == 2 or (isinstance(req, dict) and req.get("requiresHuman") is True)
if not ok:
    raise SystemExit(f"dirty simulate unexpected: statusCode={code} status={status}")
print("dirty OK")
'

echo "==> Assert dirty on pending-list"
curl -sS "${API_BASE}/api/app/promotion/pending-list" | python3 -c '
import json,sys
run=sys.argv[1]
xs=json.load(sys.stdin)
ids=[x.get("runId") or x.get("RunId") for x in xs]
if run not in ids:
    raise SystemExit(f"{run} not in pending-list: {ids[:10]}")
print("pending-list OK")
' "$DIRTY_ID"

echo "==> Gate smoke passed"
