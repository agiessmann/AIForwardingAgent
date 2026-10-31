namespace AiForwardingAgent.AgentServices.Models;

public sealed class AnsweredMissingInformation
{
    public required string Field { get; init; }

    public required string QuestionKey { get; init; }
}