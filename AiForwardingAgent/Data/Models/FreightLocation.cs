using System.ComponentModel.DataAnnotations;
using AiForwardingAgent.BusinessLogic.CheckPostalCode;

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

    [MaxLength(100)]
    public string? ResolvedPostalCode { get; set; }

    [MaxLength(100)]
    public string? ResolvedCity { get; set; }

    [MaxLength(100)]
    public string? CountryCode { get; set; }

    [MaxLength(100)]
    public string? CountryName { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public MatchQuality? PostalCodeMatchQuality { get; set; }

    public double? PostalCodeSimilarity { get; set; }
}