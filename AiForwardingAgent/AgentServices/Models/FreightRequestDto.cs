using System.Text.Json;
using AiForwardingAgent.Data.Models;

namespace AiForwardingAgent.AgentServices.Models;   

public class FreightRequestDto
{
    public int? FreightRequestId { get; set; }
    
    public FreightRequestStatus Status { get; set; } = FreightRequestStatus.New;
    
    public List<EmailDto>? FreightRequestEmailHistory { get; set; } = [];

    public string? CustomerReference { get; set; }

    public CustomerDto? Customer { get; set; }
    public bool IsFreightRequest { get; set; }
    public bool? QuotationRequested { get; set; }
    public decimal Confidence { get; set; }
    public string? Reason { get; set; }
    public ShipmentDto? ShipmentInformation { get; set; } = new ShipmentDto();
    
    public List<MissingInformation>? MissingInformation { get; set; } = [];
    
    public List<string>? DeclinedQuestionKeys { get; set; } = [];

    public bool RequiresManualReview { get; set; }
    public string? ManualReviewReason { get; set; }
    private static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public static FreightRequestDto? FromJson(string json)
    {
        return JsonSerializer.Deserialize<FreightRequestDto>(json, Options);
    }
    

    
        
}