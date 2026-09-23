# Post-setup checklist — Change Test Coverage

Generated with the workflow file. These steps cannot be completed by the workflow
definition alone and must be finished after import.

## Required before the first run

- [ ] **Import the file and publish it.** Project Settings → Workflows → import
      `change-test-coverage.workflow.json`, then publish the draft. Stages resolve
      as Entry → Work → Work → Work → Terminal.
      If skipped: the workflow exists only as a draft and no run can start against it.

- [ ] **Confirm the repository is indexed and pick the right branch.** Stage:
      *Change Intake* and *Simulate and Triage*. Simulations default to the
      organization's configured release branch, so the branch carrying the change
      must be selectable.
      If skipped: the run simulates the wrong code and every result is meaningless.

- [ ] **Decide the trigger.** The workflow assumes a code change or pull request as
      its starting input. Wire whichever entry point you want — a PR event, a manual
      run from the branch review page, or an API trigger from CI.
      If skipped: the workflow only ever runs when someone starts it by hand.

## Recommended before rolling out broadly

- [ ] **Name the approver for the scenario gate.** Stage: *Scenario Drafting*. The
      file records that the stage is gated but the portable format never carries
      specific people. It currently uses "let PlayerZero decide" — switch it to named
      individuals or a role if the review should always land with the same person.

- [ ] **Set context-source preferences.** Stage: *Change Intake*. The stage searches
      memory and the knowledge base. Decide up front whether the team trusts those
      sources for this kind of work, or only in specific circumstances, and seed that
      as a preference so runs stop re-asking.

- [ ] **Check the scenario library starting point.** Stage: *Behavior Mapping* looks
      for scenarios that already cover the affected area. If the library is empty,
      the first few runs will over-produce scenarios; that settles once coverage
      accumulates.

- [ ] **Confirm the approver on close-out.** Stage: *Coverage Report* is gated
      because it proposes memories, and memory writes need approval. Leave it gated
      unless you are happy for the agent to persist memories unattended.

## Optional / later

- [ ] **Add ticketing.** Not used today — results stay inside PlayerZero by design.
      If you later want the coverage report posted onto a Jira, Azure DevOps, or
      ServiceNow item, that is one added instruction in *Coverage Report* plus the
      connector, and it should stay gated.

- [ ] **Add a chat notification.** A close-the-loop post to a Slack or Teams channel
      when the coverage report lands.

- [ ] **Tune the execution modes.** *Behavior Mapping* and *Simulate and Triage* run
      on Pro, which is billed higher than Standard. Drop either to Standard if the
      changes you run this on are usually small.

## Design choices worth confirming

Two places where the workflow was shaped around what suits an agent rather than a
traditional human process. Both are easy to flip:

- **Behavior mapping is one parallel stage, not several sequential reviews.** Faster
  and fewer stops; the tradeoff is that the tracing, existing-coverage check, and
  history search all land in one artifact rather than as separate checkpoints.
- **Only one human gate, on the scenarios.** Everything read-only auto-advances. The
  human-first alternative is a gate after behavior mapping too, so someone confirms
  the blast radius before scenarios get written. That costs a stop per run.

## Verify

- [ ] Dry run on a real but low-stakes change; confirm the scenario gate fires and
      the retry edge back from *Simulate and Triage* works when you reject a scenario.
- [ ] Confirm the coverage report reads well to someone who did not write the change.

Owner: Teja   ·   Revisit by: after the first five runs
