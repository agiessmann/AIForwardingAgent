namespace AiForwardingAgent.AgentServices.Models;

public sealed class MissingInformation
{
    public required string Field { get; init; }

    public RequirementLevel Level { get; init; }

    public string? Reason { get; init; }

    public required string QuestionKey { get; init; }
}