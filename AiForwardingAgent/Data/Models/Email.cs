using System.ComponentModel.DataAnnotations;

namespace AiForwardingAgent.Data.Models;

public class Email
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(255)]
    public string? Subject { get; set; } = string.Empty;
    
    [Required , MaxLength(10000)]
    public string? Body { get; set; } = string.Empty;

    public int? MessageId { get; set; }

    [Required, MaxLength(255)]
    public string EmailSender { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string EmailRecipient { get; set; } = string.Empty;
}