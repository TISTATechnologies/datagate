using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace DataGate.Workflow.Activities;

/// <summary>
/// The suspension point. Execution stops here, state is persisted, and the
/// instance survives a process restart or a redeploy. It resumes when the
/// bookmark is triggered by an approval posted from the ABP side.
///
/// This is the capability a cron job or a retry loop cannot give you.
/// </summary>
[Activity("DataGate", "Governance", "Wait for the required number of human approvals")]
public class AwaitApprovalActivity : Activity<ApprovalOutcome>
{
    [Input(Description = "The promotion request being approved.")]
    public Input<Guid> RequestId { get; set; } = default!;

    [Input(Description = "How many distinct approvers must sign.")]
    public Input<int> RequiredApprovals { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var requestId = context.Get(RequestId);
        var required = context.Get(RequiredApprovals);

        context.SetProperty("ApprovalsReceived", 0);
        context.SetProperty("RequiredApprovals", required);

        // Creating a bookmark is what suspends the workflow. Nothing is polling.
        context.CreateBookmark(
            new ApprovalStimulus(requestId),
            OnApprovalReceivedAsync,
            includeActivityInstanceId: true);
    }

    private async ValueTask OnApprovalReceivedAsync(ActivityExecutionContext context)
    {
        var signal = context.GetWorkflowInput<ApprovalSignal>();
        var received = context.GetProperty<int>("ApprovalsReceived") + 1;
        var required = context.GetProperty<int>("RequiredApprovals");

        context.SetProperty("ApprovalsReceived", received);

        if (!signal.Approved)
        {
            context.SetResult(new ApprovalOutcome(false, received, signal.ApproverId));
            await context.CompleteActivityAsync();
            return;
        }

        if (received < required)
        {
            // Two-person rule not yet satisfied: re-suspend and keep waiting.
            context.CreateBookmark(
                new ApprovalStimulus(signal.RequestId), OnApprovalReceivedAsync, true);
            return;
        }

        context.SetResult(new ApprovalOutcome(true, received, signal.ApproverId));
        await context.CompleteActivityAsync();
    }
}

public sealed record ApprovalStimulus(Guid RequestId);
public sealed record ApprovalSignal(Guid RequestId, Guid ApproverId, bool Approved, string Comment);
public sealed record ApprovalOutcome(bool Approved, int ApprovalCount, Guid LastApproverId);
