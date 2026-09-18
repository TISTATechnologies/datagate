using System.Net.Http.Json;
using Shouldly;
using Xunit;

namespace DataGate.Workflow.Tests;

/// <summary>
/// The test that earns the talk: a workflow suspended mid-approval survives a
/// restart of the workflow server and resumes correctly. Anything can call a
/// webhook - surviving a redeploy mid-process is what a workflow engine buys you.
///
/// Requires the compose stack to be running:
///   docker compose -f deploy/docker-compose.yml up -d
/// Set DATAGATE_RESTART=1 to include the container-restart step (slower, and the
/// step you want on screen during the demo).
/// </summary>
[Trait("Category", "Integration")]
public class SuspendResumeTests : IAsyncLifetime
{
    private static readonly string ElsaUrl =
        Environment.GetEnvironmentVariable("ELSA_URL") ?? "http://localhost:13000";

    private HttpClient _client = default!;

    public Task InitializeAsync()
    {
        _client = new HttpClient { BaseAddress = new Uri(ElsaUrl), Timeout = TimeSpan.FromSeconds(30) };
        return Task.CompletedTask;
    }

    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Clean_run_completes_without_suspending()
    {
        var response = await _client.PostAsJsonAsync("/workflows/quality-gate", new
        {
            runId = $"clean-{Guid.NewGuid():N}",
            table = "silver.claims",
            rowCount = 1_048_221,
            rowCountDriftPct = 0.4,
            nullRatePct = 0.2,
            newColumns = Array.Empty<string>(),
            financialExposure = 40_000,
            containsSensitiveData = false,
            isRegulatoryReporting = false
        });

        response.EnsureSuccessStatusCode();
        var instance = await WaitForStatusAsync(await ReadInstanceIdAsync(response), "Finished");
        instance.ShouldNotBeNull();
    }

    [Fact]
    public async Task Failing_run_suspends_and_survives_a_restart_then_resumes()
    {
        var runId = $"dirty-{Guid.NewGuid():N}";

        // 1. Poison the run: heavy drift, a new PII column, material exposure.
        var start = await _client.PostAsJsonAsync("/workflows/quality-gate", new
        {
            runId,
            table = "silver.claims",
            rowCount = 412_008,
            rowCountDriftPct = 61.7,
            nullRatePct = 14.3,
            newColumns = new[] { "member_ssn" },
            financialExposure = 3_000_000,
            containsSensitiveData = true,
            isRegulatoryReporting = true
        });
        start.EnsureSuccessStatusCode();
        var instanceId = await ReadInstanceIdAsync(start);

        // 2. It must be parked, not finished, and not polling.
        var suspended = await WaitForStatusAsync(instanceId, "Suspended");
        suspended.ShouldNotBeNull();

        // 3. Kill the engine. State lives in the database, not in memory.
        if (Environment.GetEnvironmentVariable("DATAGATE_RESTART") == "1")
        {
            Restart("datagate-elsa");
            await WaitForServerAsync();
        }

        // 4. Still waiting after the restart - this is the assertion that matters.
        (await GetStatusAsync(instanceId)).ShouldBe("Suspended");

        // 5. Two distinct approvers, because sensitive data triggers the two-person rule.
        await SignalAsync(instanceId, Guid.NewGuid(), "checked lineage");
        (await GetStatusAsync(instanceId)).ShouldBe("Suspended", "one signature is not enough");

        await SignalAsync(instanceId, Guid.NewGuid(), "privacy reviewed");
        (await WaitForStatusAsync(instanceId, "Finished")).ShouldNotBeNull();
    }

    private async Task SignalAsync(string instanceId, Guid approverId, string comment)
    {
        var response = await _client.PostAsJsonAsync(
            $"/elsa/api/workflow-instances/{instanceId}/signals/StewardDecision",
            new { approverId, approved = true, comment });
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetStatusAsync(string instanceId)
    {
        var json = await _client.GetFromJsonAsync<Dictionary<string, object>>(
            $"/elsa/api/workflow-instances/{instanceId}");
        return json?["status"]?.ToString() ?? "Unknown";
    }

    private async Task<string?> WaitForStatusAsync(string instanceId, string expected, int seconds = 30)
    {
        for (var i = 0; i < seconds; i++)
        {
            if (await GetStatusAsync(instanceId) == expected) return instanceId;
            await Task.Delay(1000);
        }
        return null;
    }

    private static async Task<string> ReadInstanceIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return body?["workflowInstanceId"] ?? throw new InvalidOperationException("No instance id returned.");
    }

    private async Task WaitForServerAsync()
    {
        for (var i = 0; i < 60; i++)
        {
            try { if ((await _client.GetAsync("/")).IsSuccessStatusCode) return; }
            catch { /* still coming up */ }
            await Task.Delay(2000);
        }
        throw new TimeoutException("Elsa did not come back after restart.");
    }

    private static void Restart(string container) =>
        System.Diagnostics.Process.Start("docker", $"restart {container}")!.WaitForExit();
}
