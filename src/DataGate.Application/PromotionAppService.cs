using DataGate.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DataGate.Promotions;

/// <summary>
/// ponytail: demo host skips ABP Identity — Approve carries ApproverId/Tier from the UI.
/// Wire CurrentUser + DataGatePermissions when the full identity stack lands.
/// </summary>
[AllowAnonymous]
public class PromotionAppService : ApplicationService, IPromotionAppService
{
    private readonly IRepository<PromotionRequest, Guid> _repository;
    private readonly DelegationOfAuthorityPolicy _policy;
    private readonly IElsaWorkflowSignaler _elsa;

    public PromotionAppService(
        IRepository<PromotionRequest, Guid> repository,
        DelegationOfAuthorityPolicy policy,
        IElsaWorkflowSignaler elsa)
    {
        _repository = repository;
        _policy = policy;
        _elsa = elsa;
    }

    public async Task<PromotionRequestDto> EvaluateAsync(RunMetricsDto input)
    {
        var run = new RunMetrics(
            input.RunId, input.Table, input.RowCount, input.RowCountDriftPct,
            input.NullRatePct, input.NewColumns, input.FinancialExposure,
            input.ContainsSensitiveData, input.IsRegulatoryReporting);

        var decision = _policy.Evaluate(run);
        var request = new PromotionRequest(GuidGenerator.Create(), decision, run, Clock.Now.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(input.WorkflowInstanceId))
            request.AttachWorkflowInstance(input.WorkflowInstanceId);

        await _repository.InsertAsync(request, autoSave: true);
        Logger.LogInformation("Gate decision for {RunId}: {Summary}", run.RunId, decision.Explain());

        return Map(request);
    }

    public async Task<List<PromotionRequestDto>> GetPendingListAsync()
    {
        var list = await _repository.GetListAsync(x => x.Status == PromotionStatus.AwaitingApproval);
        return list.OrderBy(x => x.SlaDueUtc).Select(Map).ToList();
    }

    public async Task<List<PromotionRequestDto>> GetGoldListAsync()
    {
        var list = await _repository.GetListAsync(x =>
            x.Status == PromotionStatus.Approved || x.Status == PromotionStatus.AutoPromoted);
        return list
            .OrderByDescending(x => x.LastModificationTime ?? x.CreationTime)
            .Select(Map)
            .ToList();
    }

    public async Task<SimulateRunResultDto> SimulateAsync(RunMetricsDto input)
    {
        if (string.IsNullOrWhiteSpace(input.RunId))
            input.RunId = $"run-ui-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

        try
        {
            var elsa = await _elsa.TriggerQualityGateAsync(input);
            // stock Elsa image sometimes returns empty 200 from in-network calls; treat as miss
            if (elsa.StatusCode is >= 200 and < 300 && !string.IsNullOrWhiteSpace(elsa.Body))
            {
                return new SimulateRunResultDto
                {
                    ViaElsa = true,
                    StatusCode = elsa.StatusCode,
                    Body = elsa.Body
                };
            }

            Logger.LogWarning("Elsa quality-gate returned {Status} with empty/unusable body; evaluating locally",
                elsa.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Elsa webhook unavailable; evaluating locally");
        }

        var request = await EvaluateAsync(input);
        var status = request.RequiresHuman ? "awaiting-steward" : "auto-promoted";
        return new SimulateRunResultDto
        {
            ViaElsa = false,
            StatusCode = request.RequiresHuman ? 202 : 200,
            Body = $"{{ \"status\": \"{status}\", \"requestId\": \"{request.Id}\", \"runId\": \"{request.RunId}\", \"reason\": {System.Text.Json.JsonSerializer.Serialize(request.ReasonSummary)} }}",
            Request = request
        };
    }

    public async Task<PromotionRequestDto> ApproveAsync(Guid id, ApprovalInputDto input)
    {
        var request = await _repository.GetAsync(id);

        if (!string.IsNullOrWhiteSpace(input.WorkflowInstanceId))
            request.AttachWorkflowInstance(input.WorkflowInstanceId);

        request.Approve(input.ApproverId, input.ApproverTier, input.Comment, Clock.Now.ToUniversalTime());
        await _repository.UpdateAsync(request, autoSave: true);

        if (request.Status == PromotionStatus.Approved && !string.IsNullOrWhiteSpace(request.WorkflowInstanceId))
            await _elsa.SignalStewardDecisionAsync(request.WorkflowInstanceId, input.ApproverId, approved: true, input.Comment);

        return Map(request);
    }

    public async Task<PromotionRequestDto> RejectAsync(Guid id, RejectInputDto input)
    {
        var request = await _repository.GetAsync(id);
        if (!string.IsNullOrWhiteSpace(input.WorkflowInstanceId))
            request.AttachWorkflowInstance(input.WorkflowInstanceId);

        request.Reject(input.ApproverId, input.Reason, Clock.Now.ToUniversalTime());
        await _repository.UpdateAsync(request, autoSave: true);

        if (!string.IsNullOrWhiteSpace(request.WorkflowInstanceId))
            await _elsa.SignalStewardDecisionAsync(request.WorkflowInstanceId, input.ApproverId, approved: false, input.Reason);

        return Map(request);
    }

    public async Task<PromotionRequestDto> QuarantineAsync(Guid id)
    {
        var request = await _repository.GetAsync(id);
        request.Quarantine();
        await _repository.UpdateAsync(request, autoSave: true);
        return Map(request);
    }

    public async Task<PromotionRequestDto> GetAsync(Guid id) => Map(await _repository.GetAsync(id));

    private static PromotionRequestDto Map(PromotionRequest r) => new()
    {
        Id = r.Id,
        RunId = r.RunId,
        Table = r.Table,
        RowCount = r.RowCount,
        RowCountDriftPct = r.RowCountDriftPct,
        NullRatePct = r.NullRatePct,
        NewColumns = r.NewColumns,
        FinancialExposure = r.FinancialExposure,
        ContainsSensitiveData = r.ContainsSensitiveData,
        IsRegulatoryReporting = r.IsRegulatoryReporting,
        RequiresHuman = r.RequiredTier != ApproverTier.None,
        RequiredTier = r.RequiredTier,
        RequiredApprovals = r.RequiredApprovals,
        ApprovalsReceived = r.Approvals.Count,
        SlaDueUtc = r.SlaDueUtc,
        Status = r.Status,
        ReasonSummary = r.ReasonSummary,
        WorkflowInstanceId = r.WorkflowInstanceId,
        Signatures = r.Approvals.Select(a => new ApprovalRecordDto
        {
            ApproverId = a.ApproverId,
            Tier = a.Tier,
            Comment = a.Comment,
            AtUtc = a.AtUtc
        }).ToList()
    };
}
