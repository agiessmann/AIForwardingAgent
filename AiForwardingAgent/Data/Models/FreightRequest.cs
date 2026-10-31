using AiForwardingAgent.AgentServices.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiForwardingAgent.Data.Models;

public class FreightRequest
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public FreightRequestStatus Status { get; set; } = FreightRequestStatus.New;

    public List<Email>? FreightRequestEmailHistory { get; set; } = [];
    
    public Shipment? Shipment { get; set; } = new Shipment();
    
    public Customer? Customer { get; set; } = new Customer();
    
    public string? CustomerReference { get; set; }

    public decimal? Confidence { get; set; }

    [MaxLength(4000)]
    public string? Reason { get; set; }

    // Persisted snapshot of missing information to keep all DTO data retrievable from DB.
    public string? MissingInformationJson { get; set; }

    // Question keys the customer explicitly declined to answer (e.g. "HS code not available").
    public string? DeclinedQuestionKeysJson { get; set; }

    [NotMapped]
    public FreightRequestValidationResult? ValidationResult { get; set; } = new FreightRequestValidationResult();

    public bool IsFreightRequest { get; set; }
    public bool? QuotationRequested { get; set; }

    public List<FreightPackage> Packages { get; set; } = [];

    
    
}