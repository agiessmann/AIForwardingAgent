using System.ComponentModel.DataAnnotations;
using AiForwardingAgent.AgentServices;

namespace AiForwardingAgent.Data.Models;

public class FreightRequest
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Optional: Original-E-Mail zur Nachvollziehbarkeit
    [MaxLength(100)]
    public string? SourceEmailText { get; set; }

    [MaxLength(10)]
    public string? SourceMessageId { get; set; }

    public TransportMode? TransportMode { get; set; }
    public ShipmentType? ShipmentType { get; set; }
    public bool? QuotationRequested { get; set; }

    public DateOnly? PickupDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }

    public string? Incoterm { get; set; }
    [MaxLength(100)]
    public string? GoodsDescription { get; set; }

    public decimal? GrossWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public decimal? LoadingMeters { get; set; }

    [MaxLength(100)]
    public string? ContainerType { get; set; }

    public bool? DangerousGoods { get; set; }
    
    [MaxLength(100)]
    public string? DangerousGoodsDetails { get; set; }

    public bool? TemperatureControlled { get; set; }
    public decimal? MinimumTemperatureCelsius { get; set; }
    public decimal? MaximumTemperatureCelsius { get; set; }

    public bool? Stackable { get; set; }
    public bool? CustomsRequired { get; set; }

    [MaxLength(100)]
    public string? HsCode { get; set; }
    public string? CustomerReference { get; set; }
    
    [MaxLength(100)]
    public string? SpecialInstructions { get; set; }
    
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? PickupLocationId { get; set; }
    public FreightLocation? PickupLocation { get; set; }

    public int? DeliveryLocationId { get; set; }
    public FreightLocation? DeliveryLocation { get; set; }

    public List<FreightPackage> Packages { get; set; } = [];
}