using Volo.Abp.Application.Services;

namespace DataGate.Promotions;

public interface IPromotionAppService : IApplicationService
{
    /// <summary>Called by the workflow: evaluate a finished run and open a request if needed.</summary>
    Task<PromotionRequestDto> EvaluateAsync(RunMetricsDto input);

    /// <summary>Pending requests for the steward UI.</summary>
    Task<List<PromotionRequestDto>> GetPendingListAsync();

    /// <summary>Auto-promoted + human-approved runs (demo “gold” ledger — not lakehouse rows).</summary>
    Task<List<PromotionRequestDto>> GetGoldListAsync();

    /// <summary>Steward UI: post metrics through Elsa (falls back to evaluate if Elsa is down).</summary>
    Task<SimulateRunResultDto> SimulateAsync(RunMetricsDto input);

    /// <summary>Called by the steward UI; resumes Elsa when enough approvals land.</summary>
    Task<PromotionRequestDto> ApproveAsync(Guid id, ApprovalInputDto input);

    Task<PromotionRequestDto> RejectAsync(Guid id, RejectInputDto input);
    Task<PromotionRequestDto> QuarantineAsync(Guid id);
    Task<PromotionRequestDto> GetAsync(Guid id);
}

public sealed class RunMetricsDto
{
    public string RunId { get; set; } = default!;
    public string Table { get; set; } = default!;
    public long RowCount { get; set; }
    public decimal RowCountDriftPct { get; set; }
    public decimal NullRatePct { get; set; }
    public List<string> NewColumns { get; set; } = new();
    public decimal FinancialExposure { get; set; }
    public bool ContainsSensitiveData { get; set; }
    public bool IsRegulatoryReporting { get; set; }
    /// <summary>Optional. Elsa sends this on evaluate so the steward UI never needs a paste.</summary>
    public string? WorkflowInstanceId { get; set; }
}

public sealed class ApprovalInputDto
{
    public Guid ApproverId { get; set; }
    public ApproverTier ApproverTier { get; set; } = ApproverTier.DataSteward;
    public string? WorkflowInstanceId { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class RejectInputDto
{
    public Guid ApproverId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class PromotionRequestDto
{
    public Guid Id { get; set; }
    public string RunId { get; set; } = default!;
    public string Table { get; set; } = default!;
    public long RowCount { get; set; }
    public decimal RowCountDriftPct { get; set; }
    public decimal NullRatePct { get; set; }
    public string NewColumns { get; set; } = string.Empty;
    public decimal FinancialExposure { get; set; }
    public bool ContainsSensitiveData { get; set; }
    public bool IsRegulatoryReporting { get; set; }
    public bool RequiresHuman { get; set; }
    public ApproverTier RequiredTier { get; set; }
    public int RequiredApprovals { get; set; }
    public int ApprovalsReceived { get; set; }
    public DateTime SlaDueUtc { get; set; }
    public PromotionStatus Status { get; set; }
    public string ReasonSummary { get; set; } = default!;
    public string? WorkflowInstanceId { get; set; }
    public List<ApprovalRecordDto> Signatures { get; set; } = new();
}

public sealed class ApprovalRecordDto
{
    public Guid ApproverId { get; set; }
    public ApproverTier Tier { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; }
}

public sealed class SimulateRunResultDto
{
    public bool ViaElsa { get; set; }
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
    public PromotionRequestDto? Request { get; set; }
}
