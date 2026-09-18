using DataGate.Promotions;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace DataGate.Tests;

public class PromotionRequestTests
{
    private static readonly DateTime Now = new(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

    private static PromotionRequest Sensitive2PersonRequest()
    {
        var run = new RunMetrics("run-2", "silver.claims", 500_000, 61.7m, 14.3m,
            new[] { "member_ssn" }, 3_000_000m, true, true);
        var decision = new DelegationOfAuthorityPolicy().Evaluate(run);
        return new PromotionRequest(Guid.NewGuid(), decision, run, Now);
    }

    [Fact]
    public void One_signature_does_not_satisfy_the_two_person_rule()
    {
        var request = Sensitive2PersonRequest();
        request.Approve(Guid.NewGuid(), ApproverTier.Director, "looks fine", Now);

        request.Status.ShouldBe(PromotionStatus.AwaitingApproval);
    }

    [Fact]
    public void The_same_person_cannot_sign_twice()
    {
        var request = Sensitive2PersonRequest();
        var approver = Guid.NewGuid();
        request.Approve(approver, ApproverTier.Director, "first", Now);

        Should.Throw<BusinessException>(() =>
            request.Approve(approver, ApproverTier.Director, "second", Now));
    }

    [Fact]
    public void An_approver_below_the_required_tier_is_refused()
    {
        var request = Sensitive2PersonRequest();

        Should.Throw<BusinessException>(() =>
            request.Approve(Guid.NewGuid(), ApproverTier.DataSteward, "rubber stamp", Now));
    }

    [Fact]
    public void Two_distinct_qualified_approvers_complete_the_request()
    {
        var request = Sensitive2PersonRequest();
        request.Approve(Guid.NewGuid(), ApproverTier.Director, "checked lineage", Now);
        request.Approve(Guid.NewGuid(), ApproverTier.ChiefDataOfficer, "privacy reviewed", Now);

        request.Status.ShouldBe(PromotionStatus.Approved);
        request.Approvals.Count.ShouldBe(2);
    }

    [Fact]
    public void An_expired_sla_quarantines_rather_than_promoting()
    {
        var request = Sensitive2PersonRequest();
        request.Quarantine();

        request.Status.ShouldBe(PromotionStatus.Quarantined);
        request.IsComplete.ShouldBeTrue();
    }
}
