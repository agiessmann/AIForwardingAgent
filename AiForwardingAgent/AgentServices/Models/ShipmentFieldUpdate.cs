namespace AiForwardingAgent.AgentServices.Models;

public sealed class ShipmentFieldUpdate
{
    public required string Field { get; init; }

    public required string Value { get; init; }

    public string? SourceText { get; init; }

    public decimal Confidence { get; init; }
}