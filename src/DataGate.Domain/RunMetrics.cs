namespace DataGate.Promotions;

/// <summary>What the upstream pipeline reports about a completed run.</summary>
public sealed record RunMetrics(
    string RunId,
    string Table,
    long RowCount,
    decimal RowCountDriftPct,
    decimal NullRatePct,
    IReadOnlyList<string> NewColumns,
    decimal FinancialExposure,      // dollars of downstream reporting affected
    bool ContainsSensitiveData,
    bool IsRegulatoryReporting);

/// <summary>The policy's verdict - the thing the workflow acts on.</summary>
public sealed record GateDecision(
    string RunId,
    bool RequiresHuman,
    ApproverTier Tier,
    int RequiredApprovals,
    int SlaHours,
    IReadOnlyList<GateReason> Reasons)
{
    public static GateDecision AutoPromote(string runId) =>
        new(runId, false, ApproverTier.None, 0, 0, new[] { GateReason.Clean });

    public static GateDecision RequiresApproval(
        string runId, ApproverTier tier, int approvals, int slaHours, IReadOnlyList<GateReason> reasons) =>
        new(runId, true, tier, approvals, slaHours, reasons);

    /// <summary>Plain-English line for the approval notification and the audit log.</summary>
    public string Explain() => RequiresHuman
        ? $"Run {RunId} needs {RequiredApprovals} approval(s) at {Tier} within {SlaHours}h. " +
          $"Triggered by: {string.Join(", ", Reasons)}."
        : $"Run {RunId} passed all gates and was promoted automatically.";
}
