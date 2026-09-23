# Test plan — Incident RCA to Docs

How to prove the workflow works before you point it at a real incident. Read this
after importing `incident-rca-to-docs.workflow.json`.

The principle: **test the wiring before you test the intelligence.** Most of what
can go wrong is configuration — a missing scope, a gate that auto-advances, a
publish target nobody set — not the agent's reasoning.

---

## Before the first run: make the blast radius zero

The publish target is the **PlayerZero** page in the **TD** space. It was empty when
checked, with no child pages, so it is already a safe place to rehearse — a bad run
creates junk children you can delete, and touches nothing that exists.

1. **Confirm write scope** is actually granted, not just read. This is the single
   most common failure: the run does everything correctly and dies at the last step.
2. **Leave the page empty.** The first run creates the runbook and changelog children
   itself; that page-creation path is part of what you are testing.
3. **Check the children after each test** and delete the ones you do not want to keep,
   so the next test starts from a known state.

---

## Test 1 — Wiring (the one that matters)

Goal: every stage runs, every gate fires, every artifact appears.

Use a **resolved incident whose cause you already know**, so you can grade the
answer. Suggested seed, checkable in the DataGate code:

> A promotion request for a $3M table with a new PII column shows **Approved** in
> the steward console with two valid signatures, but the run never appears in gold
> and the Elsa workflow instance stays suspended indefinitely.

The cause is traceable in the approve path: the workflow instance is only signalled
when the promotion record actually carries a workflow instance id, and the local
evaluate fallback never attaches one. Good seed because the answer is in code, not
in telemetry you may not have.

**What to check at each stage:**

| Stage | Pass looks like |
|---|---|
| Incident Intake | Brief names symptom, impact, timeline. Your theory is recorded as a theory, not adopted as fact. |
| Docs Grounding | Cites actual Confluence pages by link. Says clearly if it found nothing. |
| Root Cause Investigation | Names a mechanism with an evidence chain, not a vague area. States confidence. |
| Root Cause Confirmation | **Stops and waits.** If it advances without you, the gate is misconfigured. |
| Documentation Draft | Two drafts exist. Nothing is in Confluence yet. |
| Publish and Close | Shows exact page content, waits, then publishes and links both pages. |

---

## Test 2 — The reject path

At **Root Cause Confirmation**, reject the cause and point at a different lead.

Pass: it routes back to investigation and pursues your lead. Fail: it proceeds to
drafting anyway, or dead-ends.

This is the loop edge, and it is the thing most likely to be wrong in a workflow
graph. Worth testing on its own.

---

## Test 3 — Graceful degradation

Run it on an incident in an area your Confluence has **no** documentation for.

Pass: Docs Grounding says so plainly, the run continues on code evidence, and
confidence is lower. Fail: it invents grounding, or stalls.

---

## Test 4 — The publish gate holds

At **Publish and Close**, reply with something ambiguous — "looks fine I guess" —
rather than an explicit approval.

Pass: it asks for explicit approval before writing. Fail: it publishes. That one
matters, because a Confluence write is visible to the whole team.

---

## Test 5 — No fix shipped

Run it on an incident that is root-caused but **not yet fixed**.

Pass: it publishes the runbook and explicitly skips the changelog entry, saying why.
A changelog entry for a fix that does not exist is worse than no entry.

---

## When you are done

- Move the publish target from the sandbox space to the real one.
- Set the named approvers on both gated stages.
- Delete the sandbox pages, or keep the space as the permanent rehearsal target.

If Test 1 and Test 2 pass, the workflow is sound. Tests 3 to 5 are about how it
behaves on a bad day, which is when you will actually be using it.
