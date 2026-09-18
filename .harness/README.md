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

## Harness

1. Import `pipeline.yaml`; Delegate with Docker.
2. No PlayerZero secret required for CI/CD.
3. Incidents: connect **ServiceNow** in PlayerZero (not deploy notify).
4. `tools/playerzero-notify.sh` only if a non-SN system must hit an API Trigger.
