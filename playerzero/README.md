# PlayerZero workflows

Importable PlayerZero workflow definitions for this project, plus the setup notes
that the workflow files cannot carry themselves.

Each workflow is a `.workflow.json` file. Import it in PlayerZero under
**Project Settings → Workflows**, then publish the draft. The extension matters —
the importer uses it to recognize the file.

Every workflow ships with a post-setup checklist. A validated file is not a finished
workflow: approvers, integration scopes, triggers, and cross-workflow links are all
configured after import.

## What is here

| Workflow | Starts from | Produces | Writes to |
|---|---|---|---|
| `change-test-coverage` | A branch or pull request | Reviewed, simulated test scenarios covering the change | PlayerZero only |
| `incident-rca-to-docs` | An incident report | Root cause, then a runbook page and a changelog entry | Confluence |
| `jira-bug-triage` | A Jira bug | Root cause, priority, links, and a triage comment | Jira |
| `jira-engineering-scoping` | A Jira feature or change request | An agreed approach and an implementation plan | Jira |

`incident-rca-to-docs-test-plan.md` is a worked test procedure for that workflow.
The same shape adapts to the others.

## Shared conventions

All four follow the same design rules, so they behave predictably together:

- **Read-only work auto-advances.** Investigation, search, and analysis run without
  stopping for anyone.
- **Every external write is gated.** Nothing reaches Jira or Confluence without
  explicit approval of the exact content. Implied consent is never enough.
- **Every stage leaves a durable artifact** documenting what it did, with citations,
  so progress is followable without reading the transcript.
- **Code beats documentation.** Authored context is treated as a hypothesis to
  confirm against the code, and disagreements are reported rather than averaged.
- **External systems are optional.** A missing integration lowers confidence and gets
  reported; it never gets silently skipped or invented around.
- **Nothing destructive.** No workflow deletes a page, closes a live bug, or edits
  someone else's comment.

## Prerequisites

| Workflow | Needs |
|---|---|
| `change-test-coverage` | Repository indexed; ability to select the branch under review |
| `incident-rca-to-docs` | Confluence connector with page **write** scope |
| `jira-bug-triage` | Jira connector; ticket attachment import enabled per project |
| `jira-engineering-scoping` | Jira connector; sub-task issue type agreed if child issues are wanted |

Telemetry or observability access improves the two investigation workflows but is
not required — both degrade gracefully without it.

## How they compose

They are deliberately separate units of work, and chain naturally:

```
Jira ticket ──> jira-bug-triage ─────────> fix branch ──> change-test-coverage
           └──> jira-engineering-scoping ─┘

incident ─────> incident-rca-to-docs ────> runbook + changelog in Confluence
```

Chaining them automatically requires cross-workflow transitions, which are wired in
the workflow editor after import — the portable JSON format drops those edges.
