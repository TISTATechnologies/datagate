const listEl = document.getElementById("list");
const goldListEl = document.getElementById("goldList");
const postForm = document.getElementById("postForm");
const postResult = document.getElementById("postResult");

const tierNames = { 0: "None", 1: "DataSteward", 2: "DataOwner", 3: "Director", 4: "ChiefDataOfficer" };
const statusNames = {
  0: "Evaluating",
  1: "AutoPromoted",
  2: "AwaitingApproval",
  3: "Approved",
  4: "Rejected",
  5: "Quarantined",
};

const CLEAN = {
  runId: "run-ui-clean",
  table: "silver.claims",
  rowCount: 1048221,
  rowCountDriftPct: 0.4,
  nullRatePct: 0.2,
  newColumns: "",
  financialExposure: 40000,
  containsSensitiveData: false,
  isRegulatoryReporting: false,
};

const DIRTY = {
  runId: "run-ui-dirty",
  table: "silver.claims",
  rowCount: 412008,
  rowCountDriftPct: 61.7,
  nullRatePct: 14.3,
  newColumns: "member_ssn",
  financialExposure: 3000000,
  containsSensitiveData: true,
  isRegulatoryReporting: true,
};

/* —— tabs —— */
function showTab(name) {
  document.querySelectorAll(".tab").forEach((t) => {
    const on = t.dataset.tab === name;
    t.setAttribute("aria-selected", on ? "true" : "false");
  });
  document.querySelectorAll(".panel").forEach((p) => {
    const on = p.dataset.panel === name;
    p.hidden = !on;
    p.style.display = on ? "" : "none";
  });
  if (name === "approvals") loadApprovals();
  if (name === "gold") loadGold();
}

document.querySelectorAll(".tab").forEach((t) => {
  t.addEventListener("click", (e) => {
    e.preventDefault();
    e.stopPropagation();
    showTab(t.dataset.tab);
  });
});

showTab("post");

/* —— post —— */
function fillForm(sample) {
  const f = postForm;
  f.runId.value = `${sample.runId}-${Date.now().toString(36).slice(-4)}`;
  f.table.value = sample.table;
  f.rowCount.value = sample.rowCount;
  f.rowCountDriftPct.value = sample.rowCountDriftPct;
  f.nullRatePct.value = sample.nullRatePct;
  f.newColumns.value = sample.newColumns;
  f.financialExposure.value = sample.financialExposure;
  f.containsSensitiveData.checked = sample.containsSensitiveData;
  f.isRegulatoryReporting.checked = sample.isRegulatoryReporting;
}

document.getElementById("presetClean").addEventListener("click", () => fillForm(CLEAN));
document.getElementById("presetDirty").addEventListener("click", () => fillForm(DIRTY));
fillForm(DIRTY);

postForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const f = postForm;
  const cols = f.newColumns.value
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
  const body = {
    runId: f.runId.value.trim(),
    table: f.table.value.trim(),
    rowCount: Number(f.rowCount.value),
    rowCountDriftPct: Number(f.rowCountDriftPct.value),
    nullRatePct: Number(f.nullRatePct.value),
    newColumns: cols,
    financialExposure: Number(f.financialExposure.value),
    containsSensitiveData: f.containsSensitiveData.checked,
    isRegulatoryReporting: f.isRegulatoryReporting.checked,
  };

  postResult.hidden = false;
  postResult.textContent = "Submitting…";
  try {
    const res = await fetch("/api/app/promotion/simulate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const text = await res.text();
    let parsed;
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = text;
    }
    postResult.textContent = typeof parsed === "string" ? parsed : JSON.stringify(parsed, null, 2);
    if (res.ok) {
      const via = parsed.viaElsa ?? parsed.ViaElsa;
      const code = parsed.statusCode ?? parsed.StatusCode;
      const req = parsed.request ?? parsed.Request;
      if (code === 202 || req?.requiresHuman || req?.RequiresHuman) {
        showTab("approvals");
      } else if (code === 200) {
        showTab("gold");
      }
    }
  } catch (err) {
    postResult.textContent = err.message;
  }
});

/* —— approvals —— */
async function loadApprovals() {
  listEl.textContent = "Loading…";
  try {
    const res = await fetch("/api/app/promotion/pending-list");
    if (!res.ok) throw new Error(await res.text());
    const items = await res.json();
    if (!items.length) {
      listEl.innerHTML = '<p class="empty">No pending promotions. Use <strong>Post run</strong> with the dirty sample.</p>';
      return;
    }
    listEl.innerHTML = items.map(cardHtml).join("");
    listEl.querySelectorAll("[data-approve]").forEach((btn) =>
      btn.addEventListener("click", () => decide(btn.dataset.approve, btn.closest(".card"), "approve"))
    );
    listEl.querySelectorAll("[data-reject]").forEach((btn) =>
      btn.addEventListener("click", () => decide(btn.dataset.reject, btn.closest(".card"), "reject"))
    );
  } catch (e) {
    listEl.innerHTML = `<p class="error">${escapeHtml(e.message)}</p>`;
  }
}

document.getElementById("refreshApprovals").addEventListener("click", loadApprovals);

/* —— gold —— */
async function loadGold() {
  goldListEl.textContent = "Loading…";
  try {
    const res = await fetch("/api/app/promotion/gold-list");
    if (!res.ok) throw new Error(await res.text());
    const items = await res.json();
    if (!items.length) {
      goldListEl.innerHTML =
        '<p class="empty">Nothing in the gold ledger yet. Post a clean run, or approve a dirty one.</p>';
      return;
    }
    goldListEl.innerHTML = items.map(goldCardHtml).join("");
  } catch (e) {
    goldListEl.innerHTML = `<p class="error">${escapeHtml(e.message)}</p>`;
  }
}

document.getElementById("refreshGold").addEventListener("click", loadGold);

function goldCardHtml(item) {
  const status = item.status ?? item.Status;
  const statusLabel = typeof status === "number" ? statusNames[status] || status : status;
  const runId = escapeHtml(item.runId ?? item.RunId);
  const table = escapeHtml(item.table ?? item.Table);
  const reason = escapeHtml(item.reasonSummary ?? item.ReasonSummary ?? "");
  const goldName = table.replace(/^silver\./, "gold.");
  const isAuto = status === 1 || statusLabel === "AutoPromoted";

  return `
    <article class="card gold-card">
      <header>
        <div>
          <div class="run">${runId}</div>
          <div class="meta">${table} → <strong>${escapeHtml(goldName)}</strong></div>
        </div>
        <span class="badge ${isAuto ? "ok" : ""}">${escapeHtml(String(statusLabel))}</span>
      </header>
      <p class="reason">${reason}</p>
      ${metricsHtml(item)}
      ${signaturesHtml(item)}
    </article>`;
}

/* —— shared —— */
function escapeHtml(s) {
  return String(s)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function parseTriggers(reason) {
  const marker = "Triggered by:";
  const i = reason.indexOf(marker);
  if (i < 0) return { summary: reason, triggers: [] };
  const summary = reason.slice(0, i).trim();
  const triggers = reason
    .slice(i + marker.length)
    .split(",")
    .map((t) => t.trim())
    .filter(Boolean);
  return { summary, triggers };
}

function money(n) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(Number(n) || 0);
}

function metricsHtml(item) {
  const rows = item.rowCount ?? item.RowCount;
  const drift = item.rowCountDriftPct ?? item.RowCountDriftPct;
  const nulls = item.nullRatePct ?? item.NullRatePct;
  const exposure = item.financialExposure ?? item.FinancialExposure;
  const cols = (item.newColumns ?? item.NewColumns ?? "").trim();
  const sensitive = item.containsSensitiveData ?? item.ContainsSensitiveData;
  const regulatory = item.isRegulatoryReporting ?? item.IsRegulatoryReporting;

  const flags = [
    sensitive ? "Sensitive / PII" : null,
    regulatory ? "Regulatory reporting" : null,
  ].filter(Boolean);

  return `
    <dl class="metrics">
      <div><dt>Rows</dt><dd>${Number(rows).toLocaleString()}</dd></div>
      <div><dt>Drift</dt><dd>${Number(drift).toFixed(1)}%</dd></div>
      <div><dt>Null rate</dt><dd>${Number(nulls).toFixed(1)}%</dd></div>
      <div><dt>Exposure</dt><dd>${money(exposure)}</dd></div>
      ${cols ? `<div class="wide"><dt>New columns</dt><dd><code>${escapeHtml(cols)}</code></dd></div>` : ""}
      ${flags.length ? `<div class="wide"><dt>Flags</dt><dd>${flags.map(escapeHtml).join(" · ")}</dd></div>` : ""}
    </dl>`;
}

function signaturesHtml(item) {
  const sigs = item.signatures ?? item.Signatures ?? [];
  if (!sigs.length) return "";
  const rows = sigs
    .map((s) => {
      const tier = s.tier ?? s.Tier;
      const tierLabel = typeof tier === "number" ? tierNames[tier] || tier : tier;
      const id = String(s.approverId ?? s.ApproverId).slice(0, 8);
      const comment = escapeHtml(s.comment ?? s.Comment ?? "");
      return `<li><strong>${escapeHtml(tierLabel)}</strong> · ${escapeHtml(id)}… — ${comment}</li>`;
    })
    .join("");
  return `<div class="sigs"><div class="field">Signatures</div><ul>${rows}</ul></div>`;
}

function cardHtml(item) {
  const need = item.requiredApprovals ?? item.RequiredApprovals;
  const got = item.approvalsReceived ?? item.ApprovalsReceived;
  const tier = item.requiredTier ?? item.RequiredTier;
  const tierLabel = typeof tier === "number" ? tierNames[tier] || tier : tier;
  const id = item.id ?? item.Id;
  const runId = escapeHtml(item.runId ?? item.RunId);
  const table = escapeHtml(item.table ?? item.Table);
  const reason = item.reasonSummary ?? item.ReasonSummary ?? "";
  const { summary, triggers } = parseTriggers(reason);
  const sla = item.slaDueUtc ?? item.SlaDueUtc;
  const wf = item.workflowInstanceId ?? item.WorkflowInstanceId ?? "";
  const pct = need ? Math.min(100, Math.round((got / need) * 100)) : 0;

  const chips = triggers
    .map((t) => `<span class="trigger">${escapeHtml(t)}</span>`)
    .join("");

  const wfMeta = wf
    ? `<div class="meta">Elsa instance ${escapeHtml(wf)}</div>`
    : `<div class="meta">Approve here — Elsa Studio may not show an instance id</div>`;

  return `
    <article class="card" data-id="${id}" data-wf="${escapeHtml(wf)}">
      <header>
        <div>
          <div class="run">${runId}</div>
          <div class="meta">${table} · SLA ${new Date(sla).toLocaleString()}</div>
          ${wfMeta}
        </div>
        <span class="badge">${escapeHtml(tierLabel)} <span class="count">${got}/${need}</span></span>
      </header>
      <div class="progress" aria-hidden="true"><span style="width:${pct}%"></span></div>
      <p class="reason">${escapeHtml(summary)}</p>
      ${chips ? `<div class="triggers">${chips}</div>` : ""}
      ${metricsHtml(item)}
      ${signaturesHtml(item)}
      <label class="field">Comment
        <input class="comment" type="text" value="reviewed" />
      </label>
      <div class="actions">
        <button type="button" data-approve="${id}">Approve</button>
        <button type="button" class="secondary" data-reject="${id}">Reject</button>
      </div>
      <p class="error msg" hidden></p>
    </article>`;
}

function actor() {
  return {
    approverId: document.getElementById("approverId").value.trim(),
    approverTier: Number(document.getElementById("tier").value),
  };
}

async function decide(id, card, action) {
  const msg = card.querySelector(".msg");
  msg.hidden = true;
  const comment = card.querySelector(".comment").value.trim() || (action === "approve" ? "approved" : "rejected");
  const wf = card.dataset.wf || null;
  const body =
    action === "approve"
      ? { ...actor(), workflowInstanceId: wf, comment }
      : { approverId: actor().approverId, workflowInstanceId: wf, reason: comment };

  const res = await fetch(`/api/app/promotion/${id}/${action}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    msg.textContent = await res.text();
    msg.hidden = false;
    return;
  }
  if (action === "approve") {
    document.getElementById("approverId").value = crypto.randomUUID();
    const data = await res.json().catch(() => null);
    const status = data?.status ?? data?.Status;
    if (status === 3) showTab("gold");
    else await loadApprovals();
    return;
  }
  await loadApprovals();
}

setInterval(() => {
  const approvalsOn = document.getElementById("panel-approvals") && !document.getElementById("panel-approvals").hidden;
  const goldOn = document.getElementById("panel-gold") && !document.getElementById("panel-gold").hidden;
  if (approvalsOn) loadApprovals();
  if (goldOn) loadGold();
}, 15000);
