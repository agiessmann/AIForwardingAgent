using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Enums;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Models;
using AiForwardingAgent.Data.Services;
using AiForwardingAgent.EmailService;
using AiForwardingAgent.ForwardingSkills;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

public class CreateFreightRequestMissingInfomationEmailExecutor(IEmailMailboxService emailMailboxService, 
    IFreightRequestMissingInformationAgentService freightRequestMissingInformationAgentService,
        IFreightRequirementMatrixService freightRequirementMatrixService, IFreightRequestService freightRequestService,
        string id = "CreateFreightRequestMissingInfomationEmailExecutor")
    : Executor<FreightRequestDto, CreateFreightRequestMissingInfomationEmailExecutor>(id)
{
    public override async ValueTask<CreateFreightRequestMissingInfomationEmailExecutor> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // Get the original email from the freight request email history
        var email = message.FreightRequestEmailHistory?.FirstOrDefault(e => e.EmailType == FreightRequestEmailType.OriginalEmail)
                    ?? throw new ArgumentNullException(nameof(message.FreightRequestEmailHistory), "No original email found in the history.");
        
        // Replies must be sent to the latest customer mail, not to the original request.
        var replyTarget = message.FreightRequestEmailHistory!
            .Where(e => e.Direction == EmailDirection.Incoming && e.MessageId is not null)
            .OrderBy(e => e.SentReceivedDate)
            .LastOrDefault() ?? email;

        // Misssing Information Check
        var missingInformation = await freightRequirementMatrixService.ValidateAsync(message.ShipmentInformation ?? throw new ArgumentNullException(nameof(message.ShipmentInformation), "ShipmentInformation cannot be null when checking for missing information."), cancellationToken);
        
        // If there is missing information, send an email to the customer
        if (missingInformation.IsProcessable)
        {
            if(message.FreightRequestId is null)
            {
                throw new ArgumentNullException(nameof(message.FreightRequestId), "RequestId cannot be null when processing missing information.");
            }
            var statusUpdate = await freightRequestService.SaveFreightRequestStatusAsync(message.FreightRequestId.Value, FreightRequestStatus.InProgress);
            return this;
        }
        if (email?.Body != null)
        {
            var emailContent =
                await freightRequestMissingInformationAgentService.CreateMissingInformationEmailAsync(
                    message, cancellationToken);
                
            await emailMailboxService.ReplyToMessageAsync(
                messageId: replyTarget.MessageId ?? throw new ArgumentNullException(nameof(replyTarget.MessageId), "Email MessageId cannot be null."),
                textBody: emailContent,
                //htmlBody: $"<p>{emailContent}</p>",
                subjectPrefix: $"frq: {message.FreightRequestId} ",
                cancellationToken: cancellationToken);
        
            await emailMailboxService.MarkAsReadAsync(
                messageId: replyTarget.MessageId.ToString() ?? throw new ArgumentNullException(nameof(replyTarget.MessageId), "Email MessageId cannot be null."),
                cancellationToken: cancellationToken);
        
            await emailMailboxService.MarkAsFlaggedAsync(
                messageId: replyTarget.MessageId ?? throw new ArgumentNullException(nameof(replyTarget.MessageId), "Email MessageId cannot be null."),
                flagColor: AppleMailFlagColor.Green,
                cancellationToken: cancellationToken);

            if (message.FreightRequestId is { } freightRequestId)
            {
                await freightRequestService.AddEmailToHistoryAsync(freightRequestId, new Email
                {
                    Subject = $"frq: {freightRequestId} {email.Subject}".Trim(),
                    Body = emailContent,
                    EmailSender = email.EmailRecipient,
                    EmailRecipient = email.EmailSender,
                    EmailType = FreightRequestEmailType.MissingInformationRequest,
                    Direction = EmailDirection.Outgoing,
                    SentReceivedDate = DateTime.UtcNow
                });
            }
        }
        
        var statusUpdateMissingInfo = await freightRequestService.SaveFreightRequestStatusAsync(message.FreightRequestId ?? throw new ArgumentNullException(nameof(message.FreightRequestId), "RequestId cannot be null when processing missing information."), FreightRequestStatus.MissingInformation);

        return this;
    }
}