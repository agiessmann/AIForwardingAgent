namespace AiForwardingAgent.AgentServices.Models;

public sealed class UnresolvedInformation
{
    public required string Field { get; init; }

    public required string Reason { get; init; }
}