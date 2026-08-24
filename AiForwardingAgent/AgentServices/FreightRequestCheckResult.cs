using System.Text.Json;

namespace AiForwardingAgent.AgentServices;

public class FreightRequestCheckResult

{
    public bool IsFreightRequest { get; init; }
    public decimal Confidence { get; init; }
    public string? Reason { get; init; }

    public string? Subject { get; set; } = string.Empty;

    public string? EmailText { get; set; } = string.Empty;
    
    public string? MessageId { get; set; } = string.Empty;
    private static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public static FreightRequestCheckResult? FromJson(string json)
    {
        return JsonSerializer.Deserialize<FreightRequestCheckResult>(json, Options);
    }
}