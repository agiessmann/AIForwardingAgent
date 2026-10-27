using System.Text.Json;

namespace AiForwardingAgent.AgentServices;

public class EmailDto
{
    public string? Subject { get; set; } = string.Empty;
    public string? Body { get; set; } = string.Empty;
    public int? MessageId { get; set; }

    public string EmailSender { get; set; } = string.Empty;
    
    public string EmailRecipient { get; set; } = string.Empty;
    
    private static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public static EmailDto? FromJson(string json)
    {
        return JsonSerializer.Deserialize<EmailDto>(json, Options);
    }
}