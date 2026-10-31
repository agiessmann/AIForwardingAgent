using System.Text.Json;
using System.Text.Json.Serialization;
using AiForwardingAgent.AgentServices.Enums;
using AiForwardingAgent.AgentServices.Models;
using Microsoft.Agents.AI;

namespace AiForwardingAgent.AgentServices;

public interface IFreightRequestReplyAnalysisAgent
{
    Task<FreightRequestReplyAnalysisResult> AnalyzeFreightRequestReplyAsync(
        FreightRequestDto freightRequest,
        CancellationToken cancellationToken = default);
}

public class FreightRequestReplyAnalysisAgent([FromKeyedServices("freight-request-check-agent")] AIAgent agent) : IFreightRequestReplyAnalysisAgent
{
    
    public async Task<FreightRequestReplyAnalysisResult> AnalyzeFreightRequestReplyAsync(
            FreightRequestDto freightRequest,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(freightRequest);
        
        var prompt = BuildAnalyzeFreightRequestReplyPrompt(freightRequest);

        var result = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

        var json = result.Text ?? throw new InvalidOperationException("No response from AI agent.");

        var analysis = JsonSerializer.Deserialize<FreightRequestReplyAnalysisResult>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return analysis ?? throw new InvalidOperationException("Could not deserialize freight reply analysis.");
    }
    
    private static string BuildAnalyzeFreightRequestReplyPrompt(
    FreightRequestDto freightRequest)
{
    var history = (freightRequest.FreightRequestEmailHistory ?? [])
        .OrderBy(x => x.SentReceivedDate)
        .ThenBy(x => x.MessageId)
        .ToList();

    var latestReply = history
        .LastOrDefault(x =>
            x.Direction == EmailDirection.Incoming &&
            x.EmailType == FreightRequestEmailType.CustomerReply);

    if (latestReply is null)
    {
        throw new InvalidOperationException(
            "No customer reply found in email history.");
    }

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    var shipmentJson = JsonSerializer.Serialize(
        freightRequest.ShipmentInformation,
        options);

    var missingInformationJson = JsonSerializer.Serialize(
        freightRequest.MissingInformation,
        options);

    var historyJson = JsonSerializer.Serialize(
        history,
        options);

    var latestReplyJson = JsonSerializer.Serialize(
        latestReply,
        options);

    return $$"""
    You are an AI assistant specialized in freight forwarding.

    Your task is to analyze the latest customer reply
    in an existing freight request conversation.

    The freight request has already been analyzed.
    Shipment information has already been extracted.
    Missing information has already been identified.

    Your task is NOT to analyze the freight request from scratch.

    Your task is to identify NEW information, corrections
    and clarifications provided in the latest customer reply.

    ==================================================
    EMAIL HISTORY
    ==================================================

    The email history contains different types of messages:

    OriginalEmail:
    The original freight request received from the customer.

    MissingInformationRequest:
    An outgoing email asking the customer for missing information.

    CustomerReply:
    An incoming email containing the customer's response.

    Each email also contains:

    - EmailType
    - Direction
    - SentReceivedDate
    - Subject
    - Body

    Use the complete email history to understand the conversation.

    However, extract NEW information ONLY from the latest
    customer reply.

    Earlier emails are CONTEXT ONLY.

    Do NOT extract information from earlier emails
    as new updates.

    Do NOT treat quoted email history as newly provided information.

    ==================================================
    CURRENT SHIPMENT INFORMATION
    ==================================================

    ShipmentInformation represents the current state
    of the freight request stored in the system.

    It contains information that has already been extracted
    and processed.

    Do NOT return existing information again.

    Do NOT overwrite existing information unless the latest
    customer reply explicitly corrects or replaces it.

    ==================================================
    MISSING INFORMATION
    ==================================================

    MissingInformation contains the currently missing
    information required to process the freight request.

    Each entry contains:

    - Field
    - Level
    - Reason
    - QuestionKey

    Use MissingInformation to understand what information
    is still required.

    The customer may answer:

    - All missing questions
    - Only some missing questions
    - Questions from earlier emails
    - Questions in a different order
    - Multiple questions in one sentence
    - Additional questions that were not explicitly asked

    Extract all relevant NEW information.

    Do NOT assume that every missing question has been answered.

    ==================================================
    CONTEXTUAL ANSWERS
    ==================================================

    The customer may provide short answers such as:

    "Yes"
    "No"
    "Correct"
    "That is fine"
    "850 kg"
    "Tomorrow"
    "Both pallets"

    Use the email history to determine what these answers
    refer to.

    Example:

    Previous outgoing email:
    "Are the pallets stackable?"

    Latest customer reply:
    "No."

    Result:
    Stackable = false

    Only map short answers when the reference is unambiguous.

    If the answer could refer to multiple questions,
    do NOT guess.

    ==================================================
    FIELD UPDATES
    ==================================================

    Return only fields that contain new or explicitly
    corrected information.

    Use exact ShipmentDto property names.

    Examples:

    TransportMode
    ShipmentType
    Pickup.PostalCode
    Pickup.City
    Pickup.Country
    Delivery.PostalCode
    Delivery.City
    Delivery.Country
    OriginTerm.Incoterm
    OriginTerm.NamedPlace
    DestinationTerm.Incoterm
    DestinationTerm.NamedPlace
    PickupDate
    DeliveryDate
    GoodsDescription
    GrossWeightKg
    VolumeM3
    LoadingMeters
    Packages[0].Quantity
    Packages[0].PackageType
    Packages[0].LengthCm
    Packages[0].WidthCm
    Packages[0].HeightCm
    Packages[0].WeightPerPackageKg
    Packages[0].TotalWeightKg
    ContainerType
    DangerousGoods
    DangerousGoodsDetails
    TemperatureControlled
    MinimumTemperatureCelsius
    MaximumTemperatureCelsius
    Stackable
    CustomsRequired
    HsCode
    CustomerReference
    SpecialInstructions

    Do NOT invent field names.

    ==================================================
    PACKAGE INFORMATION
    ==================================================

    Package indexes are zero-based.

    Use the existing ShipmentInformation.Packages
    collection to identify the correct package group.

    Example:

    Existing:

    Packages[0]:
    Quantity = 2
    PackageType = Euro pallet

    Packages[1]:
    Quantity = 1
    PackageType = Wooden crate

    Latest customer reply:
    "The wooden crate weighs 850 kg."

    Update:
    Packages[1].TotalWeightKg = 850

    Do NOT invent package indexes.

    Do NOT assign package information to an existing
    package group if the mapping is ambiguous.

    ==================================================
    WEIGHT INFORMATION
    ==================================================

    Distinguish between:

    GrossWeightKg:
    Total gross weight of the entire shipment.

    Packages[i].WeightPerPackageKg:
    Weight of one individual package.

    Packages[i].TotalWeightKg:
    Total weight of the package group.

    Examples:

    "The shipment weighs 850 kg."
    -> GrossWeightKg = 850

    "Each pallet weighs 400 kg."
    -> Packages[i].WeightPerPackageKg = 400

    "Both pallets together weigh 800 kg."
    -> Packages[i].TotalWeightKg = 800

    Do NOT calculate total weights.

    Do NOT assume package weight equals shipment weight.

    ==================================================
    TRADE TERMS
    ==================================================

    OriginTerm and DestinationTerm represent commercial
    transport boundaries.

    Example:

    "FOB Rotterdam to CFR New York"

    OriginTerm.Incoterm = FOB
    OriginTerm.NamedPlace = Rotterdam

    DestinationTerm.Incoterm = CFR
    DestinationTerm.NamedPlace = New York

    Do NOT infer Incoterms.

    ==================================================
    ANSWERED QUESTIONS
    ==================================================

    Compare the latest customer reply with MissingInformation.

    For each missing item, determine whether the customer
    has provided an unambiguous answer.

    If answered, include its original Field and QuestionKey
    in AnsweredQuestions.

    If not answered, include the Field and Reason
    in UnresolvedInformation.

    Do NOT consider a question answered merely because
    the customer mentioned related information.

    ==================================================
    DECLINED QUESTIONS
    ==================================================

    If the customer explicitly states that requested information
    is not available, unknown or will not be provided
    (for example "the HS code is not available"),
    include the original Field and QuestionKey
    in DeclinedQuestions.

    Do NOT put a placeholder value into Updates for it.

    Do NOT list it in UnresolvedInformation.

    ==================================================
    CONFLICTS AND CORRECTIONS
    ==================================================

    Whenever an Update replaces an existing, non-empty value
    in ShipmentInformation, the same Field MUST also be reported
    in Conflicts. Otherwise the update will be rejected.

    If the customer gives a new pickup location that differs
    from the stored one (for example "pickup is in 21266 Jesteburg"
    while Pickup.City is Hamburg), update Pickup.PostalCode AND
    Pickup.City and report both fields in Conflicts.

    If the customer explicitly corrects an existing value,
    include the corrected value in Updates.

    Also report the correction in Conflicts.

    Example:

    Existing:
    Pickup.City = Hamburg

    Latest reply:
    "Correction: pickup is in Bremen."

    Update:
    Pickup.City = Bremen

    Conflict:
    Field = Pickup.City
    ExistingValue = Hamburg
    NewValue = Bremen
    Reason = The customer explicitly corrected the pickup city.
    
    Every conflict must contain Field, ExistingValue, NewValue and Reason.

    If the intended correction is unclear,
    do NOT select a value.

    Set RequiresManualReview = true.

    ==================================================
    EXTRACTION RULES
    ==================================================

    Extract only explicitly stated information.

    Do NOT guess.

    Do NOT calculate.

    Do NOT perform unit conversions.

    Do NOT infer missing information from general
    freight knowledge.

    Do NOT normalize or reinterpret customer statements
    beyond what is required for exact field mapping.

    Boolean values must be true or false only when
    explicitly stated or unambiguously answered.

    Dates must only be extracted when they can be
    represented as an unambiguous calendar date.

    Do NOT invent missing years.

    ==================================================
    CONFIDENCE
    ==================================================

    Confidence must be between 0.0 and 1.0.

    Use high confidence only for explicitly stated
    and unambiguously mapped information.

    If information is ambiguous, report it in
    UnresolvedInformation instead of Updates.

    ==================================================
    OUTPUT FORMAT
    ==================================================

    Return ONLY valid JSON.

    Do NOT include Markdown.

    Do NOT include explanations outside JSON.

    Use exactly this structure:

    {
      "IsRelevantReply": true,
      "RequiresManualReview": false,
      "Updates": [
        {
          "Field": "Packages[0].TotalWeightKg",
          "Value": "850",
          "SourceText": "Die Kiste wiegt 850 kg.",
          "Confidence": 1.0
        }
      ],
      "AnsweredQuestions": [
        {
          "Field": "Packages[0].Weight",
          "QuestionKey": "PackageWeight"
        }
      ],
      "DeclinedQuestions": [],
      "UnresolvedInformation": [],
      "Conflicts": [
        {
          "Field": "Pickup.City",
          "ExistingValue": "Hamburg",
          "NewValue": "Jesteburg",
          "Reason": "The customer corrected the pickup location."
        }
      ],
      "Reason": "The customer provided the missing package weight."
    }

    ==================================================
    CURRENT SHIPMENT INFORMATION
    ==================================================

    <shipment_information>
    {{shipmentJson}}
    </shipment_information>

    ==================================================
    CURRENT MISSING INFORMATION
    ==================================================

    <missing_information>
    {{missingInformationJson}}
    </missing_information>

    ==================================================
    COMPLETE EMAIL HISTORY
    ==================================================

    <email_history>
    {{historyJson}}
    </email_history>

    ==================================================
    LATEST CUSTOMER REPLY
    ==================================================

    <latest_customer_reply>
    {{latestReplyJson}}
    </latest_customer_reply>

    ==================================================
    FINAL INSTRUCTIONS
    ==================================================

    Analyze ONLY the latest customer reply for new information.

    Use the previous email history for context.

    Identify new information and explicit corrections.

    Match answers to MissingInformation.

    Return only the JSON analysis result.

    """;
}
}