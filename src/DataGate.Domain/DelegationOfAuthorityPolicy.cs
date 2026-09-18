using DataGate.Promotions;

namespace DataGate.Promotions;

/// <summary>
/// The business rules that decide WHO must approve a data promotion.
///
/// Deliberately a pure function with no infrastructure: it is unit-testable,
/// diffable in a pull request, and readable by a non-engineer. The workflow
/// engine orchestrates; this class decides. Keeping that boundary sharp is the
/// whole architectural argument of the demo.
/// </summary>
public sealed class DelegationOfAuthorityPolicy
{
    // Tunable thresholds. Changing one of these is a PR with a test, not a redeploy of logic.
    public decimal DriftThresholdPct { get; init; } = 5.0m;
    public decimal NullRateThresholdPct { get; init; } = 2.0m;
    public decimal StewardExposureLimit { get; init; } = 250_000m;
    public decimal OwnerExposureLimit { get; init; } = 2_500_000m;
    public decimal DirectorExposureLimit { get; init; } = 25_000_000m;

    public GateDecision Evaluate(RunMetrics run)
    {
        var reasons = new List<GateReason>();

        if (run.RowCountDriftPct > DriftThresholdPct) reasons.Add(GateReason.RowCountDrift);
        if (run.NullRatePct > NullRateThresholdPct) reasons.Add(GateReason.NullRateSpike);
        if (run.NewColumns.Count > 0) reasons.Add(GateReason.NewColumn);
        if (run.ContainsSensitiveData) reasons.Add(GateReason.SensitiveDataDetected);
        if (run.IsRegulatoryReporting) reasons.Add(GateReason.RegulatoryReportingTable);

        if (reasons.Count == 0)
            return GateDecision.AutoPromote(run.RunId);

        // Sensitive data is never auto-promoted and never signed off below Data Owner,
        // regardless of how small the financial exposure is.
        var tier = ApproverTier.DataSteward;
        if (run.FinancialExposure > StewardExposureLimit) tier = ApproverTier.DataOwner;
        if (run.FinancialExposure > OwnerExposureLimit) tier = ApproverTier.Director;
        if (run.FinancialExposure > DirectorExposureLimit) tier = ApproverTier.ChiefDataOfficer;

        if (run.ContainsSensitiveData && tier < ApproverTier.DataOwner)
            tier = ApproverTier.DataOwner;

        if (run.IsRegulatoryReporting)
            reasons.Add(GateReason.HighFinancialExposure);

        // Two-person rule: above the Owner limit, or on any sensitive-data change,
        // one signature is not enough.
        var requiredApprovals =
            run.FinancialExposure > OwnerExposureLimit || run.ContainsSensitiveData ? 2 : 1;

        // Tighter SLA the higher the stakes - this drives the workflow's escalation timer.
        var slaHours = tier switch
        {
            ApproverTier.DataSteward => 24,
            ApproverTier.DataOwner => 12,
            ApproverTier.Director => 8,
            _ => 4
        };

        return GateDecision.RequiresApproval(run.RunId, tier, requiredApprovals, slaHours, reasons);
    }
}
