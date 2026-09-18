# Harness + PlayerZero

Harness = delivery. PlayerZero = **incidents** (prefer ServiceNow connector).
Architecture: [`docs/playerzero-harness.md`](../docs/playerzero-harness.md).

| File | Purpose |
|---|---|
| `pipeline.yaml` | validate → unit → security → smoke → sdlc → deploy → optional notify |

## Local

```bash
chmod +x tools/*.sh
./tools/deploy-local.sh
./tools/ci-local.sh all
```

## Harness (first pipeline)

### Fix connector error first
If you see *The given connector doesn't have api access field set*:

1. Project Setup → Connectors → `datagate_github` → Edit  
2. Enable **API access** → Personal Access Token → same PAT secret as auth  
3. Connectivity: **Connect through Harness Platform** (needed for Harness Cloud)  
4. Test connection → Save  

See [GitHub connector — Enable API access](https://developer.harness.io/docs/platform/connectors/code-repositories/ref-source-repo-provider/git-hub-connector-settings-reference/).

### YAML Path (Git Experience)
Use a **path inside the repo**, not a browser URL:

| Wrong | Right |
|---|---|
| `https://github.com/TISTATechnologies/datagate/blob/main/.harness/pipeline.yaml` | `.harness/pipeline.yaml` |

Repo: `datagate` · Branch: `main` · Connector: `datagate_github`

### Then
1. Save pipeline → Run → branch `main`.
2. Smoke/deploy later need a Delegate with Docker.
3. Incidents: ServiceNow → PlayerZero (no deploy notify required).
