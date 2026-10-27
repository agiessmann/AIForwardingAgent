using System.Text.Json.Serialization;

namespace AiForwardingAgent.BusinessLogic.RouteLocationResolver;

public sealed class GeoNamesPlace
{
    [JsonPropertyName("geonameId")]
    public long GeoNameId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("toponymName")]
    public string? ToponymName { get; set; }

    [JsonPropertyName("countryName")]
    public string? CountryName { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("adminName1")]
    public string? AdminName1 { get; set; }

    [JsonPropertyName("population")]
    public long? Population { get; set; }

    [JsonPropertyName("lat")]
    public string? Latitude { get; set; }

    [JsonPropertyName("lng")]
    public string? Longitude { get; set; }

    [JsonPropertyName("fcl")]
    public string? FeatureClass { get; set; }

    [JsonPropertyName("fcode")]
    public string? FeatureCode { get; set; }
}