using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AiForwardingAgent.AgentServices;
using Google.Protobuf.Reflection;

namespace AiForwardingAgent.Data.Models;

public class Shipment
{

    [Key]
    public int ShipmentId { get; set; }
    public int? RequestId { get; set; }
    public TransportMode? TransportMode { get; set; }
    public ShipmentType? ShipmentType { get; set; }
    public bool? QuotationRequested { get; init; }
    public Location? Pickup { get; set; }
    public Location? Delivery { get; set; }
    public TradeTerm? OriginTerm { get; set; }
    public TradeTerm? DestinationTerm { get; set; }
    public DateOnly? PickupDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    
    [MaxLength(50)]
    public string? Incoterm { get; set; }
    
    [MaxLength(1000)]
    public string? GoodsDescription { get; set; }

    // Gesamtwerte der Sendung
    public decimal? GrossWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public decimal? LoadingMeters { get; set; }

    // Einzelne Packstücke / Packstückgruppen
    [NotMapped]
    public List<PackageDto>? Packages { get; init; } = [];
    public string? ContainerType { get; set; }
    public bool? DangerousGoods { get; set; }
    public string? DangerousGoodsDetails { get; set; }
    public bool? TemperatureControlled { get; set; }
    public decimal? MinimumTemperatureCelsius { get; set; }
    public decimal? MaximumTemperatureCelsius { get; set; }
    public bool? Stackable { get; set; }
    public bool? CustomsRequired { get; set; }
    
    [MaxLength(50)]    
    public string? HsCode { get; set; }
    
    [MaxLength(100)]
    public string? CustomerReference { get; set; }
    
    [MaxLength(100)]
    public string? SpecialInstructions { get; set; }
    
    
    
}