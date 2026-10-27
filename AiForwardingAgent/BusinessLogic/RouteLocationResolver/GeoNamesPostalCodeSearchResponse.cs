using System.Text.Json.Serialization;
using AiForwardingAgent.BusinessLogic.CheckPostalCode;

namespace AiForwardingAgent.BusinessLogic.RouteLocationResolver;

public sealed class GeoNamesPostalCodeSearchResponse
{
    [JsonPropertyName("postalCodes")]
    public List<GeoNamesPostalCode> PostalCodes { get; set; } = [];
}

public sealed class GeoNamesPostalCode
{
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("placeName")]
    public string? PlaceName { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("adminName1")]
    public string? AdminName1 { get; set; }

    [JsonPropertyName("adminCode1")]
    public string? AdminCode1 { get; set; }

    [JsonPropertyName("adminName2")]
    public string? AdminName2 { get; set; }

    [JsonPropertyName("adminCode2")]
    public string? AdminCode2 { get; set; }

    [JsonPropertyName("lat")]
    public double? Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double? Longitude { get; set; }
}