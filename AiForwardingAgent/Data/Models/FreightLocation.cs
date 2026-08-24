using System.ComponentModel.DataAnnotations;

namespace AiForwardingAgent.Data.Models;

public class FreightLocation
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Company { get; set; }
    [MaxLength(100)]
    public string? Address { get; set; }
    [MaxLength(30)]
    public string? PostalCode { get; set; }
    [MaxLength(100)]
    public string? City { get; set; }
    [MaxLength(100)]
    public string? State { get; set; }
    [MaxLength(100)]
    public string? Country { get; set; }
}