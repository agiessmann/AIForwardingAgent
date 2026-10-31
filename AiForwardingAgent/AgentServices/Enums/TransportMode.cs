using System.Text.Json.Serialization;

namespace AiForwardingAgent.AgentServices;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransportMode
{
    Road,
    Air,
    Ocean,
    Rail,
    Courier,
    Express,
    Multimodal,
    Unknown
}