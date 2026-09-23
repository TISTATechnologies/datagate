# Post-setup checklist — Jira Engineering Scoping

Generated with the workflow file. These steps cannot be completed by the workflow
definition alone and must be finished after import.

## Required before the first run

- [ ] **Import the file and publish it.** Project Settings → Workflows → import
      `jira-engineering-scoping.workflow.json`, then publish the draft.
      If skipped: the workflow exists only as a draft and no run can start.

- [ ] **Decide how child issues are created.** Stage: *Publish to Ticket* can break
      the plan into child issues. Confirm the issue type to use for them and whether
      you want them at all — some teams want the plan as a comment only.
      If skipped: the run will ask every time, or create issues of a type your board
      does not expect.

- [ ] **Decide the trigger.** The natural one is a Jira webhook on tickets entering a
      "needs scoping" or "refinement" state. Manual start works fine while you are
      evaluating it.
      If skipped: the workflow only runs when someone starts it by hand.

- [ ] **Turn on ticket attachment import** if your feature tickets carry mockups or
      specs as attachments. Integration settings → your Jira project → Ticket
      Attachment Settings → **Allow Attachments**. Off by default, Project Owner only.
      Stage: *Request Intake*.

## Recommended before rolling out broadly

- [ ] **Name the approver for the approach decision.** Stage: *Approach Agreement* is
      where the technical direction gets chosen, and everything downstream inherits
      it. This is the one gate worth giving a named person — usually a tech lead or
      architect rather than whoever is nearest. The portable file cannot carry
      specific people; it currently uses "let PlayerZero decide".

- [ ] **Point it at your standards.** If you keep architecture notes, ADRs, or coding
      conventions in Confluence or a knowledge base, the deep-dive will find and use
      them. Confirm they exist and are current, or the plan will infer conventions
      from the code alone.

- [ ] **State your estimation convention.** The plan gives effort as ranges with
      assumptions. If your team uses story points, t-shirt sizes, or ideal days, say
      so once as a preference and it will stop guessing.

- [ ] **Confirm what a scoped ticket should transition to.** The workflow can move the
      ticket after scoping, but only if you tell it your states.

## Optional / later

- [ ] **Chain it to the coverage workflow.** The plan names what needs test coverage.
      Running *Change Test Coverage* on the resulting branch closes that loop. It
      would be a cross-workflow transition, which must be wired in the editor after
      import — portable exports drop those edges.

- [ ] **Publish significant decisions to Confluence.** Where scoping produces a
      durable architectural decision, it could also write an ADR page. That is an
      addition to the final stage plus the same gate.

## Design choices worth confirming

- **The deep-dive runs before the human conversation, not after.** The agent answers
  what the code can answer, so the human only decides what genuinely needs judgement.
  The human-first alternative is a refinement meeting first and investigation after,
  which means the questions asked are less informed.

- **Two gates: the approach, then the write.** Approach agreement is separate because
  the plan and the estimate both inherit that decision — catching a wrong approach
  after the plan is written wastes the plan. The write gate is separate because it
  touches a shared ticket.

- **It will not close the ticket.** Scoping decides how work will be done, not that it
  is done.

- **Rejecting an approach loops back to the deep-dive.** If the reviewer wants an
  option that was not assessed, the agent evaluates it properly rather than planning
  around a guess. That costs a second Pro-tier run, deliberately.

## Verify

- [ ] Dry run on a ticket you have already scoped by hand, so you can compare the
      recommended approach against what your team actually decided.
- [ ] At the approach gate, push back and ask for an option it did not propose;
      confirm it loops back to the deep-dive rather than planning around it.

Owner: Teja   ·   Revisit by: after the first five tickets
