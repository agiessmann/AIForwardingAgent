using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.Data.Services;
using AiForwardingAgent.ForwardingSkills;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

public class CreateFreightRequestToDbExecutor(
    IFreightRequestService freightRequestService, 
    IFreightRequirementMatrixService freightRequirementMatrixService)
    : Executor<FreightRequestDto, FreightRequestDto>("CreateFreightRequestToDbExecutor")
{
    public override async ValueTask<FreightRequestDto> HandleAsync(FreightRequestDto message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (message.ShipmentInformation != null)
        {
            var requirementMatrix =  await freightRequirementMatrixService.ValidateAsync(message.ShipmentInformation, cancellationToken);
            message.MissingInformation = requirementMatrix.MissingInformation;
        }
        
        var freightRequest = FreightRequestMapper.ToEntity(message);
        if (freightRequest is null)
        {
            throw new ArgumentNullException(nameof(freightRequest), "Freight request cannot be null.");
        }
        
        var resultFreightRequest = await freightRequestService.UpsertFreightRequestAsync(freightRequest);

        message.FreightRequestId = resultFreightRequest.Id;
        if (message.ShipmentInformation is not null)
        {
            message.ShipmentInformation.RequestId = resultFreightRequest.Id;
        }

        return await ValueTask.FromResult(message);

    }
}