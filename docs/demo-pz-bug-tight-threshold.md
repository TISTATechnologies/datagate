# PlayerZero demo bug — Option B (tight drift threshold)

**Branch:** `demo/pz-bug-tight-threshold`  
**File:** `src/DataGate.Domain/DelegationOfAuthorityPolicy.cs`

## Intentional defect

`DriftThresholdPct` default changed from **5.0** to **0.5**.

| Scenario | Spec (correct) | Buggy behavior |
|---|---|---|
| Steward UI **Load clean** (~1.2% drift) | Auto-promote → Gold | Pending — `RowCountDrift` |

## Reproduce

Steward UI → **Load clean** → **Post run**  
(or simulate with `rowCountDriftPct: 1.2`, nulls under 2%, no new cols / PII)

Expect: **Pending**. Correct: **Gold / AutoPromoted**.

## Suggested Jira text (symptom only — do not name the root cause)

**Title:** Steward "Load clean" sample no longer auto-promotes to Gold  

**Description:**  
Using the steward UI Load clean sample (about 1.2% row-count drift, low null rate, no new columns, no sensitive data), the run used to auto-promote to Gold. It now remains in Pending / Awaiting approval and cites row-count drift. Please investigate why an otherwise clean run is blocked.

**Environment:** DataGate demo / branch `demo/pz-bug-tight-threshold`
