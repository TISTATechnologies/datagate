# PlayerZero demo bug — Option A (off-by-one)

**Branch:** `demo/pz-bug-off-by-one`  
**File:** `src/DataGate.Domain/DelegationOfAuthorityPolicy.cs`

## Intentional defect

Drift comparison changed from `>` to `>=`.

| Metric | Spec (correct) | Buggy behavior |
|---|---|---|
| `rowCountDriftPct == 5.0` | Auto-promote (threshold exclusive) | Pending — `RowCountDrift` |

## Reproduce

Steward UI **Post run** (or simulate) with:

- `rowCountDriftPct`: **5.0**
- `nullRatePct`: 0.3 (under 2%)
- no new columns, no sensitive/regulatory flags

Expect: **Pending**. Correct: **Gold / AutoPromoted**.

## Suggested Jira text (symptom only — do not name the root cause)

**Title:** Clean promotion fails at exactly 5% row-count drift  

**Description:**  
When we post a silver→gold run with row-count drift of exactly 5.0% (and otherwise clean metrics: low null rate, no new columns, no sensitive data), the run stays in Pending / Awaiting approval instead of auto-promoting to Gold. Per the published gate policy, the drift threshold is 5% and values at the threshold should still auto-promote. Please investigate.

**Environment:** DataGate demo / branch `demo/pz-bug-off-by-one`
