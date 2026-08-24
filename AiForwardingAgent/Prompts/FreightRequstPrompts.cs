namespace AiForwardingAgent.Prompts;

public class FreightRequstPrompts
{
    private static string BuildFreightRequestPrompt(string emailText)
    {
    return $@"
        You are an AI assistant working for an international freight forwarding company.

        Your task consists of TWO steps:

        STEP 1 - Classification
        Determine whether the email is a NEW freight transportation request.

        A freight request is an email where the sender requests:
        - a freight quotation
        - a transport quotation
        - a shipment
        - a pickup
        - a delivery
        - freight rates
        - booking of cargo
        - transport capacity
        - shipping services

        Typical transport modes:
        - Road Freight
        - Air Freight
        - Ocean Freight
        - Rail Freight
        - Courier / Express
        - Multimodal

        Examples of freight requests:
        - ""Please quote for the following shipment.""
        - ""Can you arrange transport from Hamburg to Milan?""
        - ""Please provide your best rate.""
        - ""We need a truck tomorrow.""
        - ""Request for quotation.""
        - ""RFQ""
        - ""Freight inquiry""

        The following are NOT freight requests:
        - shipment tracking
        - complaints
        - invoices
        - payment reminders
        - job applications
        - newsletters
        - marketing emails
        - internal discussions
        - customs documents without a transport request
        - replies that do not contain a new transport request

        STEP 2 - Information Extraction

        If and ONLY IF the email is classified as a freight request, extract every transport detail that is explicitly mentioned.

        Extraction Rules (MUST FOLLOW)

        1. Extract ONLY information explicitly written in the email.
        2. NEVER guess or infer missing values.
        3. Use null for unknown values.
        4. Do not calculate weights or volumes.
        5. Do not translate values.
        6. Keep addresses exactly as written.
        7. Return ONLY valid JSON.
        8. No markdown.
        9. No explanation.

        Return this JSON structure:

        {{
          ""IsFreightRequest"": true,
          ""Confidence"": 0.97,
          ""Reason"": ""Customer requests a quotation for a shipment from Hamburg to Milan."",

          ""TransportMode"": ""Road"",
          ""QuotationRequested"": true,

          ""PickupLocation"": ""Hamburg, Germany"",
          ""DeliveryLocation"": ""Milan, Italy"",

          ""PickupDate"": null,
          ""DeliveryDate"": null,

          ""Incoterm"": null,

          ""GoodsDescription"": ""Machine parts"",
          ""Packages"": 4,
          ""PackageType"": ""Pallet"",
          ""WeightKg"": 850,
          ""VolumeM3"": null,

          ""ContainerType"": null,
          ""DangerousGoods"": false,
          ""TemperatureControlled"": false,

          ""CustomerReference"": null,

          ""ContactPerson"": null,
          ""ContactEmail"": null,
          ""ContactPhone"": null
        }}

        If the email is NOT a freight request, return:

        {{
          ""IsFreightRequest"": false,
          ""Confidence"": 0.99,
          ""Reason"": ""This email is only asking for shipment status."",

          ""TransportMode"": null,
          ""QuotationRequested"": null,

          ""PickupLocation"": null,
          ""DeliveryLocation"": null,

          ""PickupDate"": null,
          ""DeliveryDate"": null,

          ""Incoterm"": null,

          ""GoodsDescription"": null,
          ""Packages"": null,
          ""PackageType"": null,
          ""WeightKg"": null,
          ""VolumeM3"": null,

          ""ContainerType"": null,
          ""DangerousGoods"": null,
          ""TemperatureControlled"": null,

          ""CustomerReference"": null,

          ""ContactPerson"": null,
          ""ContactEmail"": null,
          ""ContactPhone"": null
        }}

        Email:

        ------------------------
        {emailText}
        ------------------------

        Return ONLY the JSON object.
        ";
    }
    
    private string BuildIsFreightRequestPrompt(string emailText)
    {
    return $@"
        You are an AI assistant working for an international freight forwarding company.

        Determine whether the email is a NEW freight transportation request.

        A freight request is an email where the sender requests:
        - a freight quotation
        - a transport quotation
        - a shipment
        - a pickup
        - a delivery
        - freight rates
        - booking of cargo
        - transport capacity
        - shipping services

        Typical transport modes:
        - Road Freight
        - Air Freight
        - Ocean Freight
        - Rail Freight
        - Courier / Express
        - Multimodal

        Examples of freight requests:
        - ""Please quote for the following shipment.""
        - ""Can you arrange transport from Hamburg to Milan?""
        - ""Please provide your best rate.""
        - ""We need a truck tomorrow.""
        - ""Request for quotation.""
        - ""RFQ""
        - ""Freight inquiry""

        The following are NOT freight requests:
        - shipment tracking
        - complaints
        - invoices
        - payment reminders
        - job applications
        - newsletters
        - marketing emails
        - internal discussions
        - customs documents without a transport request
        - replies that do not contain a new transport request

        Return this JSON structure:

        {{
          ""IsFreightRequest"": true,
          ""Confidence"": 0.97,
          ""Reason"": ""Customer requests a quotation for a shipment from Hamburg to Milan."",

        }}

        If the email is NOT a freight request, return:

        {{
          ""IsFreightRequest"": false,
          ""Confidence"": 0.99,
          ""Reason"": ""This email is only asking for shipment status."",
       }}

        Email:

        ------------------------
        {emailText}
        ------------------------

        Return ONLY the JSON object.
        ";
    }
    
    private string BuildExtractFreightRequestDetailsPrompt(string emailText)
{
    return $@"
    You are an AI assistant working for an international freight forwarding company.
    The email has already been classified as a freight request.

    Your ONLY task is to extract shipment information from the email and return it as a JSON object that exactly matches the required schema below.

    The JSON response will be deserialized directly into C# classes.
    Therefore, property names, nesting, data types, enum values, null handling, and date formats MUST strictly follow the specification.

    ==================================================
    GENERAL EXTRACTION RULES
    ==================================================

    1. Extract ONLY information explicitly present in the email.

    2. NEVER guess, assume, infer, calculate, derive, complete, normalize, or invent missing information.

    3. If a value is not explicitly available, return null.

    4. For nullable boolean values:
     - return true ONLY if explicitly stated or clearly expressed in the email.
     - return false ONLY if explicitly stated or clearly expressed as false.
     - otherwise return null.

   Example:
   If dangerous goods are not mentioned:
   ""DangerousGoods"": null

   NOT:
   ""DangerousGoods"": false

   5. Do NOT perform calculations.

   This includes:
   - quantity × unit weight
   - total shipment weight
   - package volume
   - shipment volume
   - loading meters
   - dimensional weight
   - temperature conversions
   - unit conversions

   6. Do NOT convert units.

    Populate numeric unit-specific fields ONLY if the email explicitly provides the value in the required unit.

    Examples:
    - GrossWeightKg requires kilograms.
    - WeightPerPackageKg requires kilograms.
    - LengthCm, WidthCm, HeightCm require centimeters.
    - VolumeM3 requires cubic meters.
    - LoadingMeters requires loading meters.
    - temperature fields require degrees Celsius.

    If a value is only given in another unit, return null for the normalized numeric field.

    7. Do NOT translate free-text values.

    Keep values such as:
    - GoodsDescription
    - PackageType
    - SpecialInstructions
    - Company
    - Address
    - CustomerReference
    - DangerousGoodsDetails

    as close as possible to the original wording.

    8. Return ONLY valid JSON.

    9. Do NOT use markdown.

    10. Do NOT return ```json code fences.

    11. Do NOT return comments or explanations.

    12. Every property defined in the required JSON structure MUST be present.

    ==================================================
    TRANSPORT MODE
    ==================================================

    TransportMode may ONLY contain one of these values:

    - ""Road""
    - ""Air""
    - ""Ocean""
    - ""Rail""
    - ""Courier""
    - ""Express""
    - ""Multimodal""

    If the transport mode is not explicitly identifiable, return null.

    Typical explicit mappings:

    - truck / trucking / road freight -> ""Road""
    - air freight / air cargo -> ""Air""
   - ocean freight / sea freight -> ""Ocean""
   - rail freight -> ""Rail""
   - courier -> ""Courier""
   - express shipment / express transport -> ""Express""
   - explicitly combined transport modes -> ""Multimodal""

    Do NOT determine the mode based only on origin, destination, distance, container type, or general shipping context.

    ==================================================
    SHIPMENT TYPE
    ==================================================

    ShipmentType may ONLY contain one of these values:

    - ""Ftl""
    - ""Ltl""
    - ""Fcl""
    - ""Lcl""
    - ""Parcel""
    - ""Pallet""
    - ""Bulk""
    - ""BreakBulk""

Typical mappings:

    - FTL / full truck load -> ""Ftl""
    - LTL / less than truck load -> ""Ltl""
    - FCL / full container load -> ""Fcl""
    - LCL / less than container load -> ""Lcl""
    - parcel / package shipment -> ""Parcel""
    - pallet shipment -> ""Pallet""
    - bulk cargo -> ""Bulk""
    - break bulk -> ""BreakBulk""

    If no shipment type is explicitly identifiable, return null.

    ==================================================
    LOCATIONS
    ==================================================

    Pickup represents:
    - pickup location
    - collection location
    - loading address
    - origin
    - shipper location, ONLY if explicitly identified as the pickup location

    Delivery represents:
    - delivery location
    - unloading address
    - destination
    - consignee location, ONLY if explicitly identified as the delivery location

    Do NOT use addresses from email signatures unless the email explicitly states that the address is the pickup or delivery location.

    Do NOT infer Country from:
    - city
    - postal code
    - company name
    - telephone number
    - email domain

    If only part of a location is available, populate only those fields.

    ==================================================
    DATES
    ==================================================

    PickupDate and DeliveryDate MUST use this format:

    yyyy-MM-dd

    Only populate a date if the actual calendar date is explicitly available.

    Do NOT resolve relative dates such as:

    - tomorrow
    - next Monday
    - next week
    - Friday
    - ASAP
    - end of week

    unless the corresponding explicit calendar date is also present.

    ==================================================
    PACKAGES
    ==================================================

    Packages contains one object for each distinct package group described in the email.

    A package group consists of packages that share the same explicitly stated characteristics.

    Create separate PackageInformation objects when packages differ in one or more of these attributes:

    - PackageType
    - dimensions
    - weight
    - goods description
    - stackability

    Example:

    Email:
    ""3 pallets 120x80x150 cm, 250 kg each
    2 crates 200x100x90 cm, total weight 600 kg""

    Packages must contain TWO entries.

    Do NOT merge distinct package groups.

    Do NOT create one package entry per individual package if the email provides a grouped quantity.

    Example:

    ""10 pallets 120x80x100 cm""

    Return ONE PackageInformation object with:

    ""Quantity"": 10

    Do NOT return ten objects.

    ==================================================
    PACKAGE QUANTITY
    ==================================================

    Quantity is the explicitly stated number of identical packages in this package group.

    Examples:

    ""3 pallets"" -> 3
    ""10 cartons"" -> 10
    ""1 crate"" -> 1

    If no quantity is stated, return null.

    Do NOT calculate a quantity from other information.

    ==================================================
    PACKAGE TYPE
    ==================================================

    PackageType contains the explicitly stated packaging type.

    Examples:

    - pallet
    - Euro pallet
    - carton
    - box
    - crate
    - drum
    - bag
    - skid

    Preserve the original wording where possible.

    ==================================================
    PACKAGE DIMENSIONS
    ==================================================

    LengthCm, WidthCm, and HeightCm represent the dimensions of ONE package in centimeters.

    Only populate them if:

    1. all dimensions are clearly associated with the package group, and
    2. the unit is explicitly centimeters or cm.

    Example:

    ""120 x 80 x 150 cm""

    Return:

    ""LengthCm"": 120
    ""WidthCm"": 80
    ""HeightCm"": 150

    Interpret dimensions in the order:

    Length x Width x Height

    ONLY when the email clearly presents them as a conventional three-dimensional package measurement.

    If the email provides dimensions in mm, m, inch, feet, or another unit:
    return null for LengthCm, WidthCm, and HeightCm.

    Do NOT convert dimensions.

    ==================================================
    PACKAGE WEIGHT
    ==================================================

    WeightPerPackageKg means the weight of ONE individual package.

    Example:

    ""3 pallets, 250 kg each""

    Return:

    ""Quantity"": 3
    ""WeightPerPackageKg"": 250
    ""TotalWeightKg"": null

    TotalWeightKg means the explicitly stated total weight of the entire package group.

    Example:

    ""3 pallets, total weight 750 kg""

    Return:

    ""Quantity"": 3
    ""WeightPerPackageKg"": null
    ""TotalWeightKg"": 750

    If both values are explicitly stated, populate both.

    NEVER calculate one value from the other.

    Example:

    ""3 pallets, 250 kg each""

    Do NOT calculate:

    ""TotalWeightKg"": 750

    unless 750 kg is explicitly stated.

    ==================================================
    PACKAGE GOODS DESCRIPTION
    ==================================================

    Package-level GoodsDescription should only be populated when a goods description can be explicitly associated with that specific package group.

    Example:

    ""2 pallets machine parts, 1 crate electronics""

    Create two package entries with separate GoodsDescription values.

    If the goods description applies to the whole shipment but cannot be assigned to a specific package group:
    populate ShipmentInformation.GoodsDescription and leave the package-level GoodsDescription null.

    ==================================================
    PACKAGE STACKABILITY
    ==================================================

    Package-level Stackable:

    true:
    if this specific package group is explicitly described as stackable.

    false:
    if this specific package group is explicitly described as not stackable / non-stackable.

    null:
    if stackability is not stated for this package group.

    Shipment-level Stackable should only be populated if the statement clearly applies to the entire shipment.

    Example:

    ""All pallets are stackable.""

    Shipment-level:
    ""Stackable"": true

    Individual package groups may also contain true if clearly applicable.

    ==================================================
    SHIPMENT TOTALS
    ==================================================

    GrossWeightKg represents an explicitly stated total gross weight for the entire shipment.

    Example:

    ""Total gross weight: 1,850 kg""

    Return:

    ""GrossWeightKg"": 1850

    Do NOT calculate GrossWeightKg from package weights.

    VolumeM3 represents an explicitly stated total shipment volume in cubic meters.

    Do NOT calculate VolumeM3 from package dimensions.

    LoadingMeters represents explicitly stated loading meters / LDM.

    Do NOT calculate LoadingMeters from pallet dimensions.

    ==================================================
    QUOTATION
    ==================================================

    QuotationRequested:

    true:
    if the sender explicitly asks for:
    - quotation
    - quote
    - rate
    - price
    - offer
    - pricing
    - best rate
    - freight cost

    false:
    only if the email explicitly indicates that no quotation is required.

    Otherwise:
    null

    ==================================================
    DANGEROUS GOODS
    ==================================================

    DangerousGoods:

    true:
    if dangerous goods / hazardous cargo / ADR / IMO / IATA DGR are explicitly mentioned as applicable.

    false:
    only if explicitly stated that the shipment is NOT dangerous goods.

    Otherwise:
    null

    DangerousGoodsDetails contains only explicitly stated details such as:

    - UN number
    - hazard class
    - packing group
    - proper shipping name
    - ADR details
    - IMO details
    - IATA DGR details

    Do NOT infer dangerous goods status from the commodity description alone.

    ==================================================
    TEMPERATURE CONTROL
    ==================================================

    TemperatureControlled:

    true:
    if temperature-controlled transport is explicitly required.

    false:
    only if explicitly stated that temperature control is not required.

    Otherwise:
    null

    MinimumTemperatureCelsius and MaximumTemperatureCelsius:

    Populate only if explicit Celsius values are provided.

    Example:

    ""Keep between +2°C and +8°C""

    Return:

    ""MinimumTemperatureCelsius"": 2
    ""MaximumTemperatureCelsius"": 8

    Do NOT convert Fahrenheit or other units.

    ==================================================
    CUSTOMS
    ==================================================

    CustomsRequired:

    true:
    if customs clearance, import customs, export customs, or customs handling is explicitly requested.

    false:
    only if explicitly stated that customs services are not required.

    Otherwise:
    null

    HsCode contains an explicitly stated:
    - HS Code
    - tariff code
    - customs tariff number

    Do NOT derive an HS code from the goods description.

    ==================================================
    CUSTOMER REFERENCE
    ==================================================

    CustomerReference may contain an explicitly stated:

    - RFQ number
    - inquiry number
    - shipment reference
    - booking reference
    - transport reference
    - PO number
    - customer reference

    Preserve the original value.

    ==================================================
    SPECIAL INSTRUCTIONS
    ==================================================

    SpecialInstructions contains explicit shipment requirements that cannot be represented in another property.

    Examples:

    - tail lift required
    - side loading required
    - crane required
    - fixed delivery appointment
    - exchange pallets
    - time-critical shipment
    - special handling instructions
    - loading restrictions
    - delivery restrictions

    Do NOT duplicate information that already has its own structured property unless preserving the instruction is necessary for meaning.

    ==================================================
    REQUIRED JSON STRUCTURE
    ==================================================

    Return EXACTLY this structure:

    {{
      ""TransportMode"": null,
      ""ShipmentType"": null,
      ""QuotationRequested"": null,

      ""Pickup"": {{
        ""Company"": null,
        ""Address"": null,
        ""PostalCode"": null,
        ""City"": null,
        ""State"": null,
        ""Country"": null
      }},

      ""Delivery"": {{
        ""Company"": null,
        ""Address"": null,
        ""PostalCode"": null,
        ""City"": null,
        ""State"": null,
        ""Country"": null
      }},

      ""PickupDate"": null,
      ""DeliveryDate"": null,
      ""Incoterm"": null,
      ""GoodsDescription"": null,

      ""GrossWeightKg"": null,
      ""VolumeM3"": null,
      ""LoadingMeters"": null,

      ""Packages"": [
      ],

      ""ContainerType"": null,

      ""DangerousGoods"": null,
      ""DangerousGoodsDetails"": null,

      ""TemperatureControlled"": null,
      ""MinimumTemperatureCelsius"": null,
      ""MaximumTemperatureCelsius"": null,

      ""Stackable"": null,
      ""CustomsRequired"": null,

      ""HsCode"": null,
      ""CustomerReference"": null,
      ""SpecialInstructions"": null
    }}

    Each Packages element MUST have exactly this structure:

    {{
      ""Quantity"": null,
      ""PackageType"": null,
      ""LengthCm"": null,
      ""WidthCm"": null,
      ""HeightCm"": null,
      ""WeightPerPackageKg"": null,
      ""TotalWeightKg"": null,
      ""GoodsDescription"": null,
      ""Stackable"": null
    }}

    If no package information is present, return:

    ""Packages"": []

    Do NOT return:

    ""Packages"": null

    ==================================================
    EMAIL TO ANALYZE
    ==================================================

    ------------------------
    {emailText}
    ------------------------

    Return ONLY the JSON object.
    ";
  }
}