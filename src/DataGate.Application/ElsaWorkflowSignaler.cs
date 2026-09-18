using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataGate.Promotions;

public interface IElsaWorkflowSignaler
{
    Task SignalStewardDecisionAsync(string workflowInstanceId, Guid approverId, bool approved, string comment, CancellationToken cancellationToken = default);

    /// <summary>POST pipeline metrics to Elsa's quality-gate webhook (same as curl simulate).</summary>
    Task<ElsaWebhookResult> TriggerQualityGateAsync(RunMetricsDto metrics, CancellationToken cancellationToken = default);
}

public sealed record ElsaWebhookResult(int StatusCode, string Body);

public sealed class ElsaWorkflowSignaler(HttpClient http, IConfiguration config, ILogger<ElsaWorkflowSignaler> log) : IElsaWorkflowSignaler
{
    public async Task SignalStewardDecisionAsync(string workflowInstanceId, Guid approverId, bool approved, string comment, CancellationToken cancellationToken = default)
    {
        var bas = BaseUrl();
        var tokens = await LoginAsync(cancellationToken);

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{bas}/elsa/api/workflow-instances/{workflowInstanceId}/signals/StewardDecision");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        req.Content = JsonContent.Create(new { approverId, approved, comment });

        var res = await http.SendAsync(req, cancellationToken);
        if (!res.IsSuccessStatusCode)
            log.LogWarning("Elsa signal failed: {Status} {Body}", res.StatusCode, await res.Content.ReadAsStringAsync(cancellationToken));
        res.EnsureSuccessStatusCode();
    }

    public async Task<ElsaWebhookResult> TriggerQualityGateAsync(RunMetricsDto metrics, CancellationToken cancellationToken = default)
    {
        var bas = BaseUrl();
        // compose maps HTTP__BASEPATH=/workflows → public path /workflows/quality-gate
        var url = $"{bas}/workflows/quality-gate";
        var res = await http.PostAsJsonAsync(url, new
        {
            runId = metrics.RunId,
            table = metrics.Table,
            rowCount = metrics.RowCount,
            rowCountDriftPct = metrics.RowCountDriftPct,
            nullRatePct = metrics.NullRatePct,
            newColumns = metrics.NewColumns,
            financialExposure = metrics.FinancialExposure,
            containsSensitiveData = metrics.ContainsSensitiveData,
            isRegulatoryReporting = metrics.IsRegulatoryReporting
        }, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
            log.LogWarning("Elsa quality-gate failed: {Status} {Body}", res.StatusCode, body);
        return new ElsaWebhookResult((int)res.StatusCode, body);
    }

    private string BaseUrl() => (config["Elsa:BaseUrl"] ?? "http://elsa:8080").TrimEnd('/');

    private async Task<LoginResponse> LoginAsync(CancellationToken cancellationToken)
    {
        var bas = BaseUrl();
        var user = config["Elsa:UserName"] ?? "admin";
        var password = config["Elsa:Password"] ?? "password";
        var login = await http.PostAsJsonAsync($"{bas}/elsa/api/identity/login",
            new { username = user, password }, cancellationToken);
        login.EnsureSuccessStatusCode();
        return await login.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty Elsa login response.");
    }

    private sealed record LoginResponse([property: JsonPropertyName("accessToken")] string AccessToken);
}
