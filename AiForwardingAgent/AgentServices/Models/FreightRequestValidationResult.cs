namespace AiForwardingAgent.AgentServices.Models;

public enum RequirementLevel
{
    Optional,
    Recommended,
    Required
}

public sealed class FreightRequestValidationResult
{
    public List<MissingInformation> MissingInformation { get; init; } = [];

    public bool IsProcessable =>
        MissingInformation.All(x => x.Level != RequirementLevel.Required);

    public IEnumerable<MissingInformation> Required =>
        MissingInformation.Where(x => x.Level == RequirementLevel.Required);

    public IEnumerable<MissingInformation> Recommended =>
        MissingInformation.Where(x => x.Level == RequirementLevel.Recommended);
}