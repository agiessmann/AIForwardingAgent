using System.Text.Json.Serialization;

namespace AiForwardingAgent.BusinessLogic.RouteLocationResolver;

public sealed class GeoNamesSearchResponse
{
    [JsonPropertyName("totalResultsCount")]
    public int TotalResultsCount { get; set; }

    [JsonPropertyName("geonames")]
    public List<GeoNamesPlace> GeoNames { get; set; } = [];
}