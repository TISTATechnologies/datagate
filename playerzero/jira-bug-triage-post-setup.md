# Post-setup checklist — Jira Bug Triage

Generated with the workflow file. These steps cannot be completed by the workflow
definition alone and must be finished after import.

## Required before the first run

- [ ] **Import the file and publish it.** Project Settings → Workflows → import
      `jira-bug-triage.workflow.json`, then publish the draft.
      If skipped: the workflow exists only as a draft and no run can start.

- [ ] **Turn on ticket attachment import.** Integration settings → your Jira project
      → Ticket Attachment Settings → **Allow Attachments**. Off by default, and only
      a Project Owner can change it. Stage: *Ticket Intake*.
      If skipped: the agent reads the ticket text but not the screenshots and logs
      attached to it — which is usually where the real evidence is.

- [ ] **Decide the trigger.** The natural one is a Jira webhook on new bugs in a
      specific project, so triage starts the moment a ticket is filed. Manual start
      and API trigger also work.
      If skipped: the workflow only runs when someone starts it by hand.

- [ ] **Confirm the Jira project scope.** The duplicate check searches Jira broadly.
      Confirm which projects the integration can see, so it is not searching a space
      the reporter's bug could never appear in. Stage: *Duplicate and History Check*.

## Recommended before rolling out broadly

- [ ] **Name the approver.** Stage: *Triage and Update* is the only gate, and it
      covers every write to the ticket. The portable file cannot carry specific
      people — it currently uses "let PlayerZero decide". Set named individuals or a
      role if triage sign-off should always land with the same person.

- [ ] **State your triage conventions.** The workflow proposes priority, component,
      labels, assignee, and a state transition. It will infer your conventions from
      what it sees on existing tickets, which works but takes a few runs. Seeding
      them as a preference — your priority ladder, which components exist, whether
      triage transitions the ticket — gets it right immediately.

- [ ] **Connect telemetry if you have it.** Stage: *Root Cause Investigation* pulls
      operational evidence in parallel with the code trace. It degrades gracefully
      without it, but severity judgements rest on code reading alone.

- [ ] **Decide the private-comment question.** If these tickets are customer-facing
      through Jira Service Management, triage findings probably belong in an internal
      comment rather than a public one. Say so and it becomes one line in the final
      stage.

## Optional / later

- [ ] **Sub-tasks for the fix.** The connector can create sub-tasks under a parent.
      If you want triage to break the fix into child issues, that is an addition to
      *Triage and Update* — and it should stay behind the same gate.

- [ ] **Hand off to a fix workflow.** A confirmed root cause could kick off an
      implementation workflow. That is a cross-workflow transition and must be wired
      in the editor after import — portable exports drop those edges.

- [ ] **Pair it with the coverage workflow.** Once a fix exists for a triaged bug,
      *Change Test Coverage* is the natural next run on that branch.

## Design choices worth confirming

- **The duplicate check runs before the investigation, and can skip it.** Cheap
  check first, expensive investigation only when the bug is genuinely new. The
  human-first alternative is always investigating and letting a person spot the
  duplicate, which costs a Pro-tier run on every repeat report.

- **One gate, covering every Jira write.** Comment, fields, links, and transition are
  approved together. The alternative is approving each in turn, which is three or
  four extra stops per ticket.

- **It will not close a ticket it did not confirm as a duplicate or already fixed.**
  Triage decides what a bug is, not whether it is done. Deliberate — an agent closing
  live bugs is the failure mode worth designing out.

## Verify

- [ ] Dry run on a real but already-triaged ticket, so you can compare its verdict to
      what a human decided. Confirm the gate fires before anything is written.
- [ ] Run it on a known duplicate and confirm it takes the short path and proposes a
      link rather than a fresh investigation.

Owner: Teja   ·   Revisit by: after the first ten tickets
