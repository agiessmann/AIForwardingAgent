using AiForwardingAgent.AgentServices;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

internal sealed partial class FreightRequestDetectionExecutor(IFreightRequestAgentService freightRequestAgentService)
    : Executor<FreightRequestEmail, FreightRequestCheckResult>("FreightRequestDetectionExecutor")
{
    [MessageHandler]
    public override async ValueTask<FreightRequestCheckResult> HandleAsync(FreightRequestEmail message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var subject = message.Subject;
        var body = message.Body;
        var messageId = message.MessageId;
        // check for valid freight request in the email subject and body
        if(subject is null) 
        {
            throw new ArgumentNullException(nameof(subject), "Email subject cannot be null.");
        }
        
        if(body is null)
        {
            throw new ArgumentNullException(nameof(body), "Email body cannot be null.");
        }
        
        if(messageId is null)
        {
            throw new ArgumentNullException(nameof(messageId), "Email MessageId cannot be null.");
        }
        
        var result = await freightRequestAgentService.CheckIfFreightRequestAsync(subject, body, messageId, cancellationToken);
        return await ValueTask.FromResult(result);
    }
}