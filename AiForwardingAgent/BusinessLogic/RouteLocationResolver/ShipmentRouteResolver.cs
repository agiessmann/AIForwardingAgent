using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;

namespace AiForwardingAgent.BusinessLogic.RouteLocationResolver;

public interface IShipmentRouteResolver
{
    Task<ShipmentRouteInformation> ResolveAsync(
        ShipmentDto shipment,
        CancellationToken cancellationToken = default);
}


public sealed class ShipmentRouteResolver(ILocationResolver locationResolver) : IShipmentRouteResolver
{
    public async Task<ShipmentRouteInformation> ResolveAsync(
        ShipmentDto shipment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        var origin = await ResolveOriginAsync(
            shipment,
            cancellationToken);

        var destination = await ResolveDestinationAsync(
            shipment,
            cancellationToken);

        return new ShipmentRouteInformation
        {
            Origin = origin,
            Destination = destination
        };
    }

    private async Task<ResolvedLocation?> ResolveOriginAsync(
        ShipmentDto shipment,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(
                shipment.OriginTerm?.NamedPlace))
        {
            return await locationResolver.ResolvePlaceAsync(
                shipment.OriginTerm.NamedPlace,
                LocationSource.OriginTradeTerm,
                cancellationToken);
        }

        if (HasLocationInformation(shipment.Pickup))
        {
            return await locationResolver.ResolveLocationAsync(
                shipment.Pickup!,
                LocationSource.Pickup,
                cancellationToken);
        }

        return null;
    }

    private async Task<ResolvedLocation?> ResolveDestinationAsync(
        ShipmentDto shipment,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(
                shipment.DestinationTerm?.NamedPlace))
        {
            return await locationResolver.ResolvePlaceAsync(
                shipment.DestinationTerm.NamedPlace,
                LocationSource.DestinationTradeTerm,
                cancellationToken);
        }

        if (HasLocationInformation(shipment.Delivery))
        {
            return await locationResolver.ResolveLocationAsync(
                shipment.Delivery!,
                LocationSource.Delivery,
                cancellationToken);
        }

        return null;
    }

    private static bool HasLocationInformation(
        LocationDto? location)
    {
        if (location is null)
            return false;

        return
            !string.IsNullOrWhiteSpace(location.Address) ||
            !string.IsNullOrWhiteSpace(location.PostalCode) ||
            !string.IsNullOrWhiteSpace(location.City) ||
            !string.IsNullOrWhiteSpace(location.State) ||
            !string.IsNullOrWhiteSpace(location.Country);
    }
}