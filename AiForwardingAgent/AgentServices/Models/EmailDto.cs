using System.Text.Json;
using AiForwardingAgent.AgentServices.Enums;

namespace AiForwardingAgent.AgentServices;

public class EmailDto
{
    public string? Subject { get; set; } = string.Empty;
    public string? Body { get; set; } = string.Empty;
    public int? MessageId { get; set; }

    public string EmailSender { get; set; } = string.Empty;
    
    public string EmailRecipient { get; set; } = string.Empty;
    
    public FreightRequestEmailType EmailType { get; set; } = FreightRequestEmailType.OriginalEmail;
    
    public DateTimeOffset? SentReceivedDate { get; set; }
    
    public EmailDirection Direction { get; set; } = EmailDirection.Incoming;
    
    private static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public static EmailDto? FromJson(string json)
    {
        return JsonSerializer.Deserialize<EmailDto>(json, Options);
    }
}