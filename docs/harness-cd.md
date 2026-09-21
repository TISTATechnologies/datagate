# DataGate — Harness CD (next after CI)

**Goal:** after green CI, **deploy** the demo stack autonomously via Harness.
This is Continuous Delivery for our Docker Compose product — not Kubernetes
Blue-Green yet.

| Piece | What we use |
|---|---|
| Deploy command | `./tools/deploy-local.sh` (optional `RUN_SMOKE=1`) |
| Where it runs | **Harness Delegate** with Docker + Compose (not Harness Cloud) |
| After deploy | Steward UI on the Delegate host (`http://<host>:5001`) |

Harness Cloud CI runners **cannot** leave a lasting compose stack. CD for this
demo = Delegate machine that keeps containers running.

---

## Prerequisites

1. **CI green** — `datagate-ci-cd` validate + unit already works.
2. **CD / Free plan** — Account Settings → Subscriptions → enable **Continuous Delivery** if prompted (same pattern as CI Free).
3. **Delegate** on a machine that has:
   - Docker + Docker Compose
   - outbound HTTPS to Harness and GitHub
   - ports **5001** (API/UI) and **13000** (Elsa) free (or change compose)
4. GitHub connector `datagate_github` (API access on) — same as CI.

---

## Step 1 — Install a Delegate

1. Harness → project **datagate** → **Project Setup** → **Delegates** → **Install Delegate**.
2. Choose **Docker** delegate (simplest for this laptop/VM demo).
3. Copy the install command; run it on the machine that will host the stack.
4. Wait until Delegate status is **Connected**.
5. Note the **Delegate name** / tags (e.g. `datagate-delegate`) — pipeline steps must select it.

Official overview: [Harness Delegates](https://developer.harness.io/docs/platform/delegates/delegate-concepts/get-started-with-delegates/).

### If Delegate fails to connect

Error: *failed to connect to Harness SaaS* / *check pods on the cluster*

1. **Prefer Docker, not Kubernetes**, for this demo  
   Project Setup → Delegates → Install → **Docker**.  
   A K8s install needs a healthy cluster; compose CD does not need one.
2. **Outbound HTTPS** from the host/container to Harness (port **443**):

```bash
curl -sI https://app.harness.io | head -5
# Account Overview may show prod-2/prod-3 — also try:
# curl -sI https://app.harness.io/gratis | head -3
# curl -sI https://app3.harness.io | head -3
```

3. **Docker Delegate status / logs**

```bash
docker ps -a | grep -i delegate
docker logs -f <delegate-container-name>
```

4. Common fixes  
   - Corporate VPN/firewall blocking `app.harness.io` → allowlist or different network  
   - Proxy required → set `PROXY_HOST` / `PROXY_PORT` / `PROXY_SCHEME` on the Delegate ([proxy docs](https://developer.harness.io/docs/platform/delegates/manage-delegates/configure-delegate-proxy-settings/))  
   - Wrong manager URL for your cluster (Account Settings → Overview → Harness Cluster) — [install guide](https://developer.harness.io/docs/platform/tutorials/install-delegate/)  
   - Docker not running / out of CPU-memory (give Delegate ~1 CPU / 2GB)  
5. Delete the failed Delegate in Harness UI, reinstall fresh Docker command from the wizard.  
6. Until Connected: keep using `./tools/deploy-local.sh` on your laptop for the demo.

---

## Step 2 — Create the CD pipeline (UI)

Use the **Continuous Delivery & GitOps** module (switcher) *or* stay in CI and
add a Deploy stage that runs on the Delegate. For this compose demo, a **Shell /
Run on Delegate** deploy is enough and matches `deploy-local.sh`.

### Recommended: one pipeline, CI then CD

1. Open pipeline **datagate-ci-cd** (imported from `.harness/pipeline.yaml`).
2. Confirm **Validate and unit** uses Harness Cloud.
3. Open **Deploy demo** → set infrastructure to your **Delegate** (not Cloud).
4. Deploy step should run:

```bash
chmod +x tools/*.sh
cp -n .env.example .env || true
export RUN_SMOKE=1
./tools/deploy-local.sh
```

5. Save → Run (branch `main`).

YAML: [`.harness/pipeline.yaml`](../.harness/pipeline.yaml) (single **datagate-ci-cd** — import that file only).

### Alternative: CD module Service / Environment

Full CD (Service → Environment → Infrastructure → Deploy) is better when you
have Kubernetes or a fixed VM inventory. For compose-on-Delegate, Shell/Run is
the honest path; Blue-Green comes later with K8s.

---

## Step 3 — Wire trigger (autonomous ship)

1. Pipeline → **Triggers** → New → GitHub.
2. On push to `main` (and optionally after CI success only, if you use two pipelines).
3. First runs: start **manually** until Delegate deploy is green once.

---

## Step 4 — Demo what to say

> CI on Harness Cloud proves the build. CD on a Delegate runs the same
> `deploy-local.sh` we use on a laptop — stack stays up; stewards open the UI.

Show:

1. Green CI stages  
2. Green Deploy stage on Delegate  
3. Browser → `http://<delegate-host>:5001` — Post / Approvals / Gold  

---

## What we are *not* doing yet

| Later | Why wait |
|---|---|
| Kubernetes Blue-Green / Canary | Needs cluster + CD native steps |
| Harness Cloud for deploy | Ephemeral; no lasting demo UI |
| PlayerZero on every deploy | Incidents via ServiceNow only |

---

## Success criteria

1. Delegate **Connected**.
2. Pipeline run: CI green → Deploy green (`deploy-local.sh` + smoke).
3. Steward UI reachable on the Delegate host after the run.
4. Docs pitch: **CI proves · CD ships** ([`demo-harness.md`](demo-harness.md)).

Troubleshoot: if Deploy fails with Docker permission / compose missing, fix the
Delegate host first (`docker compose version`), not the pipeline YAML.
