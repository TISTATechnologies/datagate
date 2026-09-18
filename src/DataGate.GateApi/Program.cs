using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DataGate.Promotions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DelegationOfAuthorityPolicy>();
builder.Services.AddSingleton<PendingApprovals>();
builder.Services.AddHttpClient<ElsaClient>();

var app = builder.Build();

// ponytail: thin host until ABP HttpApi.Host lands; same paths the app service / activities expect
app.MapPost("/api/app/promotion/evaluate", (EvaluateRequest body, DelegationOfAuthorityPolicy policy, PendingApprovals pending) =>
{
    var run = new RunMetrics(
        body.RunId,
        body.Table ?? "",
        body.RowCount,
        body.RowCountDriftPct,
        body.NullRatePct,
        body.NewColumns ?? Array.Empty<string>(),
        body.FinancialExposure,
        body.ContainsSensitiveData,
        body.IsRegulatoryReporting);

    var decision = policy.Evaluate(run);
    var now = DateTime.UtcNow;
    var id = Guid.NewGuid();

    if (decision.RequiresHuman)
        pending.Open(body.RunId, id, decision.RequiredApprovals, decision.Tier, decision.Explain());

    return Results.Ok(new
    {
        id,
        runId = decision.RunId,
        requiresHuman = decision.RequiresHuman,
        requiredTier = decision.Tier.ToString(),
        requiredApprovals = decision.RequiredApprovals,
        approvalsReceived = 0,
        slaDueUtc = decision.RequiresHuman ? now.AddHours(decision.SlaHours) : now,
        status = decision.RequiresHuman ? "AwaitingApproval" : "AutoPromoted",
        reasonSummary = decision.Explain()
    });
});

app.MapPost("/api/app/promotion/approve", async (ApproveRequest body, PendingApprovals pending, ElsaClient elsa, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(body.WorkflowInstanceId))
        return Results.BadRequest(new { error = "workflowInstanceId is required (from Elsa Studio or the x-elsa-workflow-instance-id response header)." });
    if (string.IsNullOrWhiteSpace(body.RunId))
        return Results.BadRequest(new { error = "runId is required." });

    var gate = pending.Get(body.RunId);
    if (gate is null)
        return Results.NotFound(new { error = $"No pending gate for runId '{body.RunId}'. Evaluate it first via the quality-gate webhook." });

    if (!body.Approved)
    {
        pending.Close(body.RunId);
        await elsa.SignalAsync(body.WorkflowInstanceId, body.ApproverId, approved: false, body.Comment ?? "rejected", ct);
        return Results.Ok(new { runId = body.RunId, status = "Rejected", approvalsReceived = gate.Approvals.Count, resumed = true });
    }

    try
    {
        gate.AddApproval(body.ApproverId, body.Comment ?? "");
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }

    var complete = gate.Approvals.Count >= gate.RequiredApprovals;
    if (complete)
    {
        await elsa.SignalAsync(body.WorkflowInstanceId, body.ApproverId, approved: true, body.Comment ?? "approved", ct);
        pending.Close(body.RunId);
    }

    return Results.Ok(new
    {
        runId = body.RunId,
        requiredTier = gate.Tier.ToString(),
        requiredApprovals = gate.RequiredApprovals,
        approvalsReceived = gate.Approvals.Count,
        status = complete ? "Approved" : "AwaitingApproval",
        resumed = complete,
        reasonSummary = gate.ReasonSummary
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public sealed record EvaluateRequest(
    string RunId,
    string? Table,
    long RowCount,
    decimal RowCountDriftPct,
    decimal NullRatePct,
    string[]? NewColumns,
    decimal FinancialExposure = 0,
    bool ContainsSensitiveData = false,
    bool IsRegulatoryReporting = false);

public sealed record ApproveRequest(
    string WorkflowInstanceId,
    string RunId,
    Guid ApproverId,
    bool Approved = true,
    string? Comment = null);

public sealed class PendingApprovals
{
    private readonly ConcurrentDictionary<string, PendingGate> _byRun = new(StringComparer.Ordinal);

    public void Open(string runId, Guid requestId, int requiredApprovals, ApproverTier tier, string reasonSummary) =>
        _byRun[runId] = new PendingGate(requestId, requiredApprovals, tier, reasonSummary);

    public PendingGate? Get(string runId) => _byRun.TryGetValue(runId, out var g) ? g : null;

    public void Close(string runId) => _byRun.TryRemove(runId, out _);
}

public sealed class PendingGate(Guid requestId, int requiredApprovals, ApproverTier tier, string reasonSummary)
{
    public Guid RequestId { get; } = requestId;
    public int RequiredApprovals { get; } = requiredApprovals;
    public ApproverTier Tier { get; } = tier;
    public string ReasonSummary { get; } = reasonSummary;
    public List<ApprovalEntry> Approvals { get; } = [];

    public void AddApproval(Guid approverId, string comment)
    {
        if (Approvals.Any(a => a.ApproverId == approverId))
            throw new InvalidOperationException("DuplicateApprover");
        Approvals.Add(new ApprovalEntry(approverId, comment, DateTime.UtcNow));
    }
}

public sealed record ApprovalEntry(Guid ApproverId, string Comment, DateTime AtUtc);

public sealed class ElsaClient(HttpClient http, IConfiguration config)
{
    private readonly string _base = (config["ELSA_URL"] ?? "http://elsa:8080").TrimEnd('/');
    private readonly string _user = config["ELSA_USER"] ?? "admin";
    private readonly string _password = config["ELSA_PASSWORD"] ?? "password";

    public async Task SignalAsync(string instanceId, Guid approverId, bool approved, string comment, CancellationToken ct)
    {
        var login = await http.PostAsJsonAsync($"{_base}/elsa/api/identity/login",
            new { username = _user, password = _password }, ct);
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>(ct)
            ?? throw new InvalidOperationException("Elsa login returned empty body.");

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{_base}/elsa/api/workflow-instances/{instanceId}/signals/StewardDecision");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        req.Content = JsonContent.Create(new { approverId, approved, comment });

        var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    private sealed record LoginResponse(
        [property: JsonPropertyName("accessToken")] string AccessToken);
}
