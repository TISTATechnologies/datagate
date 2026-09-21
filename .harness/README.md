# Harness + PlayerZero

**Import one file:** [`.harness/pipeline.yaml`](pipeline.yaml) → pipeline **datagate-ci-cd**  
(CI on Cloud → Deploy on Delegate).

Talk track: [`docs/demo-harness.md`](../docs/demo-harness.md) ·
CD / Delegate: [`docs/harness-cd.md`](../docs/harness-cd.md).

| File | Purpose |
|---|---|
| `pipeline.yaml` | **Import this** — validate + unit + deploy |
| `cd-pipeline.yaml` | Stub only (merged); do not import |

## Local

```bash
chmod +x tools/*.sh
./tools/deploy-local.sh
./tools/ci-local.sh all
```

## Import into Harness

1. Connector `datagate_github` with **API access** + Connect through Harness Platform.
2. Pipelines → Create / Edit → YAML path: **`.harness/pipeline.yaml`** (not a GitHub blob URL).
3. Save → Run → branch `main` (CI stages should go green on Cloud).
4. Install **Docker Delegate** ([docs/harness-cd.md](../docs/harness-cd.md)).
5. Edit **Deploy demo** stage → set infrastructure to that Delegate (not Cloud).
6. Delete the old separate `datagate-ci` / `datagate-cd` pipelines in the UI if you no longer need them.

### YAML Path

| Wrong | Right |
|---|---|
| `https://github.com/.../blob/main/.harness/pipeline.yaml` | `.harness/pipeline.yaml` |
