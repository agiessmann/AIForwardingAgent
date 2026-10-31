using System.Text.RegularExpressions;
using AiForwardingAgent.AgentServices.Enums;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Models;
using AiForwardingAgent.Data.Services;

namespace AiForwardingAgent.AgentServices;

public interface IFreightRequestMissingInformationReplyAgentService
{
    Task<FreightRequestDto> CheckIfFreightRequestIsMissingInfoResponseAsync(
        FreightRequestDto freightRequestDto,
        CancellationToken cancellationToken = default);
}

public class FreightRequestMissingInformationReplyAgentService(IFreightRequestService freightRequestService)
    : IFreightRequestMissingInformationReplyAgentService
{
    public async Task<FreightRequestDto> CheckIfFreightRequestIsMissingInfoResponseAsync(
        FreightRequestDto freightRequest,
        CancellationToken cancellationToken = default)
    {
        
        // The freshly received email is typed as OriginalEmail until we know it belongs to an existing request
        var incomingEmail = freightRequest.FreightRequestEmailHistory?
                                .LastOrDefault(e => e.Direction == EmailDirection.Incoming)
                            ?? throw new ArgumentException("No incoming email found in the history.", nameof(freightRequest));
        
        // Check the subject text for freight request number pattern (e.g., frq: 12345)
        var freightRequestNumberPattern = @"frq:\s*(\d+)";
        var match = Regex.Match(incomingEmail.Subject ?? string.Empty, freightRequestNumberPattern, RegexOptions.IgnoreCase);
        
        // match.Success will be true if the pattern is found extract the freight request number
        if (match.Success)
        {
            var freightRequestNumber = match.Groups[1].Value;
            var freightRequestId = int.TryParse(freightRequestNumber, out var id) ? id : 0;

            freightRequest.IsFreightRequest = true;
            freightRequest.Confidence = 1.0m;
            freightRequest.FreightRequestId = freightRequestId;
            freightRequest.Reason = $"Email subject contains 'frq:' with freight request number {freightRequestNumber
            }. This is a freight request missing information response.";
            
            // Load freightrequest from Db 
            var freightRequestFromDb = await freightRequestService.GetFreightRequestDtoByIdAsync(freightRequestId);
            if (freightRequestFromDb is null)
            {
                return freightRequest;
            }

            incomingEmail.EmailType = FreightRequestEmailType.CustomerReply;
            await freightRequestService.AddEmailToHistoryAsync(freightRequestId, new Email
            {
                Subject = incomingEmail.Subject,
                Body = incomingEmail.Body,
                MessageId = incomingEmail.MessageId,
                EmailSender = incomingEmail.EmailSender,
                EmailRecipient = incomingEmail.EmailRecipient,
                EmailType = FreightRequestEmailType.CustomerReply,
                Direction = EmailDirection.Incoming,
                SentReceivedDate = incomingEmail.SentReceivedDate?.UtcDateTime
            });

            freightRequestFromDb = await freightRequestService.GetFreightRequestDtoByIdAsync(freightRequestId);
            freightRequestFromDb!.IsFreightRequest = true;
            freightRequestFromDb.Confidence = freightRequest.Confidence;
            freightRequestFromDb.Reason = freightRequest.Reason;
            return freightRequestFromDb;
        }

        freightRequest.IsFreightRequest = false;
        freightRequest.Confidence = 0.0m;
        freightRequest.Reason = "Email subject does not contain 'frq:'. Not a freight request missing information response.";
        freightRequest.FreightRequestId = 0;
        return await ValueTask.FromResult(freightRequest);
        
    }
}