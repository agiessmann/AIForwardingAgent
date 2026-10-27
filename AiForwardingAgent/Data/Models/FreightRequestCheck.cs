using System.ComponentModel.DataAnnotations;

namespace AiForwardingAgent.Data.Models;

public class FreightRequestCheck
{
    [Key]
    public int Id { get; set; }
    public bool IsFreightRequest { get; init; }
    public decimal Confidence { get; init; }
    
    [MaxLength(1000)]
    public string? Reason { get; init; }

    [MaxLength(200)]
    public string? Subject { get; set; } = string.Empty;

    public string? EmailText { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? MessageId { get; set; } = string.Empty;
}