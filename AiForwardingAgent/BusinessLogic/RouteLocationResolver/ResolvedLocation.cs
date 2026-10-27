namespace AiForwardingAgent.BusinessLogic.RouteLocationResolver;

public sealed class ResolvedLocation
{
    public string? Company { get; init; }

    public string? Address { get; init; }
    public string? PostalCode { get; init; }

    public string? City { get; init; }
    public string? State { get; init; }

    public string? Country { get; init; }
    public string? CountryCode { get; init; }

    public string? SourceValue { get; init; }

    public LocationSource Source { get; init; }

    public LocationResolutionStatus Status { get; init; }
}

public enum LocationResolutionStatus
{
    Resolved,
    PartiallyResolved,
    Ambiguous,
    NotResolved
}

public enum LocationSource
{
    Pickup,
    Delivery,
    OriginTradeTerm,
    DestinationTradeTerm
}