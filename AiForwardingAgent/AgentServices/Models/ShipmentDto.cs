using System.Text.Json;
using AiForwardingAgent.AgentServices;
using AiForwardingAgent.Data.Models;
using AiForwardingAgent.ForwardingSkills;

namespace AiForwardingAgent.AgentServices.Models;

public class ShipmentDto
{
    public int? RequestId { get; set; }
    public TransportMode? TransportMode { get; init; }
    public ShipmentType? ShipmentType { get; init; }
    public bool? QuotationRequested { get; init; }
    public LocationDto? Pickup { get; init; }
    public LocationDto? Delivery { get; init; }
    public TradeTermDto? OriginTerm { get; init; }
    public TradeTermDto? DestinationTerm { get; init; }
    public DateOnly? PickupDate { get; init; }
    public DateOnly? DeliveryDate { get; init; }
    public string? Incoterm { get; init; }
    public string? GoodsDescription { get; init; }

    // Gesamtwerte der Sendung
    public decimal? GrossWeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public decimal? LoadingMeters { get; init; }

    // Einzelne Packstücke / Packstückgruppen
    public List<PackageDto>? Packages { get; init; } = [];
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
    
    
    private static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public static ShipmentDto? FromJson(string json)
    {
        return JsonSerializer.Deserialize<ShipmentDto>(json, Options);
    }
}