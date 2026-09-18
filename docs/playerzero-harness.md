# DataGate — PlayerZero + Harness target stack

**Goal:** Harness runs delivery. PlayerZero is for **incidents** (and optional PR
insight) — not for gold promotion. Prefer **ServiceNow → PlayerZero** over
firing API Triggers on every deploy.

| Live link | Status |
|---|---|
| GitHub | https://github.com/TISTATechnologies/datagate |
| [Harness project `datagate`](https://app.harness.io/ng/account/w3CIbHK_T-yjEpzKqD-uuA/all/orgs/default/projects/datagate/overview) | Project up (`orgs/default`) |
| PlayerZero | Repo loaded / ingesting |

GitHub Actions can stay as a temporary PR gate until the first Harness CI run is green.

---

## Who owns what

| Concern | Owner | DataGate hook |
|---|---|---|
| Build, test, scan, smoke, deploy | **Harness** | `.harness/pipeline.yaml` → `./tools/ci-local.sh` / `deploy-local.sh` |
| Code index (so agents know this repo) | **PlayerZero** | GitHub App + `.pzignore` |
| **Incidents** | **ServiceNow → PlayerZero** | PlayerZero ServiceNow connector |
| Gold promote / steward approve | **Elsa + DataGate API** | Never PlayerZero / ServiceNow |

```text
Harness CI/CD
  validate → unit → security → gate-smoke → sdlc → deploy
                                                    (no PlayerZero required)

Incidents (primary PlayerZero path)
  ServiceNow incident
       → PlayerZero connector
       → AI investigation against this repo’s code
```

---

## Done vs next

### Done
- [x] Repo on GitHub (`TISTATechnologies/datagate`)
- [x] PlayerZero: repo loaded
- [x] Harness: project `datagate` created

### Next in Harness (wire delivery)

1. **Connect codebase** — Project Settings → Code Repo / GitHub connector → `TISTATechnologies/datagate`, branch `main`.
2. **Install a Delegate** that can run Docker Compose (laptop/VM where you demo is fine for now).
3. **Create pipeline** from [`.harness/pipeline.yaml`](../.harness/pipeline.yaml)  
   Identifiers already match: `orgIdentifier: default`, `projectIdentifier: datagate`, pipeline `datagate_ci`.
4. **Infrastructure** — point CI stages at Harness Cloud **or** the Delegate; deploy/smoke need Docker on the Delegate.
5. **Run once** — confirm stages call:
   - `./tools/ci-local.sh validate|unit|security|smoke|sdlc`
   - `./tools/deploy-local.sh`
6. **Trigger** — webhook / push to `main` (and optionally PRs).
7. Do **not** require `PLAYERZERO_TRIGGER_TOKEN` for a green deploy.

### Next in PlayerZero (incidents)

1. Confirm ingest finished; primary branch = `main`.
2. Settings → Context → Ticketing → **Connect ServiceNow**; grant this project.
3. Optional: PR reviews / code sims — not required for the incident story.

---

## Repo artifacts

| Path | Role |
|---|---|
| `.harness/pipeline.yaml` | Import into Harness project `datagate` |
| `.pzignore` | Keep `.env` / secrets out of PlayerZero ingest |
| `tools/playerzero-notify.sh` | Optional API Trigger only (non-SN sources) |
| `tools/ci-local.sh` | Same commands Harness Run steps should call |

---

## Success criteria

1. PlayerZero shows this repo indexed.
2. Harness pipeline run green on validate → unit (smoke/deploy once Delegate has Docker).
3. ServiceNow incident → PlayerZero can triage with DataGate code context.
4. Elsa gate still proves clean → gold / dirty → pending.
