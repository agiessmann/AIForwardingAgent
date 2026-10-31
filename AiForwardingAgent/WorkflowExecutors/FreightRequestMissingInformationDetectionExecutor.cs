using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Enums;
using AiForwardingAgent.AgentServices.Models;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

internal sealed partial class FreightRequestMissingInformationDetectionExecutor(IFreightRequestMissingInformationReplyAgentService freightRequestMissingInformationReplyAgentService)
    : Executor<FreightRequestDto, FreightRequestDto>("FreightRequestMissingInformationDetectionExecutor")

{
    public override async ValueTask<FreightRequestDto> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var email = message.FreightRequestEmailHistory?.FirstOrDefault(e => e.EmailType == FreightRequestEmailType.OriginalEmail)
                    ?? throw new ArgumentNullException(nameof(message.FreightRequestEmailHistory), "No customer reply email found in the history.");
        var subject = email.Subject;
        var body = email.Body;
        var messageId = email.MessageId;
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
        
        var result = await freightRequestMissingInformationReplyAgentService.CheckIfFreightRequestIsMissingInfoResponseAsync(message, cancellationToken);
        return await ValueTask.FromResult(result);
    }
}