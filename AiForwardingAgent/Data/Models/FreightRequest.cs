using AiForwardingAgent.AgentServices.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiForwardingAgent.Data.Models;

public class FreightRequest
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public FreightRequestStatus Status { get; set; } = FreightRequestStatus.New;
    
    public Email? FreightRequestEmail { get; set; } = new Email();
    
    public Shipment? Shipment { get; set; } = new Shipment();
    
    public Customer? Customer { get; set; } = new Customer();
    
    public string? CustomerReference { get; set; }

    public decimal? Confidence { get; set; }

    [MaxLength(4000)]
    public string? Reason { get; set; }

    // Persisted snapshot of missing information to keep all DTO data retrievable from DB.
    public string? MissingInformationJson { get; set; }

    [NotMapped]
    public FreightRequestValidationResult? ValidationResult { get; set; } = new FreightRequestValidationResult();

    public bool IsFreightRequest { get; set; }
    public bool? QuotationRequested { get; set; }

    public List<FreightPackage> Packages { get; set; } = [];

    /*public DateOnly? PickupDate { get; set; }
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
    
    public int? OriginTermId { get; set; }
    public TradeTerm? OriginTerm { get; set; }

    public int? DestinationTermId { get; set; }
    public TradeTerm? DestinationTerm { get; set; }
    public List<FreightPackage> Packages { get; set; } = [];
    */
    
}