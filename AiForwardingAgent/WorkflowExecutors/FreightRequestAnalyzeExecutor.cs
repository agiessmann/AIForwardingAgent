using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

internal partial class FreightRequestAnalyzeExecutor(IFreightRequestAnalyzeAgentService freightRequestAnalyzeAgentService)
    : Executor<FreightRequestDto,FreightRequestDto>("FreightRequestAnalyzeExecutor")
{
    public override async ValueTask<FreightRequestDto> HandleAsync(
        FreightRequestDto message, 
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var email = message.FreightRequestEmail 
                    ?? throw new ArgumentNullException(nameof(message.FreightRequestEmail));

        if (string.IsNullOrEmpty(email.Subject))
            throw new ArgumentNullException(nameof(email.Subject), "Email subject cannot be null or empty.");

        if (string.IsNullOrEmpty(email.Body))
            throw new ArgumentNullException(nameof(email.Body), "Email text cannot be null or empty.");

        if (string.IsNullOrEmpty(email.EmailSender))
            throw new ArgumentNullException(nameof(email.EmailSender), "Email sender cannot be null or empty.");

        var messageId = email.MessageId;
        if (string.IsNullOrEmpty(messageId?.ToString()))
            throw new ArgumentNullException(nameof(messageId), "Email MessageId cannot be null or empty.");


        var result = await freightRequestAnalyzeAgentService.AnalyzeFreightRequestAsync(message, cancellationToken);
        
        result.FreightRequestEmail =email;
        return result;
    }


    
}