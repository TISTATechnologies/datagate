# DataGate demo — autonomous delivery with Harness

**Main goal of this demo:** show how a product team can **ship and verify
autonomously** using **Harness CI/CD** — not how to operate a lakehouse day to
day.

DataGate (silver→gold gate + steward UI) is the **sample product**. Harness is
the **delivery system** you would reuse on the next product.

PlayerZero / ServiceNow are **optional** and only for **incidents after ship** —
they are not the headline.

---

## One-sentence pitch

> Push to GitHub → Harness validates, tests, and (optionally) deploys the same
> way every time — humans only where governance requires it (dirty gold runs),
> not for every build.

---

## What “autonomous” means here

| Automated (Harness) | Still human (by design) |
|---|---|
| Workflow JSON validate | Steward approval when metrics are dirty |
| Unit tests (policy-as-code) | Org policy / connector setup (one-time) |
| Security scan (when wired) | Incident triage (ServiceNow → PlayerZero) |
| Same scripts locally + in cloud | — |
| Deploy demo stack on Delegate | — |

Autonomy ≠ “no humans ever.” It means **delivery is machine-driven**; **product
governance** (two-person promote) stays explicit.

---

## Demo beats (10–15 min)

1. **Product in one line** — DataGate auto-promotes clean silver→gold; parks risk for stewards. Show steward UI briefly if useful.
2. **Same commands everywhere** — `./tools/ci-local.sh validate` / `unit` on a laptop = what Harness runs.
3. **Harness pipeline** — open [project datagate](https://app.harness.io/ng/account/w3CIbHK_T-yjEpzKqD-uuA/all/orgs/default/projects/datagate/overview) → run `datagate-ci` → green validate + unit (.NET 10 install on Cloud if needed).
4. **Autonomy story** — “Next product: same pattern — repo scripts + Harness stages; no bespoke CI snowflake.”
5. **Optional coda** — incidents via ServiceNow → PlayerZero; gold promote stays Elsa, not AI.

---

## What to show on screen

| Show | Why |
|---|---|
| Harness execution (green stages) | Proof of autonomous CI |
| `.harness/pipeline.yaml` + `tools/ci-local.sh` | Pipeline-as-code, portable |
| Steward UI Post / Approvals / Gold | Product value (governed autonomy) |
| Skip deep Elsa Studio | Definitions ≠ the delivery story |

---

## Stretch (when ready)

| Capability | Unlocks |
|---|---|
| Push/PR **trigger** on `main` | Hands-off CI on every change |
| **CD on Delegate** | [`harness-cd.md`](harness-cd.md) — `deploy-local.sh` + smoke, lasting UI |
| security + sdlc stages | Full “ship with evidence” pack |
| ServiceNow → PlayerZero | Autonomous *ops* after ship |

---

## Harness modules (CI today, what else?)

We are on **Continuous Integration** in project `datagate`. Other modules in the
Harness switcher can join later — pick only what serves the autonomy story.

```text
Now:      Continuous Integration          ← validate + unit (live)
Next:     CD on Delegate (this guide)     ← docs/harness-cd.md + .harness/cd-pipeline.yaml
Later:    Continuous Delivery + K8s       ← Blue-Green / Canary
Optional: Security Testing Orchestration
Side:     PlayerZero + ServiceNow         ← incidents
```

### Fit for DataGate

| Module | Use? | Notes |
|---|---|---|
| **Continuous Integration** | **Yes — now** | `datagate-ci`: validate, unit; scripts via `ci-local.sh` |
| **CD (Delegate + compose)** | **Yes — next** | [`harness-cd.md`](harness-cd.md): install Delegate → `deploy-local.sh` → lasting UI |
| **Continuous Delivery & GitOps** (K8s) | Later | Native Blue-Green / Canary when we have a cluster |
| **Security Testing Orchestration** | Stretch | Prefer over only ad-hoc Trivy in a Run step |
| **Infrastructure as Code Management** | Later | Env-as-code; overkill for compose-only demo |
| **Feature Management & Experimentation** | Skip for demo | Flags for UI/policy — not the headline |
| **Resilience Testing** | After CD | Chaos against a lasting deployed API |
| **Service Reliability Management** | After CD | SLOs on a lasting env |
| **Database DevOps** | Skip for now | We use EF + compose SQL Server |
| **Artifact Registry** | Optional | If we push images to Harness instead of local build |
| **Code Repository** | Skip | Stay on GitHub (`TISTATechnologies/datagate`) |
| **Cloud & AI Cost Management** | Org FinOps | Not this product demo |
| **AI DLC Insights** | Optional | Platform analytics |
| **Continuous Error Tracking** | Skip | Avoid overlapping PlayerZero incident story |

**Pitch line:** “CI proves the product; CD ships it on a Delegate — same
`deploy-local.sh` as a laptop.” Full CD steps: [`harness-cd.md`](harness-cd.md).

**UI tip:** Project Setup → **Delegates** → install Docker Delegate → import
[`.harness/cd-pipeline.yaml`](../.harness/cd-pipeline.yaml) or add Deploy stage
after CI. Module switcher → **Continuous Delivery** when you move to K8s.

Blue / green in Harness usually means the **CD Blue-Green strategy** (two
versions, traffic swap), not CI status colors (green = success, red = failed).

---

## Success for *this* demo

1. Audience understands **Harness is how we build/ship products**, DataGate is the example.
2. One **green Harness run** (validate + unit) on the live project.
3. Clear split: **Harness = delivery autonomy**; **Elsa/steward = data governance**; **PlayerZero = incidents only**.
4. Audience knows **CI now → CD next**; other modules are optional, not the demo.

Setup detail: [`playerzero-harness.md`](playerzero-harness.md) ·
CD next: [`harness-cd.md`](harness-cd.md) · CI scope: [`cicd-automation.md`](cicd-automation.md).
