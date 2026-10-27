namespace AiForwardingAgent.BusinessLogic.CheckPostalCode;

using System.Text.Json.Serialization;

internal sealed class GeoNamesPostalCodeResponse
{
    [JsonPropertyName("postalCodes")]
    public List<GeoNamesPostalCode> PostalCodes { get; set; } = [];
}

internal sealed class GeoNamesPostalCode
{
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("placeName")]
    public string? PlaceName { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("adminName1")]
    public string? AdminName1 { get; set; }

    [JsonPropertyName("lat")]
    public double? Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double? Longitude { get; set; }
}