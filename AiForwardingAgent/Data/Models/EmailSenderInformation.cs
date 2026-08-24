using System.ComponentModel.DataAnnotations;

namespace AiForwardingAgent.Data.Models;

public class EmailSenderInformation
{
   [Key]
   public int EmailSenderInformationId { get; set; }
   
   [EmailAddress]
   [MaxLength(100)]
   public string?  EmailAddress { get; set; }
   
   [MaxLength(100)]
   public string? Name { get; set; }
   
    [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Customer auswählen.")]
   public int CustomerId { get; set; }

   public Customer Customer { get; set; } = null!;

}