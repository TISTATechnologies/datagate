using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace DataGate.Promotions;

/// <summary>
/// The auditable record of one promotion decision. ABP's FullAuditedAggregateRoot
/// gives creator, modifier, timestamps and soft delete for free - which is most of
/// what an auditor asks for before you write a line of code.
/// </summary>
public class PromotionRequest : FullAuditedAggregateRoot<Guid>
{
    public string RunId { get; private set; } = default!;
    public string Table { get; private set; } = default!;
    public long RowCount { get; private set; }
    public decimal RowCountDriftPct { get; private set; }
    public decimal NullRatePct { get; private set; }
    /// <summary>Comma-separated new column names from the run (evidence for stewards).</summary>
    public string NewColumns { get; private set; } = string.Empty;
    public decimal FinancialExposure { get; private set; }
    public bool ContainsSensitiveData { get; private set; }
    public bool IsRegulatoryReporting { get; private set; }
    public ApproverTier RequiredTier { get; private set; }
    public int RequiredApprovals { get; private set; }
    public DateTime SlaDueUtc { get; private set; }
    public PromotionStatus Status { get; private set; }
    public string ReasonSummary { get; private set; } = default!;
    /// <summary>Elsa instance waiting on StewardDecision; set from the steward UI / approve call.</summary>
    public string? WorkflowInstanceId { get; private set; }

    private readonly List<ApprovalRecord> _approvals = new();
    public IReadOnlyCollection<ApprovalRecord> Approvals => _approvals.AsReadOnly();

    private PromotionRequest() { }

    public PromotionRequest(Guid id, GateDecision decision, RunMetrics run, DateTime nowUtc) : base(id)
    {
        RunId = run.RunId;
        Table = run.Table;
        RowCount = run.RowCount;
        RowCountDriftPct = run.RowCountDriftPct;
        NullRatePct = run.NullRatePct;
        NewColumns = string.Join(", ", run.NewColumns);
        FinancialExposure = run.FinancialExposure;
        ContainsSensitiveData = run.ContainsSensitiveData;
        IsRegulatoryReporting = run.IsRegulatoryReporting;
        RequiredTier = decision.Tier;
        RequiredApprovals = decision.RequiredApprovals;
        SlaDueUtc = nowUtc.AddHours(decision.SlaHours);
        ReasonSummary = decision.Explain();
        Status = decision.RequiresHuman ? PromotionStatus.AwaitingApproval : PromotionStatus.AutoPromoted;
    }

    public void AttachWorkflowInstance(string workflowInstanceId)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            throw new BusinessException("DataGate:MissingWorkflowInstance");
        WorkflowInstanceId = workflowInstanceId.Trim();
    }

    public void Approve(Guid approverId, ApproverTier approverTier, string comment, DateTime nowUtc)
    {
        if (Status != PromotionStatus.AwaitingApproval)
            throw new BusinessException("DataGate:NotAwaitingApproval");

        if (approverTier < RequiredTier)
            throw new BusinessException("DataGate:InsufficientAuthority")
                .WithData("required", RequiredTier).WithData("actual", approverTier);

        // Two-person rule: the same human cannot satisfy both signatures.
        if (_approvals.Any(a => a.ApproverId == approverId))
            throw new BusinessException("DataGate:DuplicateApprover");

        _approvals.Add(new ApprovalRecord(approverId, approverTier, comment, nowUtc));

        if (_approvals.Count >= RequiredApprovals)
            Status = PromotionStatus.Approved;
    }

    public void Reject(Guid approverId, string reason, DateTime nowUtc)
    {
        _approvals.Add(new ApprovalRecord(approverId, RequiredTier, $"REJECTED: {reason}", nowUtc));
        Status = PromotionStatus.Rejected;
    }

    /// <summary>Called when the workflow's escalation timer fires with no decision.</summary>
    public void Quarantine()
    {
        if (Status == PromotionStatus.AwaitingApproval)
            Status = PromotionStatus.Quarantined;
    }

    public bool IsComplete => Status is PromotionStatus.Approved
        or PromotionStatus.Rejected or PromotionStatus.AutoPromoted or PromotionStatus.Quarantined;
}

public sealed record ApprovalRecord(Guid ApproverId, ApproverTier Tier, string Comment, DateTime AtUtc);
