using AiForwardingAgent.AgentServices;
using AiForwardingAgent.Data.Models;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

internal sealed partial class FreightRequestAnalyzeExecutor(IFreightRequestAnalyzeAgentService freightRequestAnalyzeAgentService)
    : Executor<FreightRequestCheckResult, ShipmentInformation>("FreightRequestAnalyzeExecutor")
{
    public override async ValueTask<ShipmentInformation> HandleAsync(FreightRequestCheckResult message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var subject = message.Subject;
        if (string.IsNullOrEmpty(subject)) 
        {
            throw new ArgumentNullException(nameof(subject), "Email subject cannot be null or empty.");
        }
        var text = message.EmailText;
        if (string.IsNullOrEmpty(text)) 
        {
            throw new ArgumentNullException(nameof(text), "Email text cannot be null or empty.");
        }
        var massageId = message.MessageId; 
        if (string.IsNullOrEmpty(massageId)) 
        {
            throw new ArgumentNullException(nameof(massageId), "Email MessageId cannot be null or empty.");
        }
        var result = await freightRequestAnalyzeAgentService.AnalyzeFreightRequestAsync(emailSubject: subject, emailText: text, messageId: massageId, cancellationToken);
        result.MessageId = massageId;
        result.MessageText = text;
        return await ValueTask.FromResult(result);
    }
}