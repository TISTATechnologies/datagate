# Post-setup checklist — Incident RCA to Docs

Generated with the workflow file. These steps cannot be completed by the workflow
definition alone and must be finished after import.

## Required before the first run

- [ ] **Import the file and publish it.** Project Settings → Workflows → import
      `incident-rca-to-docs.workflow.json`, then publish the draft.
      If skipped: the workflow exists only as a draft and no run can start.

- [ ] **Confirm Confluence write scope is granted.** Stages: *Docs Grounding* (read)
      and *Publish and Close* (write). Read alone is not enough — the publish stage
      needs page create and update permission.
      If skipped: the run does all the work, then fails at the last step with nothing
      published. The drafts survive in the artifact, but someone has to paste them in
      by hand.

- [x] **Publish target is set.** Stages: *Documentation Draft* and *Publish and
      Close* now publish under the **PlayerZero** page in the **TD** space, as child
      pages — one runbook per failure mode, one running changelog page. That page was
      empty when checked, so the first run creates the structure rather than
      appending to anything.
      Change it in both stage instructions if you want a different destination.

- [ ] **Decide the incident trigger.** The entry stage accepts an incident from a
      connected ticketing system, a PlayerZero error or session, or a pasted report.
      Wire whichever is real for you — a ServiceNow incident event, an API trigger,
      or manual start.
      If skipped: the workflow only runs when someone starts it by hand.

## Recommended before rolling out broadly

- [ ] **Connect telemetry if you have it.** Stage: *Root Cause Investigation* pulls
      operational evidence in parallel with the code trace. It degrades gracefully
      without it, but the root cause will rest on code reading alone.

- [ ] **Name the approvers.** Stages: *Root Cause Confirmation* and *Publish and
      Close*. Both are gated, but the portable file cannot carry specific people —
      they currently use "let PlayerZero decide". Set named individuals or a role if
      publishing to Confluence should always be signed off by the same person.

- [ ] **Set the Confluence trust preference.** Stage: *Docs Grounding*. The stage
      treats your pages as a hypothesis to confirm against code, on purpose. If some
      spaces are reliably current and others are archive, seed that as a preference
      so runs stop re-deciding.

- [ ] **Confirm the changelog register.** The workflow appends to one running
      changelog page. If you would rather have a page per release, say so — it is a
      one-line change to the draft stage.

## Optional / later

- [ ] **Add a chat notification.** A close-the-loop post to Slack or Teams when the
      runbook publishes.

- [ ] **Hand off instead of closing.** If a confirmed root cause should kick off a
      fix workflow, that is a cross-workflow transition from *Publish and Close* and
      has to be wired in the editor after import — portable exports drop those edges.

- [ ] **Tune the execution mode.** *Root Cause Investigation* runs on Pro, billed
      higher than Standard. Leave it there for real incidents.

## Design choices worth confirming

- **One workflow, two outputs — with a caveat.** The runbook and the changelog entry
  both close out the same incident, so they share one gate here. A repo-wide release
  notes page is a genuinely different unit of work, driven by merges rather than by
  an incident. If you want that, it should be its own workflow on a schedule.

- **Two gates, not four.** Root cause is confirmed once, then the publish stage takes
  one approval covering both pages and any memories. The human-first alternative is a
  separate approval per page; that is two extra stops per incident.

- **Nothing is ever deleted.** The connector allows page deletion. The workflow
  proposes corrections instead and leaves the decision with the reviewer.

## Verify

- [ ] Dry run on a resolved, low-stakes incident. Confirm the publish gate fires
      before any Confluence write, and that rejecting the root cause routes back to
      investigation.
- [ ] Confirm the runbook reads usefully to an on-call engineer who was not involved.

Owner: Teja   ·   Revisit by: after the first three incidents
