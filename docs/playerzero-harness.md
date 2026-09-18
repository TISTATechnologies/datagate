# DataGate — PlayerZero + Harness target stack

**Goal:** Harness runs delivery. PlayerZero is for **incidents** (and optional PR
insight) — not for gold promotion. Prefer **ServiceNow → PlayerZero** over
firing API Triggers on every deploy.

GitHub Actions can stay as a temporary PR gate until Harness CI is live.

---

## Who owns what

| Concern | Owner | DataGate hook |
|---|---|---|
| Build, test, scan, smoke, deploy | **Harness** | `.harness/pipeline.yaml` → `./tools/ci-local.sh` / `deploy-local.sh` |
| Code index (so agents know this repo) | **PlayerZero** | GitHub App + `.pzignore` |
| **Incidents** | **ServiceNow → PlayerZero** | PlayerZero ServiceNow connector; triage / debug with code context |
| Gold promote / steward approve | **Elsa + DataGate API** | Never PlayerZero / ServiceNow |

```text
Harness CI/CD
  validate → unit → security → gate-smoke → sdlc → deploy
                                                    (no PlayerZero required)

Incidents (primary PlayerZero path)
  ServiceNow incident opened / updated
       → PlayerZero connector (read / triage / work notes)
       → AI investigation against this repo’s code

Optional only
  External system POST → PlayerZero API Trigger  (if SN isn’t the source)
```

**API Trigger:** useful when something *outside* ServiceNow must start a PlayerZero
Channel (pager webhook, custom monitor). For “incidents only,” the ServiceNow
connector is the better default — tickets stay the system of record.

---

## Setup checklist

### 1. PlayerZero + ServiceNow (incidents)

1. Create a PlayerZero project; [import this GitHub repo](https://playerzero.ai/docs/developer-guide/configuration-guides/importing-code/github); set primary branch.
2. Settings → Context → Ticketing → **Connect ServiceNow**; grant the DataGate project access.
3. Map a PlayerZero workflow for incident triage (read incident → investigate code → work notes).
4. Skip API Triggers unless a non-ServiceNow source must open Channels.

### 2. Harness (delivery)

1. Project `datagate`, import `.harness/pipeline.yaml`, Delegate with Docker.
2. Stages call `./tools/ci-local.sh <stage>` and `./tools/deploy-local.sh`.
3. Do **not** require `PLAYERZERO_TRIGGER_TOKEN` for a green deploy.
4. Trigger on PR / push to `main`.

Until Harness is connected: GitHub Actions + `./tools/ci-local.sh` (same stages).

---

## Repo artifacts

| Path | Role |
|---|---|
| `.harness/pipeline.yaml` | CI + deploy; notify stage optional / skippable |
| `.pzignore` | Keep `.env` / secrets out of ingest |
| `tools/playerzero-notify.sh` | Optional API Trigger helper (non-SN sources only) |
| `tools/ci-local.sh` | Commands Harness Run steps call |

---

## Success criteria

1. Repo indexed in PlayerZero.
2. ServiceNow incident in scope → PlayerZero can triage / comment with DataGate code context.
3. Harness (or Actions) green on `./tools/ci-local.sh all` **without** PlayerZero.
4. Elsa gate still proves clean → gold / dirty → pending.
