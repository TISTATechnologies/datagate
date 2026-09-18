using System.Net.Http.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace DataGate.Workflow.Activities;

/// <summary>
/// Calls the ABP application service to evaluate a finished pipeline run.
///
/// Note what this activity does NOT do: it holds no business rules. The tier,
/// the SLA and the two-person rule all live in DelegationOfAuthorityPolicy, which
/// is unit-tested. The workflow only orchestrates. That separation is what keeps
/// the approval logic reviewable and the workflow diagram readable.
/// </summary>
[Activity("DataGate", "Governance", "Evaluate a run against the Delegation of Authority matrix")]
public class EvaluateGateActivity : CodeActivity
{
    [Input(Description = "Run metrics reported by the upstream pipeline.")]
    public Input<RunMetricsPayload> Run { get; set; } = default!;

    [Output(Description = "The gate decision: who must approve, by when, and why.")]
    public Output<GateDecisionPayload> Decision { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var run = context.Get(Run)!;
        var factory = context.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("DataGateApi");

        var response = await client.PostAsJsonAsync(
            "/api/app/promotion/evaluate", run, context.CancellationToken);
        response.EnsureSuccessStatusCode();

        var decision = await response.Content
            .ReadFromJsonAsync<GateDecisionPayload>(context.CancellationToken)
            ?? throw new InvalidOperationException("Empty gate decision.");

        context.Set(Decision, decision);

        // Correlating on the business key means an approval posted days later
        // finds this exact instance, even after a redeploy.
        context.WorkflowExecutionContext.CorrelationId = decision.Id.ToString();
    }
}

public sealed record RunMetricsPayload(
    string RunId, string Table, long RowCount, decimal RowCountDriftPct,
    decimal NullRatePct, string[] NewColumns, decimal FinancialExposure,
    bool ContainsSensitiveData, bool IsRegulatoryReporting);

public sealed record GateDecisionPayload(
    Guid Id, string RunId, bool RequiresHuman, string RequiredTier,
    int RequiredApprovals, int ApprovalsReceived, DateTime SlaDueUtc,
    string Status, string ReasonSummary);
