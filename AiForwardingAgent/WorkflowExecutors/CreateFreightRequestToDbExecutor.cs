using AiForwardingAgent.AgentServices;
using AiForwardingAgent.Data.Models;
using AiForwardingAgent.Data.Services;
using Microsoft.Agents.AI.Workflows;

namespace AiForwardingAgent.WorkflowExecutors;

public class CreateFreightRequestToDbExecutor(
    IFreightRequestService freightRequestService, 
    IFreightLocationService freightLocationService)
    : Executor<ShipmentInformation, ShipmentInformation>("CreateFreightRequestToDbExecutor")
{
    public override async ValueTask<ShipmentInformation> HandleAsync(ShipmentInformation message, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var freightRequest = MapToEntity(message);
        if (freightRequest is null)
        {
            throw new ArgumentNullException(nameof(freightRequest), "Freight request cannot be null.");
        }
        
        if(freightRequest.PickupLocation is not null)
        {
            var result = await freightLocationService.CreateFreightLocationAsync(freightRequest.PickupLocation);
            freightRequest.PickupLocationId = result.Id;
            // Avoid creating the same location again when persisting FreightRequest.
            freightRequest.PickupLocation = null;
        }
        if (freightRequest.DeliveryLocation is not null)
        {
            var result = await freightLocationService.CreateFreightLocationAsync(freightRequest.DeliveryLocation);
            freightRequest.DeliveryLocationId = result.Id;
            // Avoid creating the same location again when persisting FreightRequest.
            freightRequest.DeliveryLocation = null;
        }
        
        freightRequest.SourceEmailText = message.MessageText;
        freightRequest.SourceMessageId = message.MessageId;
        var resultFreightRequest = await freightRequestService.CreateFreightRequestAsync(freightRequest);

        message.RequestId = resultFreightRequest.Id;
        return await ValueTask.FromResult(message);

    }

    private static FreightRequest MapToEntity(ShipmentInformation shipment)
{
    return new FreightRequest
    {
        TransportMode = shipment.TransportMode,
        ShipmentType = shipment.ShipmentType,
        QuotationRequested = shipment.QuotationRequested,

        PickupDate = shipment.PickupDate,
        DeliveryDate = shipment.DeliveryDate,

        Incoterm = shipment.Incoterm,
        GoodsDescription = shipment.GoodsDescription,

        GrossWeightKg = shipment.GrossWeightKg,
        VolumeM3 = shipment.VolumeM3,
        LoadingMeters = shipment.LoadingMeters,

        ContainerType = shipment.ContainerType,

        DangerousGoods = shipment.DangerousGoods,
        DangerousGoodsDetails = shipment.DangerousGoodsDetails,

        TemperatureControlled = shipment.TemperatureControlled,
        MinimumTemperatureCelsius = shipment.MinimumTemperatureCelsius,
        MaximumTemperatureCelsius = shipment.MaximumTemperatureCelsius,

        Stackable = shipment.Stackable,
        CustomsRequired = shipment.CustomsRequired,

        HsCode = shipment.HsCode,
        CustomerReference = shipment.CustomerReference,
        SpecialInstructions = shipment.SpecialInstructions,

        PickupLocation = shipment.Pickup is null
            ? null
            : new FreightLocation
            {
                Company = shipment.Pickup.Company,
                Address = shipment.Pickup.Address,
                PostalCode = shipment.Pickup.PostalCode,
                City = shipment.Pickup.City,
                State = shipment.Pickup.State,
                Country = shipment.Pickup.Country
            },

        DeliveryLocation = shipment.Delivery is null
            ? null
            : new FreightLocation
            {
                Company = shipment.Delivery.Company,
                Address = shipment.Delivery.Address,
                PostalCode = shipment.Delivery.PostalCode,
                City = shipment.Delivery.City,
                State = shipment.Delivery.State,
                Country = shipment.Delivery.Country
            },

        Packages =
        [
            .. shipment.Packages
                .Select(x => new FreightPackage
                {
                    Quantity = x.Quantity,
                    PackageType = x.PackageType,
                    LengthCm = x.LengthCm,
                    WidthCm = x.WidthCm,
                    HeightCm = x.HeightCm,
                    WeightPerPackageKg = x.WeightPerPackageKg,
                    TotalWeightKg = x.TotalWeightKg,
                    GoodsDescription = x.GoodsDescription,
                    Stackable = x.Stackable
                })
        ]
    };
}
}