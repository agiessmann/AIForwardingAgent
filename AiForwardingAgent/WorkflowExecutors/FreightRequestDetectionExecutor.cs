using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

internal sealed partial class FreightRequestDetectionExecutor(IFreightRequestAgentService freightRequestAgentService)
    : Executor<FreightRequestDto, FreightRequestDto>("FreightRequestDetectionExecutor")
{
    [MessageHandler]
    public override async ValueTask<FreightRequestDto> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var email = message.FreightRequestEmail 
                    ?? throw new ArgumentNullException(nameof(message.FreightRequestEmail), "FreightRequestEmail cannot be null.");
        
        var subject = email.Subject;
        var body = email;
        var messageId =  email.MessageId;
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
        
        var result = await freightRequestAgentService.CheckIfFreightRequestAsync(message, cancellationToken);
        return await ValueTask.FromResult(result);
    }
}