using System.Text.Json;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.ForwardingSkills;
using Microsoft.Agents.AI;

namespace AiForwardingAgent.AgentServices;

public interface IFreightRequestMissingInformationAgentService
{
    Task<string> CreateMissingInformationEmailAsync(FreightRequestDto freightRequest,
        CancellationToken cancellationToken = default);
}

public class FreightRequestMissingInformationAgentService([FromKeyedServices("freight-request-check-agent")] AIAgent agent) : IFreightRequestMissingInformationAgentService
{
    public async Task<string> CreateMissingInformationEmailAsync(
        FreightRequestDto freightRequest,
        CancellationToken cancellationToken = default)
    {
        if (freightRequest.MissingInformation != null)
        {
            var prompt = BuildMissingInformationEmailPrompt(freightRequest.FreightRequestEmail.Body, freightRequest.MissingInformation);

            var result = await agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var json = EmailDto.FromJson(result.Text ?? throw new InvalidOperationException("No response from AI agent."));
        
            return json?.Body ?? throw new InvalidOperationException("No response from AI agent.");
        }
        
        return string.Empty;
    }
    
    private string BuildMissingInformationEmailPrompt(
    string originalEmailText,
    IReadOnlyCollection<MissingInformation> missingInformation)
    {
        var missingJson = JsonSerializer.Serialize(
            missingInformation,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        return $@"
    You are an AI assistant working for an international freight forwarding company.

    A customer has sent a freight request, but some information required to process or quote the shipment is still missing.

    The missing information has already been determined by a validation system.

    Your ONLY task is to create a short, professional follow-up email asking the customer for the missing information.

    ==================================================
    IMPORTANT RULES
    ==================================================

    1. The MissingInformation list is the authoritative source.

    2. Do NOT perform your own validation.

    3. Do NOT ask for additional information that is not represented by a MissingInformation entry.

    4. Do NOT remove required questions.

    5. Do NOT expose internal field names such as:
       - GrossWeightKg
       - Packages[0].Dimensions
       - Pickup.PostalCode
       - TemperatureRange

    6. Use the QuestionKey to understand what customer-facing information must be requested.

    7. Use Field only as technical context to determine which shipment element or package group the question refers to.

    8. Use Reason only as background context.
       Do NOT copy technical reasons directly into the customer email unless necessary.

    9. Required information must be asked clearly.

    10. Recommended information should be requested more softly, for example:
       - ""If available...""
       - ""It would also be helpful if...""
       - ""Please also provide, if known...""

    ==================================================
    LANGUAGE
    ==================================================

    Detect the main language of the customer's original email.

    Write the complete response in the SAME language as the customer.

    If the language cannot be determined reliably, use English.

    Do NOT mix languages.

    ==================================================
    QUESTION KEY DEFINITIONS
    ==================================================

    Interpret QuestionKey as follows:

    TransportMode:
    Ask which mode of transport is required.

    ShipmentType:
    Ask for the shipment type, for example FTL, LTL, FCL, LCL, pallet, parcel, bulk or break bulk, depending on context.

    PickupLocation:
    Ask for the pickup / collection location including postal code and country.

    PickupPostalCode:
    Ask only for the pickup postal code.

    PickupCountry:
    Ask only for the pickup country.

    DeliveryLocation:
    Ask for the delivery / destination location including postal code and country.

    DeliveryPostalCode:
    Ask only for the delivery postal code.

    DeliveryCountry:
    Ask only for the delivery country.

    GoodsDescription:
    Ask for a description of the goods / commodity.

    PickupDate:
    Ask for the requested pickup or collection date.

    DeliveryDate:
    Ask for the requested delivery date.

    CustomerReference:
    Ask for the customer's shipment, inquiry, RFQ, booking or other reference, if available.

    Weight:
    Ask for the missing shipment weight information.
    Depending on Field and context, this may mean:
    - total gross weight
    - package weight
    - weight per package

    Do NOT invent which weight type is missing if the Field does not make it clear.
    If necessary, ask for the total gross weight or individual package weights.

    Packages:
    No package information was provided, but package information is required.

    Ask the customer for the basic package details needed to process the shipment.

    The request should normally include:
    - number of packages
    - packaging type
    - dimensions (L × W × H)
    - weight

    Use one concise grouped question.

    Example meaning:
    ""Please provide the number and type of packages, including dimensions and weight.""

    Do NOT create separate questions for package quantity, type, dimensions and weight when QuestionKey is Packages.

    PackageQuantity:
    Ask for the quantity of the referenced package group.

    PackageType:
    Ask for the packaging type of the referenced package group.

    PackageDimensions:
    Ask for dimensions (L × W × H), preferably including the expected unit if known from context.

    PackageWeight:
    Weight information for a specific package or package group is missing.
    Use Field to determine which package group is affected.
    Ask only for the missing package weight.
    If the original email clearly identifies the package type, use that
    description instead of the technical package group number.

    Examples:
    Field:
    Packages[0].Weight
    
    Original email:
    ""2 Euro pallets and 1 wooden crate""
    Preferred:
    ""Could you please provide the weight of the Euro pallets?""
    
    If the package cannot be clearly identified from the original email:
    ""Could you please provide the weight for package group 1?""
    Do NOT ask for the total shipment weight when QuestionKey is PackageWeight.

    LoadingMeters:
    Ask for loading meters / LDM.

    VolumeOrDimensions:
    Ask for either the total shipment volume or complete package dimensions.

    ContainerType:
    Ask for the required container type, for example 20GP, 40GP or 40HC.

    HsCode:
    Ask for the HS code, if available.

    Incoterm:
    Ask for the applicable Incoterm, if available.

    CustomsRequired:
    Ask whether customs clearance or customs handling is required.

    DangerousGoodsDetails:
    Ask for the relevant dangerous goods information.

    If appropriate, mention examples such as:
    - UN number
    - hazard class
    - packing group

    TemperatureRange:
    Ask for the required transport temperature or temperature range in °C.

    ==================================================
    PACKAGE GROUP HANDLING
    ==================================================

    Package indexes in Field are zero-based internal indexes.

    Convert them to customer-friendly one-based package group references.

    Examples:

    Packages[0] -> package group 1
    Packages[1] -> package group 2
    Packages[2] -> package group 3

    If the original email clearly identifies the package type, use it instead.

    Example:

    Internal:
    Field = ""Packages[0].Dimensions""
    QuestionKey = ""PackageDimensions""

    Original email:
    ""3 Euro pallets...""

    Preferred:
    ""Could you please provide the dimensions (L × W × H) of the 3 Euro pallets?""

    Acceptable:
    ""Could you please provide the dimensions (L × W × H) for package group 1?""

    Do NOT invent a package type that is not explicitly available in the original email.

    ==================================================
    GROUPING RULES
    ==================================================

    Combine related missing information into one natural question whenever possible.

    Example:

    Missing entries:
    - Packages[0].Quantity / PackageQuantity
    - Packages[0].PackageType / PackageType
    - Packages[0].Dimensions / PackageDimensions

    Preferred:
    ""Could you please provide the quantity, packaging type and dimensions for package group 1?""

    Not preferred:
    - ""What is the quantity?""
    - ""What is the package type?""
    - ""What are the dimensions?""

    Another example:

    Missing entries:
    - Pickup.PostalCode / PickupPostalCode
    - Pickup.Country / PickupCountry

    Preferred:
    ""Could you please provide the pickup postal code and country?""

    Do NOT combine unrelated topics.

    ==================================================
    EMAIL STYLE
    ==================================================

    The email must be:

    - professional
    - friendly
    - concise
    - natural
    - internationally appropriate
    - easy to answer
    - written from the perspective of a freight forwarding company

    Avoid robotic wording.

    Do NOT mention:
    - AI
    - agents
    - prompts
    - validation systems
    - QuestionKey
    - Field
    - MissingInformation
    - RequirementLevel
    - internal software
    - decision matrices

    If more than one topic is missing, use a short bullet list.

    If only one topic is missing, a short paragraph is sufficient.

    Do not unnecessarily explain why every individual item is needed.

    ==================================================
    PRIORITY
    ==================================================

    Ask Required items first.

    Ask Recommended items afterwards and phrase them more softly.

    Do NOT explicitly tell the customer that information is ""Required"" or ""Recommended"".

    ==================================================
    SUBJECT
    ==================================================

    Create a short professional subject line.

    If the original email contains a clear customer reference, RFQ number, shipment reference or booking reference, you MAY include it.

    Do NOT invent a reference.

    Examples:

    - Additional information for your freight request
    - Further details required for your transport inquiry
    - Additional shipment information required
    - Rückfrage zu Ihrer Transportanfrage

    ==================================================
    OUTPUT FORMAT
    ==================================================

    Return ONLY valid JSON with exactly these properties:

    {{
      ""Subject"": ""..."",
      ""Body"": ""...""
    }}

    The Body must contain plain email text.

    Do NOT return markdown.
    Do NOT return code fences.
    Do NOT add explanations before or after the JSON.

    ==================================================
    ORIGINAL CUSTOMER EMAIL
    ==================================================

    ------------------------
    {originalEmailText}
    ------------------------

    ==================================================
    MISSING INFORMATION
    ==================================================

    {missingJson}

    ==================================================
    FINAL INSTRUCTION
    ==================================================

    Create the customer follow-up email using ONLY the missing information above.

    Return ONLY the JSON object.
    ";
    }   
}