using System.ComponentModel.DataAnnotations;
namespace AiForwardingAgent.Data.Models;

public class Customer
{
    [Key]
    public int CustomerId { get; set; }
    
    [MaxLength(100)]
    public string? CustomerName { get; set; }
    
    [MaxLength(100)]
    public string? CustomerCode { get; set; }
    
    [MaxLength(100)]
    public string? CustomerEmail { get; set; }
    
    [MaxLength(100)]
    public string? CustomerPhone { get; set; }
    
    [MaxLength(200)]
    public string? CustomerAddress { get; set; }
    
    [MaxLength(100)]
    public string? CustomerCity { get; set; }
    
    [MaxLength(100)]
    public string? CustomerCountry { get; set; }

    public ICollection<EmailSenderInformation> EmailSenderInformations { get; set; } = new List<EmailSenderInformation>();
}