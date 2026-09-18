using DataGate.Promotions;
using Shouldly;
using Xunit;

namespace DataGate.Tests;

/// <summary>
/// These tests are the business spec. Read the [InlineData] rows aloud in a
/// governance review and a non-engineer can confirm or correct them - which is
/// the point of keeping the policy free of infrastructure.
/// </summary>
public class DelegationOfAuthorityPolicyTests
{
    private readonly DelegationOfAuthorityPolicy _policy = new();

    private static RunMetrics Run(
        decimal drift = 0m, decimal nulls = 0m, string[]? newCols = null,
        decimal exposure = 0m, bool sensitive = false, bool regulatory = false) =>
        new("run-1", "silver.claims", 1_000_000, drift, nulls,
            newCols ?? Array.Empty<string>(), exposure, sensitive, regulatory);

    [Fact]
    public void Clean_run_is_promoted_with_no_human_involved()
    {
        var decision = _policy.Evaluate(Run(drift: 1.2m, nulls: 0.3m, exposure: 40_000m));

        decision.RequiresHuman.ShouldBeFalse();
        decision.Tier.ShouldBe(ApproverTier.None);
        decision.Reasons.ShouldContain(GateReason.Clean);
    }

    [Theory]
    [InlineData(100_000, ApproverTier.DataSteward)]
    [InlineData(900_000, ApproverTier.DataOwner)]
    [InlineData(9_000_000, ApproverTier.Director)]
    [InlineData(90_000_000, ApproverTier.ChiefDataOfficer)]
    public void Approval_tier_escalates_with_financial_exposure(decimal exposure, ApproverTier expected)
    {
        var decision = _policy.Evaluate(Run(drift: 40m, exposure: exposure));
        decision.Tier.ShouldBe(expected);
    }

    [Fact]
    public void Sensitive_data_is_never_signed_off_below_data_owner()
    {
        // Tiny exposure would normally land at Steward.
        var decision = _policy.Evaluate(Run(newCols: new[] { "member_ssn" }, exposure: 1_000m, sensitive: true));

        decision.Tier.ShouldBe(ApproverTier.DataOwner);
        decision.Reasons.ShouldContain(GateReason.SensitiveDataDetected);
    }

    [Fact]
    public void Sensitive_changes_require_two_approvers()
    {
        var decision = _policy.Evaluate(Run(drift: 10m, exposure: 5_000m, sensitive: true));
        decision.RequiredApprovals.ShouldBe(2);
    }

    [Fact]
    public void Large_exposure_requires_two_approvers_and_a_tighter_sla()
    {
        var decision = _policy.Evaluate(Run(drift: 10m, exposure: 9_000_000m));

        decision.RequiredApprovals.ShouldBe(2);
        decision.SlaHours.ShouldBe(8);
    }

    [Fact]
    public void Decision_explains_itself_in_plain_english()
    {
        var decision = _policy.Evaluate(Run(drift: 61.7m, nulls: 14.3m,
            newCols: new[] { "member_ssn" }, exposure: 3_000_000m, sensitive: true));

        decision.Explain().ShouldContain("2 approval(s)");
        decision.Explain().ShouldContain("Director");
        decision.Explain().ShouldContain("RowCountDrift");
    }
}
