namespace AiForwardingAgent.BusinessLogic.RouteLocationResolver;

public sealed class ShipmentRouteInformation
{
    public ResolvedLocation? Origin { get; init; }
    public ResolvedLocation? Destination { get; init; }
}