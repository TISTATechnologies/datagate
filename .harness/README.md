# Harness + PlayerZero

**Import one file:** [`.harness/pipeline.yaml`](pipeline.yaml)  
Display name **datagate-ci-cd**, identifier **`datagateci`** (must match existing Harness pipeline).  
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
3. Keep YAML `identifier: datagateci` if the pipeline already exists in Harness (changing it causes *Expected Pipeline identifier…* errors).
4. Save → Run → branch `main` (CI stages should go green on Cloud).
5. Install **Docker Delegate** ([docs/harness-cd.md](../docs/harness-cd.md)).
6. Edit **Deploy demo** stage → set infrastructure to that Delegate (not Cloud).

### YAML Path

| Wrong | Right |
|---|---|
| `https://github.com/.../blob/main/.harness/pipeline.yaml` | `.harness/pipeline.yaml` |
