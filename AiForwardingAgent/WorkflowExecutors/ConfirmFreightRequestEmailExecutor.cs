using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.EmailService;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

public class ConfirmFreightRequestEmailExecutor(IEmailMailboxService emailMailboxService)
    : Executor<FreightRequestDto, ConfirmFreightRequestEmailExecutor>("ConfirmFreightRequestEmailExecutor")
{
    [MessageHandler]
    public override async ValueTask<ConfirmFreightRequestEmailExecutor> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message), "Email MessageId cannot be null.");
        }   
        
        // Implement the logic to handle the confirmation of the freight request email
        // For example, you can send a confirmation email back to the sender
        if (message.FreightRequestEmail != null)
        {
            await emailMailboxService.ReplyToMessageAsync(
                messageId: message.FreightRequestEmail.MessageId ??
                           throw new ArgumentNullException(nameof(message.FreightRequestEmail.MessageId),
                               "Email MessageId cannot be null."),
                textBody:
                "Thank you for your freight request. We have received your request and will process it shortly.",
                htmlBody:
                "<p>Thank you for your freight request. We have received your request and will process it shortly.</p>",
                subjectPrefix: $"frq: {message.FreightRequestId} ",
                cancellationToken: cancellationToken);

            await emailMailboxService.MarkAsReadAsync(
                messageId: message.FreightRequestEmail.MessageId.ToString() ??
                           throw new ArgumentNullException(nameof(message.FreightRequestEmail.MessageId),
                               "Email MessageId cannot be null."),
                cancellationToken: cancellationToken);

            await emailMailboxService.MarkAsFlaggedAsync(
                messageId: message.FreightRequestEmail.MessageId ??
                           throw new ArgumentNullException(nameof(message.FreightRequestEmail.MessageId),
                               "Email MessageId cannot be null."),
                flagColor: AppleMailFlagColor.Green,
                cancellationToken: cancellationToken);
        }

        return this;
    }

    
}