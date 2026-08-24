using AiForwardingAgent.AgentServices;
using AiForwardingAgent.Data.Models;
using AiForwardingAgent.EmailService;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

public class ConfirmFreightRequestEmailExecutor(IEmailMailboxService emailMailboxService)
    : Executor<ShipmentInformation, ConfirmFreightRequestEmailExecutor>("ConfirmFreightRequestEmailExecutor")
{
    [MessageHandler]
    public override async ValueTask<ConfirmFreightRequestEmailExecutor> HandleAsync(ShipmentInformation message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message), "Email MessageId cannot be null.");
        }   
        
        // Implement the logic to handle the confirmation of the freight request email
        // For example, you can send a confirmation email back to the sender
        await emailMailboxService.ReplyToMessageAsync(
            messageId: message.MessageId ?? throw new ArgumentNullException(nameof(message.MessageId), "Email MessageId cannot be null."),
            textBody: "Thank you for your freight request. We have received your request and will process it shortly.",
            htmlBody:
            "<p>Thank you for your freight request. We have received your request and will process it shortly.</p>",
            subjectPrefix: $"frq: {message.RequestId} ",
            cancellationToken: cancellationToken);
        
        return this;
    }

    
}