using System.Text.Json;
using AiForwardingAgent.AgentServices;

namespace AiForwardingAgent.AgentServices;

public class ShipmentInformation
{
    public int? RequestId { get; set; }
    public TransportMode? TransportMode { get; init; }
    public ShipmentType? ShipmentType { get; init; }
    public bool? QuotationRequested { get; init; }
    public LocationInformation? Pickup { get; init; }
    public LocationInformation? Delivery { get; init; }
    public DateOnly? PickupDate { get; init; }
    public DateOnly? DeliveryDate { get; init; }
    public string? Incoterm { get; init; }
    public string? GoodsDescription { get; init; }

    // Gesamtwerte der Sendung
    public decimal? GrossWeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public decimal? LoadingMeters { get; init; }

    // Einzelne Packstücke / Packstückgruppen
    public List<PackageInformation> Packages { get; init; } = [];
    public string? ContainerType { get; init; }
    public bool? DangerousGoods { get; init; }
    public string? DangerousGoodsDetails { get; init; }
    public bool? TemperatureControlled { get; init; }
    public decimal? MinimumTemperatureCelsius { get; init; }
    public decimal? MaximumTemperatureCelsius { get; init; }
    public bool? Stackable { get; init; }
    public bool? CustomsRequired { get; init; }
    public string? HsCode { get; init; }
    public string? CustomerReference { get; init; }
    public string? SpecialInstructions { get; init; }

    public string? MessageId { get; set; } = string.Empty;
    public string? MessageText { get; set; } = string.Empty;
    
    private static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public static ShipmentInformation? FromJson(string json)
    {
        return JsonSerializer.Deserialize<ShipmentInformation>(json, Options);
    }
}