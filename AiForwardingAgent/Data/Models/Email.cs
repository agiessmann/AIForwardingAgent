using System.ComponentModel.DataAnnotations;
using AiForwardingAgent.AgentServices.Enums;

namespace AiForwardingAgent.Data.Models;

public class Email
{
    [Key]
    public int Id { get; set; }
    
    public int? FreightRequestId { get; set; }
    
    [Required, MaxLength(255)]
    public string? Subject { get; set; } = string.Empty;
    
    [Required , MaxLength(10000)]
    public string? Body { get; set; } = string.Empty;

    public int? MessageId { get; set; }

    [Required, MaxLength(255)]
    public string EmailSender { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string EmailRecipient { get; set; } = string.Empty;
    
    public FreightRequestEmailType EmailType { get; set; } = FreightRequestEmailType.OriginalEmail;
    
    public DateTime? SentReceivedDate { get; set; }
    
    public EmailDirection Direction { get; set; } = EmailDirection.Incoming;
}