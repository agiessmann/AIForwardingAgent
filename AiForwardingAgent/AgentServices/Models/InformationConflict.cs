namespace AiForwardingAgent.AgentServices.Models;

public sealed class InformationConflict
{
    public required string Field { get; init; }

    public string? ExistingValue { get; init; }

    public string? NewValue { get; init; }

    public string? Reason { get; init; }
}