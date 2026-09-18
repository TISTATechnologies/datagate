# DataGate

A governed **silver → gold** promotion gate for data pipelines.

Pipelines often fail in one of two ways: bad data reaches gold unnoticed, or
every run is blocked on a human. DataGate sits in between. A Databricks-style
job posts run metrics to a webhook; an **Elsa 3** workflow either
**auto-promotes** a clean run or **suspends and waits** for a data steward when
drift, null spikes, new columns, or risk thresholds trip.

The design split matters:

| Concern | Where it lives | Why |
|---|---|---|
| Who may approve, by amount and risk | C# policy + ABP permissions | Pure functions, unit-tested, diffable in a PR |
| When to wait, escalate, quarantine | Elsa workflow JSON | Orchestration visible to non-engineers |
| Audit trail | ABP aggregates | Creator, modifier, timestamps for free |

**Rules are code with tests. Process is a versioned diagram.** Changing a
threshold is a pull request — not a redeploy of business logic, and not a
ticket that nobody can execute.

**Lightweight steward UI** at http://localhost:5001 — tabs for **Post run**,
**Approvals**, and **Gold**. Elsa Studio shows **definitions**; prefer the
steward UI for the live queue.

---

## Local setup

**Requirements:** Docker Desktop (or Docker Engine + Compose), `curl`, `jq`,
`python3`. Optional for full CI locally: .NET SDK matching `global.json`.

### One-command deploy (recommended)

```bash
chmod +x tools/*.sh
./tools/deploy-local.sh
```

This will:

1. Create `.env` from `.env.example` if needed  
2. `docker compose … up -d --build` (SQL + API + Elsa)  
3. Wait until the API is healthy  
4. Seed the Elsa **Gold Promotion Gate** workflow  

Optional:

```bash
RUN_SMOKE=1 ./tools/deploy-local.sh   # also run clean/dirty gate smoke
SEED=0 ./tools/deploy-local.sh        # skip Elsa seed
```

### Step by step (same result)

```bash
cp .env.example .env
docker compose --env-file .env -f deploy/docker-compose.yml up -d --build
./tools/seed-workflows.sh
```

### Open the app

| Surface | URL |
|---|---|
| Steward UI (Post / Approvals / Gold) | http://localhost:5001 |
| Elsa Studio | http://localhost:13000 (admin / password) |
| Pending API | `GET http://localhost:5001/api/app/promotion/pending-list` |

**Try it:** steward UI → **Post run** → Load dirty sample → Submit → **Approvals**
→ approve twice with different approver ids → **Gold**.  
Or use payloads in `tools/simulate-run.http`.

### Tear down

```bash
docker compose --env-file .env -f deploy/docker-compose.yml down -v
```

---

## CI / CD

| What | How |
|---|---|
| Continuous integration | Target: **Harness** (`.harness/pipeline.yaml`); interim: `.github/workflows/ci.yml` |
| Continuous deploy (demo) | `./tools/deploy-local.sh` on laptop or Harness Delegate |
| Incidents (AI triage) | **PlayerZero + ServiceNow** — see [`docs/playerzero-harness.md`](docs/playerzero-harness.md) |
| Local CI mirror | `./tools/ci-local.sh all` |

```bash
./tools/ci-local.sh validate   # workflow JSON
./tools/ci-local.sh unit       # build + domain tests
./tools/ci-local.sh security   # trivy if installed + nuget list
./tools/ci-local.sh smoke      # compose + gate asserts
./tools/ci-local.sh deploy     # lasting stack (alias of deploy-local)
./tools/ci-local.sh sdlc       # docs + test artifact pack
```

Details: [`docs/cicd-automation.md`](docs/cicd-automation.md),
[`docs/playerzero-harness.md`](docs/playerzero-harness.md).

**Note:** GitHub-hosted deploy jobs verify the deploy script then tear down
(ephemeral runners). A lasting demo stack runs on your machine or a
**self-hosted** runner / Harness delegate via `./tools/deploy-local.sh`.

---

## Layout

| Path | What it holds |
|---|---|
| `src/DataGate.Domain*` | Gate rules (`DelegationOfAuthorityPolicy`) and aggregates |
| `src/DataGate.EntityFrameworkCore/` | EF Core mapping (SQL Server) |
| `src/DataGate.Application*` | ABP app services (evaluate / approve / pending / simulate) |
| `src/DataGate.HttpApi.Host/` | ABP host + steward UI (`wwwroot`) |
| `docs/` | Learning log, architecture, CI/CD automation scope |
| `workflow/definitions/` | Elsa workflow definitions |
| `deploy/` | docker compose stack |
| `.github/workflows/` | CI + deploy workflows |
| `.harness/` | Harness CI/CD stub (+ PlayerZero notify) |
| `.pzignore` | PlayerZero ingest exclusions |
| `tools/` | deploy/ci/smoke/seed + `playerzero-notify.sh` |

---

## Later: ABP Identity

The host is open-source ABP (Autofac, MVC conventional controllers, EF Core)
without Identity/OpenIddict yet — the steward UI sends `approverId` + tier.
Add Identity when you need real login and `DataGatePermissions` enforcement.

```bash
dotnet tool install -g Volo.Abp.Studio.Cli
# optional: merge full identity template modules into HttpApi.Host
```

## Notes and constraints

- The Elsa image is a reference app, **not for production use**.
- Elsa's designer currently supports **Flowchart** activities only; this repo
  uses JavaScript expressions.
- The ABP Platform's bundled Elsa module is a **commercial** feature; this runs
  Elsa as a separate open-source service instead.
- Silver/gold **tables** are not in Docker — only promotion decisions. Real gold
  data lives in your lakehouse (e.g. Databricks).
