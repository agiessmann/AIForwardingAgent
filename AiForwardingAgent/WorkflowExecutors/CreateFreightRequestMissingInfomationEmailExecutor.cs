using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Models;
using AiForwardingAgent.Data.Services;
using AiForwardingAgent.EmailService;
using AiForwardingAgent.ForwardingSkills;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

public class CreateFreightRequestMissingInfomationEmailExecutor(IEmailMailboxService emailMailboxService, 
    IFreightRequestMissingInformationAgentService freightRequestMissingInformationAgentService,
        IFreightRequirementMatrixService freightRequirementMatrixService, IFreightRequestService freightRequestService)
    : Executor<FreightRequestDto, CreateFreightRequestMissingInfomationEmailExecutor>("CreateFreightRequestMissingInfomationEmailExecutor")
{
    public override async ValueTask<CreateFreightRequestMissingInfomationEmailExecutor> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
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
        if (message.FreightRequestEmail?.Body != null)
        {
            var emailContent =
                await freightRequestMissingInformationAgentService.CreateMissingInformationEmailAsync(
                    message, cancellationToken);
                
            await emailMailboxService.ReplyToMessageAsync(
                messageId: message.FreightRequestEmail.MessageId ?? throw new ArgumentNullException(nameof(message.FreightRequestEmail.MessageId), "Email MessageId cannot be null."),
                textBody: emailContent,
                //htmlBody: $"<p>{emailContent}</p>",
                subjectPrefix: $"frq: {message.FreightRequestId} ",
                cancellationToken: cancellationToken);
        
            await emailMailboxService.MarkAsReadAsync(
                messageId: message.FreightRequestEmail.MessageId.ToString() ?? throw new ArgumentNullException(nameof(message.FreightRequestEmail.MessageId), "Email MessageId cannot be null."),
                cancellationToken: cancellationToken);
        
            await emailMailboxService.MarkAsFlaggedAsync(
                messageId: message.FreightRequestEmail.MessageId ?? throw new ArgumentNullException(nameof(message.FreightRequestEmail.MessageId), "Email MessageId cannot be null."),
                flagColor: AppleMailFlagColor.Green,
                cancellationToken: cancellationToken);
        }
        
        var statusUpdateMissingInfo = await freightRequestService.SaveFreightRequestStatusAsync(message.FreightRequestId ?? throw new ArgumentNullException(nameof(message.FreightRequestId), "RequestId cannot be null when processing missing information."), FreightRequestStatus.MissingInformation);

        return this;
    }
}