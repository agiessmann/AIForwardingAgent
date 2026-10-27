using AiForwardingAgent.BusinessLogic.CheckPostalCode;

namespace AiForwardingAgent.AgentServices;

public class LocationDto
{
    public string? Company { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ResolvedPostalCode { get; set; }

    public string? ResolvedCity { get; set; }
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public MatchQuality? PostalCodeMatchQuality { get; set; }
    public double? PostalCodeSimilarity { get; set; }
}