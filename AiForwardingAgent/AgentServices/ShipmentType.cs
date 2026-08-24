using System.Text.Json.Serialization;

namespace AiForwardingAgent.AgentServices;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShipmentType
{
    Ftl,
    Ltl,
    Fcl,
    Lcl,
    Parcel,
    Pallet,
    Bulk,
    BreakBulk,
    Unknown
}