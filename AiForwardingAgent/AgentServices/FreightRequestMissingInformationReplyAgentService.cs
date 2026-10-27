using System.Text.RegularExpressions;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.AgentServices;

public interface IFreightRequestMissingInformationReplyAgentService
{
    Task<FreightRequestDto> CheckIfFreightRequestIsMissingInfoResponseAsync(
        FreightRequestDto freightRequestDto,
        CancellationToken cancellationToken = default);
}

public class FreightRequestMissingInformationReplyAgentService : IFreightRequestMissingInformationReplyAgentService
{
    public async Task<FreightRequestDto> CheckIfFreightRequestIsMissingInfoResponseAsync(
        FreightRequestDto freightRequest,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async work if needed
        // Check the email subject if it contains frq:
        
        
        // Check the subject text for freight request number pattern (e.g., frq: 12345)
        var freightRequestNumberPattern = @"frq:\s*(\d+)";
        var match = Regex.Match(freightRequest.FreightRequestEmail.Subject, freightRequestNumberPattern, RegexOptions.IgnoreCase);
        
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
        }
        else
        {
            freightRequest.IsFreightRequest = false;
            freightRequest.Confidence = 0.0m;
            freightRequest.Reason = "Email subject does not contain 'frq:'. Not a freight request missing information response.";
            freightRequest.FreightRequestId = 0;
            
        }
        return await ValueTask.FromResult(freightRequest);
        
    }
}