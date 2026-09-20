# Harness + PlayerZero

Harness = delivery. PlayerZero = **incidents** (prefer ServiceNow connector).
Talk track: [`docs/demo-harness.md`](../docs/demo-harness.md) ·
CD steps: [`docs/harness-cd.md`](../docs/harness-cd.md).

| File | Purpose |
|---|---|
| `pipeline.yaml` | CI on Harness Cloud: validate + unit |
| `cd-pipeline.yaml` | CD: precheck + `deploy-local.sh` (run on **Delegate**) |

## Local

```bash
chmod +x tools/*.sh
./tools/deploy-local.sh
./tools/ci-local.sh all
```

## Harness CI (Cloud)

### Fix connector error first
If you see *The given connector doesn't have api access field set*:

1. Project Setup → Connectors → `datagate_github` → Edit  
2. Enable **API access** → Personal Access Token → same PAT secret as auth  
3. Connectivity: **Connect through Harness Platform** (needed for Harness Cloud)  
4. Test connection → Save  

### YAML Path (Git Experience)
Use a **path inside the repo**, not a browser URL:

| Wrong | Right |
|---|---|
| `https://github.com/.../blob/main/.harness/pipeline.yaml` | `.harness/pipeline.yaml` |

## Harness CD (next)

1. Follow [`docs/harness-cd.md`](../docs/harness-cd.md).
2. Install a **Docker Delegate** with Compose.
3. Import `cd-pipeline.yaml` (path `.harness/cd-pipeline.yaml`) or add Deploy stage.
4. Point Deploy at the Delegate (not Cloud) so the stack stays up.
5. Incidents: ServiceNow → PlayerZero (no deploy notify required).
