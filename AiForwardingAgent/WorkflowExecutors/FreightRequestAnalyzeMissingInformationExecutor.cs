using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

internal sealed partial class FreightRequestAnalyzeMissingInformationExecutor(
    IFreightRequestReplyAnalysisAgent freightRequestReplyAnalysisAgentService,
    IFreightRequestUpdateService freightRequestUpdateService)
    : Executor<FreightRequestDto, FreightRequestDto>("FreightRequestAnalyzeMissingInformationExecutor")
{
    public override async ValueTask<FreightRequestDto> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var analyzeResult = await freightRequestReplyAnalysisAgentService
            .AnalyzeFreightRequestReplyAsync(message, cancellationToken);

        var result = freightRequestUpdateService.Apply(message, analyzeResult);

        var freightRequest = result.FreightRequest;

        if (result.RequiresManualReview)
        {
            var reasons = new List<string>();
            if (!string.IsNullOrWhiteSpace(analyzeResult.Reason))
                reasons.Add(analyzeResult.Reason);
            reasons.AddRange(result.RejectedUpdates.Select(x => $"{x.Field}: {x.Reason}"));

            freightRequest.RequiresManualReview = true;
            freightRequest.ManualReviewReason = string.Join(Environment.NewLine, reasons);
        }

        return freightRequest;
    }
}