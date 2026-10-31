using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Services;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

public class CreateFreightRequestCheckToDb(IFreightRequestCheckService freightRequestCheckService) 
    : Executor<FreightRequestDto, FreightRequestDto>("CreateFreightRequestCheckToDb")
{
    public override async ValueTask<FreightRequestDto> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var freightRequest = MapToEntity(message);
        if (freightRequest is null)    
        {
            throw new ArgumentNullException(nameof(message), "FreightRequestCheck cannot be null.");
        }
        
        //await freightRequestService.CreateFreightRequestCheckAsync(freightRequest);
        
        return await ValueTask.FromResult(message);
        
    }

    private string? nameOf(FreightRequestDto? freightRequest, string freightRequestCheckCannotBeNull)
    {
        throw new NotImplementedException();
    }

    private static FreightRequestDto MapToEntity(FreightRequestDto freightRequest)
    {
        return new FreightRequestDto
        {
            IsFreightRequest = freightRequest.IsFreightRequest,
            Confidence = freightRequest.Confidence,
            Reason = freightRequest.Reason,
            FreightRequestEmailHistory = freightRequest.FreightRequestEmailHistory
        };
    }
}