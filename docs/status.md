# DataGate — learning log

A running note of **what this project is**, **what we built so far**, and **why
it matters for the business**. Use it to stay oriented; deeper diagrams live in
[`architecture.md`](architecture.md).

---

## The business problem

Data pipelines usually promote **silver → gold**. Two bad outcomes show up often:

1. **Bad data reaches gold** — drift, null spikes, new PII columns, or a high-dollar
   table slips through with nobody accountable.
2. **Everything waits on a human** — every run is blocked, SLAs die, and the gate
   becomes theater instead of governance.

Business needs a **selective brake**: most clean runs fly through; risky ones pause
until the **right role** signs (steward → owner → director → CDO), with an audit
trail and a two-person rule when exposure or sensitive data is high.

That is what DataGate demos.

---

## How DataGate answers those needs

| Business need | How we address it |
|---|---|
| Don’t block every run | Clean metrics → **auto-promote** (milliseconds, no UI) |
| Don’t promote blind risk | Dirty metrics → **await steward** until approvals land |
| Right person by stakes | `DelegationOfAuthorityPolicy` picks tier from exposure / PII / regulatory flags |
| Two-person integrity | Same human can’t satisfy both signatures when policy requires 2 |
| Rules are reviewable | Thresholds live in **tested C#**, changed by PR — not buried in a flowchart |
| Process is visible | **Elsa** workflow JSON = wait / respond / resume (orchestration only) |
| Operators can act | Lightweight **steward UI** lists pending promotions and approve/reject |
| Auditors can ask “who signed?” | ABP `PromotionRequest` aggregate + approvals persisted in SQL Server |

**Mental model:** *rules decide; workflow waits; humans confirm; gold stays trusted.*

---

## What we built (journey so far)

### 1. Domain rules (the product brain)

- `DelegationOfAuthorityPolicy` — pure function: given run metrics, return
  auto-promote **or** required tier, approval count, SLA hours, and plain-English reasons.
- `PromotionRequest` — aggregate that enforces status, tier checks, and the
  two-person rule when someone approves.
- Unit tests keep thresholds honest when someone edits a number.

**Business takeaway:** changing “who must approve above \$2.5M” is a code review,
not a rediscovered tribal rule in a diagram.

### 2. Process orchestration (Elsa)

Elsa runs the promotion like a **pipeline of activities** (not the business rules —
those stay in C#). Definition: `workflow/definitions/gold-promotion-gate.json`.
Pipelines start it with `POST /workflows/quality-gate`.

```text
Trigger → CallGate → NeedsHuman ─┬─ false → AutoPromote (done)
                                  └─ true  → ParkedResponse → AwaitSteward → ApprovedPromote
```

| Activity | Elsa type | What it does |
|---|---|---|
| **Trigger** | `HttpEndpoint` | Starts on `POST /workflows/quality-gate` (run metrics body) |
| **CallGate** | `FlowSendHttpRequest` | Calls DataGate `evaluate` — **policy decision happens here** |
| **NeedsHuman** | `FlowDecision` | Branches on `requiresHuman` from the API |
| **AutoPromote** | `WriteHttpResponse` | Clean path → HTTP **200** `auto-promoted` |
| **ParkedResponse** | `WriteHttpResponse` | Dirty path → HTTP **202** `awaiting-steward` |
| **AwaitSteward** | `Event` | Pauses until signal `StewardDecision` (after steward approvals) |
| **ApprovedPromote** | `WriteLine` | Demo log line after resume (“promoting to gold…”) |

**Business takeaway:** ops can see the *process*; engineers own the *policy*.
Elsa Studio is for **definitions**. Prefer the steward UI for the live queue
(this stock Elsa image often shows an empty Instances list even when runs work).

### 3. ABP host + persistence

- Replaced the thin GateApi with open-source **ABP HttpApi.Host**
  (`DataGate.HttpApi.Host`) + EF Core on SQL Server.
- `PromotionAppService` — evaluate, pending list, approve, reject (demo still
  sends `approverId` + tier from the UI; full Identity comes later).

**Business takeaway:** promotions are durable records, not sticky notes in chat.

### 4. Steward console (lightweight UI)

- Static UI at http://localhost:5001 — tabs for **Post run**, **Approvals**, and **Gold**.
- **Post** fires metrics through Elsa (same webhook as simulate payloads).
- **Approvals** shows pending cards with a metrics snapshot + approve/reject.
- **Gold** is a demo ledger of auto-promoted / approved runs (not lakehouse rows).
- No full ABP Angular app; enough for a steward to clear the queue in a demo.

**Business takeaway:** governance only works if a human has somewhere obvious to act.

### 5. Local stack & tooling

| Piece | Role |
|---|---|
| SQL Server | Persists promotion requests |
| Elsa (`:13000`) | Webhook + Studio definitions |
| ABP host (`:5001`) | Policy API + steward UI |
| `tools/seed-workflows.sh` | Publishes the workflow definition |
| `tools/simulate-run.http` | Clean vs dirty sample payloads |

---

## End-to-end story (one picture in words)

1. A Databricks-style job finishes and posts **run metrics** (drift, nulls, exposure, PII…).
2. Elsa receives the webhook and asks DataGate: *promote or park?*
3. **Policy** answers with reasons and authority level.
4. If parked, a row appears on the **steward UI**; the right people approve (twice if required).
5. Gold only moves forward when policy + humans agree — with a record of *why* and *who*.

---

## Try it yourself

```bash
docker compose --env-file .env -f deploy/docker-compose.yml up -d --build
./tools/seed-workflows.sh
```

| Surface | URL |
|---|---|
| Steward UI (live pending queue) | http://localhost:5001 |
| Elsa Studio (definitions) | http://localhost:13000 (admin / password) |
| Gate APIs | `http://localhost:5001/api/app/promotion/*` |

Dirty run (should land in the steward UI):

```bash
curl -sS -X POST http://localhost:13000/workflows/quality-gate \
  -H 'Content-Type: application/json' \
  -d @- <<'EOF'
{"runId":"run-demo-dirty","table":"silver.claims","rowCount":412008,
 "rowCountDriftPct":61.7,"nullRatePct":14.3,"newColumns":["member_ssn"],
 "financialExposure":3000000,"containsSensitiveData":true,"isRegulatoryReporting":true}
EOF
```

Clean run should auto-promote with **no** pending card. Compare payloads in
`tools/simulate-run.http`. Architecture: `docs/architecture.md`. CI/CD
automation scope: `docs/cicd-automation.md`.

---

## Still ahead (honest backlog)

- Real login (ABP Identity / OpenIddict) instead of picking approver id in the UI
- Enforce `DataGatePermissions` (e.g. ApproveAsDirector) on the live host
- SLA auto-quarantine wired into the Elsa graph
- Elsa Instances visibility (stock image limitation) if we need Studio as the ops console
- CI demo jobs from `docs/cicd-automation.md` (security scan + gate-smoke beyond Elsa ping) → **implemented** in `.github/workflows/ci.yml`; run locally with `./tools/ci-local.sh`

---

## One-line recap

**DataGate makes silver→gold promotion safe for the business:** speed when data is
clean, accountable humans when it isn’t, and rules you can test and change in a PR.
