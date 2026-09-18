# DataGate — CI/CD automation scope

Identify **what can be automated** in the delivery pipeline for this repo, what
already runs, and a **lean demo** that proves test execution, security scanning,
and SDLC documentation packaging.

Related: [`architecture.md`](architecture.md), [`status.md`](status.md),
[`playerzero-harness.md`](playerzero-harness.md) (**target:** Harness CI/CD + PlayerZero),
[`.github/workflows/ci.yml`](../.github/workflows/ci.yml).

---

## Business framing

| Business need | CI/CD automation |
|---|---|
| Bad data / bad rules don’t ship | Policy unit tests + Elsa workflow JSON validation |
| Risky code changes are visible | Dependency / image / secrets scans on PR |
| Auditors want process evidence | Auto-packaged architecture + status + test reports |
| Demo is repeatable | Compose smoke: clean evaluate → gold; dirty → pending |

**Mental model:** the same split as the product — *rules are tested in CI;
process artifacts (workflow JSON) are validated; humans only appear where
governance requires them (steward UI), not in every pipeline step.*

---

## What already runs

From `.github/workflows/ci.yml` (push to `main`, PRs, `workflow_dispatch`):

| Job | Activity | Status |
|---|---|---|
| `validate-workflows` | Elsa definitions must be valid JSON with `definitionId`, `name`, `root` | **Done** |
| `build` | Bootstrap + `dotnet test` on `DataGate.Domain.Tests`; upload TRX | **Done** |
| `security` | Trivy fs (SARIF artifact) + NuGet vulnerable list | **Done** |
| `gate-smoke` | Compose up → API simulate clean/dirty → gold + pending asserts | **Done** |
| `sdlc-pack` | Bundle `docs/*` + test results → `sdlc-pack.tgz` artifact | **Done** |
| `deploy` (`.github/workflows/deploy.yml`) | `deploy-local.sh` on `main` / `workflow_dispatch` | **Done** |
| Local mirror | `./tools/ci-local.sh` + `./tools/deploy-local.sh` | **Done** |
| Harness | Stub in `.harness/` (CI + deploy + PlayerZero notify) | **Stub** (target CI/CD) |
| PlayerZero | GitHub App + `.pzignore` + `tools/playerzero-notify.sh` | **Scaffold** |

Run the same stages on a laptop:

```bash
chmod +x tools/*.sh
./tools/deploy-local.sh      # lasting demo (compose + seed)
./tools/ci-local.sh all      # validate → unit → security → smoke → sdlc
./tools/ci-local.sh deploy   # same as deploy-local.sh
```

See `.harness/README.md` for porting to Harness.

---

## Automation backlog (specific activities)

### 1. Test creation / execution

| Automate | Why it fits DataGate |
|---|---|
| Policy unit tests on every PR | Threshold edits fail CI before merge |
| Two-person approve tests | Keep `PromotionRequestTests` green as a merge gate |
| API contract smoke | Clean → auto / gold-list; dirty → pending-list; optional approve ×2 |
| Workflow artifact regression | Require CallGate evaluate URL + `requiresHuman` branch in JSON |
| Fixture generation from policy | Script known thresholds → `tools/fixtures/*.json` (not unsupervised “AI writes all tests”) |

**Demo beat:** PR that raises drift threshold → unit test fails → merge blocked.

### 2. Security scans

| Automate | Typical tooling |
|---|---|
| NuGet / transitive advisories | `dotnet list package --vulnerable`, Dependabot |
| SAST on C# | CodeQL or Semgrep |
| Container image scan | Trivy / Grype on the API image |
| Secrets scan | gitleaks / GitHub secret scanning |
| Dockerfile / compose hygiene | Hadolint + config scan |

**Demo beat:** intentional vulnerable package or fake secret → CI fails; report uploaded as artifact.

### 3. SDLC documentation generation

| Automate | Output |
|---|---|
| Bundle `docs/architecture.md` + `docs/status.md` + this file on release | Artifact or GitHub Pages |
| Test report packaging | TRX / HTML summary from `dotnet test` |
| Changelog from conventional commits | Release notes |
| Gate decision matrix | Generated from policy constants + tests (single source of truth) |

**Demo beat:** tag `v0.x` → pipeline uploads an “SDLC pack” (architecture + status + CI summary).

### 4. Build / deploy

| Automate | Note |
|---|---|
| Build & start stack | `./tools/deploy-local.sh` (compose up --build) |
| Seed Elsa definitions | Included in deploy-local (or `./tools/seed-workflows.sh`) |
| Post-deploy smoke | `RUN_SMOKE=1 ./tools/deploy-local.sh` |
| GitHub deploy workflow | `.github/workflows/deploy.yml` on `main` / dispatch |
| Harness deploy stage | `.harness/pipeline.yaml` → `./tools/deploy-local.sh` |

**Lasting demo:** run deploy on a workstation or self-hosted runner / delegate.
GitHub-hosted runners verify deploy then tear down (ephemeral).

---

## Recommended demo package (lean)

Three automated gates on PR / `workflow_dispatch` (extend current `ci.yml`):

```text
PR / push / workflow_dispatch
  ├─ 1. validate-workflows          (already)
  ├─ 2. build + domain unit tests   (already)
  ├─ 3. NEW: security
  │      Trivy fs (repo) and/or dotnet vulnerable packages
  └─ 4. NEW: gate-smoke
         docker compose --env-file .env -f deploy/docker-compose.yml up -d --build
         wait for API :5001 healthy (pending-list)
         POST simulate/evaluate clean → expect auto-promoted / gold-list hit
         POST simulate/evaluate dirty → pending-list contains run
         tear down
```

Optional fifth step: upload `docs/*.md` + `TestResults/` as **SDLC documentation pack**.

This covers the ask without overclaiming:

| Ask | Demo answer |
|---|---|
| Automatic test execution | Domain tests + gate-smoke |
| Automatic test-case creation | Fixture generation from policy thresholds (scripted) |
| Security scans | Trivy / vulnerable packages job |
| SDLC documentation generation | Artifact bundle of docs + test results |

---

## Suggested job sketches (for implementers)

### Security (illustrative)

```yaml
# job: security
# - checkout
# - aquasecurity/trivy-action (scan-type: fs, severity: HIGH,CRITICAL)
# - or: dotnet restore && dotnet list package --vulnerable --include-transitive
```

### Gate smoke (illustrative)

```yaml
# job: gate-smoke
# needs: validate-workflows
# - docker compose --env-file .env -f deploy/docker-compose.yml up -d --build
# - wait loop: curl -sf http://localhost:5001/api/app/promotion/pending-list
# - curl POST .../simulate clean → assert statusCode 200 / viaElsa or request.status AutoPromoted
# - curl POST .../simulate dirty → assert pending-list length increases
# - always: compose down -v
```

Use the steward UI **Post run** samples as the source of truth for payload shapes
(`tools/simulate-run.http`).

---

## Out of scope for the first demo

- Full browser UI E2E (fragile for a steward console demo)
- Real Databricks / lakehouse gold table promotion (not in this Docker stack)
- Asserting Elsa Studio **Instances** list (stock image often leaves it empty)
- Unsupervised AI “write all our tests” as the headline (weak audit story next to a governance product)

---

## Mapping to repo layout

| Path | Role in CI/CD |
|---|---|
| `test/DataGate.Domain.Tests` | Fast merge gate (no Docker) |
| `workflow/definitions/*.json` | Versioned process artifact |
| `deploy/docker-compose.yml` | Integration / smoke environment |
| `tools/seed-workflows.sh` | Post-deploy process publish |
| `tools/simulate-run.http` | Human + CI payload reference |
| `docs/*` | SDLC pack inputs |
| `.github/workflows/ci.yml` | Automation entrypoint |
| `.harness/` | Target CI/CD (import when Harness project exists) |
| `.pzignore` / `tools/playerzero-notify.sh` | PlayerZero ingest + post-deploy trigger |

**Target stack:** see [`playerzero-harness.md`](playerzero-harness.md) — Harness
owns pipelines; PlayerZero owns AI review / sims / post-deploy investigation.

---

## Success criteria for the automation demo

1. **Red PR:** change a policy threshold without updating tests → CI fails on unit tests.
2. **Green path:** clean simulate in gate-smoke → gold ledger / auto-promoted status.
3. **Dirty path:** dirty simulate → appears on pending-list.
4. **Security:** scan job produces a SARIF or log artifact on every PR.
5. **Docs:** one downloadable artifact containing architecture + status + this scope + test summary.

When those five are green (Actions or Harness), the deterministic CI story is
demo-ready. Wire PlayerZero + Harness per [`playerzero-harness.md`](playerzero-harness.md).
