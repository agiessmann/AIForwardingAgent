using AiForwardingAgent.AgentServices;
using AiForwardingAgent.AgentServices.Models;
using AiForwardingAgent.BusinessLogic.CheckPostalCode;
using AiForwardingAgent.Data.Services;

namespace AiForwardingAgent.ForwardingSkills;

public interface IFreightRequirementMatrixService
{
    Task<FreightRequestValidationResult> ValidateAsync(
        ShipmentDto shipment,
        CancellationToken cancellationToken = default);
}

public sealed class FreightRequirementMatrixService(ICheckPostalCodeService checkPostalCodeService, 
    IFreightLocationService freightLocationService, IFreightRequestService freightRequestService)
    : IFreightRequirementMatrixService
{
    public async Task<FreightRequestValidationResult> ValidateAsync(
        ShipmentDto shipment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        var missing = new List<MissingInformation>();

        ValidateCommonRequirements(shipment, missing);
        ValidateLocationsAndTradeTerms(shipment, missing);
        ValidateTransportModeRequirements(shipment, missing);
        ValidateConditionalRequirements(shipment, missing);
        await ValidatePostalCodesAsync(shipment, missing, cancellationToken);
        
        // Update Locations to Database
        if (shipment.RequestId != null)
        {
            var freightrequest = await freightRequestService.GetFreightRequestByIdAsync(shipment.RequestId.Value);
            if (freightrequest != null)
            {
                if (freightrequest.Shipment?.Pickup != null)
                {
                    var pickupLocation = await freightLocationService.GetFreightLocationByIdAsync(freightrequest.Shipment.Pickup.Id);
                    if (pickupLocation != null)
                    {
                        pickupLocation.ResolvedPostalCode = shipment.Pickup?.PostalCode;
                        pickupLocation.ResolvedCity = shipment.Pickup?.City;
                        pickupLocation.State = shipment.Pickup?.State;
                        pickupLocation.Country = shipment.Pickup?.Country;
                        pickupLocation.PostalCodeSimilarity = shipment.Pickup?.PostalCodeSimilarity;
                        pickupLocation.PostalCodeMatchQuality = shipment.Pickup?.PostalCodeMatchQuality;
                        pickupLocation.Latitude = shipment.Pickup?.Latitude;
                        pickupLocation.Longitude = shipment.Pickup?.Longitude;
                        await freightLocationService.UpdateFreightLocationAsync(pickupLocation);
                    }
                }

                if (freightrequest.Shipment?.Delivery != null)
                {
                    var deliveryLocation =
                        await freightLocationService.GetFreightLocationByIdAsync(
                            freightrequest.Shipment.Delivery.Id);
                    if (deliveryLocation != null)
                    {
                        deliveryLocation.ResolvedPostalCode = shipment.Delivery?.PostalCode;
                        deliveryLocation.ResolvedCity = shipment.Delivery?.City;
                        deliveryLocation.State = shipment.Delivery?.State;
                        deliveryLocation.Country = shipment.Delivery?.Country;
                        deliveryLocation.PostalCodeSimilarity = shipment.Delivery?.PostalCodeSimilarity;
                        deliveryLocation.PostalCodeMatchQuality = shipment.Delivery?.PostalCodeMatchQuality;
                        deliveryLocation.Latitude = shipment.Delivery?.Latitude;
                        deliveryLocation.Longitude = shipment.Delivery?.Longitude;
                        await freightLocationService.UpdateFreightLocationAsync(deliveryLocation);
                    }
                }
            }
        }


        return new FreightRequestValidationResult
        {
            MissingInformation = missing
                .GroupBy(x => x.Field)
                .Select(group => group
                    .OrderByDescending(x => x.Level)
                    .First())
                .ToList()
        };
    }
    
    // =========================================================
    // POSTAL CODE VALIDATION
    // =========================================================
    private async Task ValidatePostalCodesAsync(
        ShipmentDto shipment,
        List<MissingInformation> missing,
        CancellationToken cancellationToken)
    {
        await ValidateLocationPostalCodeAsync(
            shipment.Pickup,
            fieldPrefix: "Pickup",
            displayName: "pickup",
            missing,
            cancellationToken);

        await ValidateLocationPostalCodeAsync(
            shipment.Delivery,
            fieldPrefix: "Delivery",
            displayName: "delivery",
            missing,
            cancellationToken);
    }
    
    private async Task ValidateLocationPostalCodeAsync(
        LocationDto? location,
        string fieldPrefix,
        string displayName,
        List<MissingInformation> missing,
        CancellationToken cancellationToken)
    {
        if (location is null)
            return;

        /*
         * GeoNames kann nur sinnvoll prüfen,
         * wenn PLZ UND Ort vorhanden sind.
         *
         * Fehlende Werte werden bereits von der
         * Requirement Matrix behandelt.
         */
        if (string.IsNullOrWhiteSpace(location.PostalCode) ||
            string.IsNullOrWhiteSpace(location.City))
        {
            return;
        }

        var result = await checkPostalCodeService.CheckAsync(
            location.PostalCode,
            location.City,
            cancellationToken);
        
        ApplyPostalCodeResult(location, result);

        switch (result.MatchQuality)
        {
            case MatchQuality.Exact:
                HandleExactLocationMatch(
                    location,
                    result);

                break;

            case MatchQuality.Similar:
                HandleSimilarLocationMatch(
                    location,
                    result,
                    fieldPrefix,
                    displayName,
                    missing);

                break;

            case MatchQuality.NoMatch:
                HandleInvalidLocationMatch(
                    location,
                    result,
                    fieldPrefix,
                    displayName,
                    missing);

                break;
        }
    }
    
    private static void HandleExactLocationMatch(
        LocationDto location,
        CheckPostalCodeResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.CountryCode))
        {
            location.Country = result.CountryCode;
        }

        /*
         * Optional:
         *
         * Falls dein LocationInformation-Modell diese
         * Properties enthält, kannst du auch folgendes übernehmen:
         *
         * location.City = result.City;
         * location.Latitude = result.Latitude;
         * location.Longitude = result.Longitude;
         * location.State = result.State;
         */
    }
    private static void HandleSimilarLocationMatch(
        LocationDto location,
        CheckPostalCodeResult result,
        string fieldPrefix,
        string displayName,
        List<MissingInformation> missing)
    {
        Add(
            RequirementLevel.Recommended,
            field: $"{fieldPrefix}.City",
            reason:
            $"The entered {displayName} city '{location.City}' does not exactly match the postal code '{location.PostalCode}'. " +
            $"The closest matching city is '{result.City}' ({result.CountryCode}).",
            questionKey:
            fieldPrefix == "Pickup"
                ? "PickupCityConfirmation"
                : "DeliveryCityConfirmation",
            missing);
    }
    
    private static void HandleInvalidLocationMatch(
        LocationDto location,
        CheckPostalCodeResult result,
        string fieldPrefix,
        string displayName,
        List<MissingInformation> missing)
    {
        var suggestion =
            !string.IsNullOrWhiteSpace(result.City)
                ? $" The postal code appears to belong to '{result.City}' ({result.CountryCode})."
                : string.Empty;

        Add(
            RequirementLevel.Required,
            field: $"{fieldPrefix}.PostalCodeCity",
            reason:
            $"The {displayName} postal code '{location.PostalCode}' does not match the city '{location.City}'." +
            suggestion,
            questionKey:
            fieldPrefix == "Pickup"
                ? "PickupPostalCodeCity"
                : "DeliveryPostalCodeCity",
            missing);
    }

    private static void ApplyPostalCodeResult(
        LocationDto location,
        CheckPostalCodeResult result)
    {
        location.ResolvedPostalCode = result.PostalCode;
        location.ResolvedCity = result.City;

        location.CountryCode = result.CountryCode;
        location.CountryName = result.CountryName;

        location.State = result.State;

        location.Latitude = result.Latitude;
        location.Longitude = result.Longitude;

        location.PostalCodeMatchQuality = result.MatchQuality;
        location.PostalCodeSimilarity = result.Similarity;

        // Wenn gar kein Land angegeben wurde,
        // können wir es sinnvoll ergänzen.
        if (string.IsNullOrWhiteSpace(location.Country) &&
            !string.IsNullOrWhiteSpace(result.CountryCode))
        {
            location.Country = result.CountryCode;
        }
    }
    
    // =========================================================
    // COMMON
    // =========================================================

    private static void ValidateCommonRequirements(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        Require(
            shipment.TransportMode is not null,
            field: "TransportMode",
            reason:
                "The transport mode is required to determine how the shipment should be handled.",
            questionKey: "TransportMode",
            missing);

        Require(
            !string.IsNullOrWhiteSpace(shipment.GoodsDescription),
            field: "GoodsDescription",
            reason:
                "A goods description is required to evaluate and quote the shipment.",
            questionKey: "GoodsDescription",
            missing);

        Recommend(
            shipment.PickupDate is not null,
            field: "PickupDate",
            reason:
                "The pickup date helps us check availability and calculate the appropriate rate.",
            questionKey: "PickupDate",
            missing);

        Recommend(
            !string.IsNullOrWhiteSpace(shipment.CustomerReference),
            field: "CustomerReference",
            reason:
                "A customer reference helps identify the shipment in further communication.",
            questionKey: "CustomerReference",
            missing);
    }

    // =========================================================
    // LOCATIONS / TRADE TERMS
    // =========================================================

    private static void ValidateLocationsAndTradeTerms(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        ValidateOriginSide(shipment, missing);
        ValidateDestinationSide(shipment, missing);
    }

    private static void ValidateOriginSide(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        var term = NormalizeIncoterm(
            shipment.OriginTerm?.Incoterm);

        switch (term)
        {
            case "FOB":
            case "FAS":
                RequireOriginNamedPlace(shipment, missing);
                break;

            case "FCA":
                RequireOriginNamedPlace(shipment, missing);
                break;

            case "EXW":
                RequirePickupLocation(shipment, missing);
                break;

            case null:
                RequirePickupLocation(shipment, missing);
                break;

            default:
                if (!HasOriginNamedPlace(shipment))
                {
                    RequirePickupLocation(shipment, missing);
                }

                break;
        }
    }

    private static void ValidateDestinationSide(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        var term = NormalizeIncoterm(
            shipment.DestinationTerm?.Incoterm);

        switch (term)
        {
            case "CFR":
            case "CIF":
                RequireDestinationNamedPlace(shipment, missing);
                break;

            case "CPT":
            case "CIP":
                RequireDestinationNamedPlace(shipment, missing);
                break;

            case "DAP":
            case "DPU":
            case "DDP":
                RequireDeliveryLocation(shipment, missing);
                break;

            case null:
                RequireDeliveryLocation(shipment, missing);
                break;

            default:
                if (!HasDestinationNamedPlace(shipment))
                {
                    RequireDeliveryLocation(shipment, missing);
                }

                break;
        }
    }

    private static void RequireOriginNamedPlace(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        Require(
            HasOriginNamedPlace(shipment),
            field: "OriginTerm.NamedPlace",
            reason:
                "The named origin place, port or terminal is required for the stated trade term.",
            questionKey: "OriginNamedPlace",
            missing);
    }

    private static void RequireDestinationNamedPlace(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        Require(
            HasDestinationNamedPlace(shipment),
            field: "DestinationTerm.NamedPlace",
            reason:
                "The named destination place, port or terminal is required for the stated trade term.",
            questionKey: "DestinationNamedPlace",
            missing);
    }

    private static bool HasOriginNamedPlace(
        ShipmentDto shipment)
    {
        return !string.IsNullOrWhiteSpace(
            shipment.OriginTerm?.NamedPlace);
    }

    private static bool HasDestinationNamedPlace(
        ShipmentDto shipment)
    {
        return !string.IsNullOrWhiteSpace(
            shipment.DestinationTerm?.NamedPlace);
    }

    private static string? NormalizeIncoterm(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static void RequirePickupLocation(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        RequireLocation(
            shipment.Pickup,
            fieldPrefix: "Pickup",
            displayName: "pickup",
            missing);
    }

    private static void RequireDeliveryLocation(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        RequireLocation(
            shipment.Delivery,
            fieldPrefix: "Delivery",
            displayName: "delivery",
            missing);
    }

    private static void RequireLocation(
        LocationDto? location,
        string fieldPrefix,
        string displayName,
        List<MissingInformation> missing)
    {
        if (location is null)
        {
            Add(
                RequirementLevel.Required,
                field: fieldPrefix,
                reason:
                $"The {displayName} location is required for routing and pricing.",
                questionKey:
                fieldPrefix == "Pickup"
                    ? "PickupLocation"
                    : "DeliveryLocation",
                missing);

            return;
        }

        var hasLocation =
            !string.IsNullOrWhiteSpace(location.Address) ||
            !string.IsNullOrWhiteSpace(location.City) ||
            !string.IsNullOrWhiteSpace(location.PostalCode);

        if (!hasLocation)
        {
            Add(
                RequirementLevel.Required,
                field: fieldPrefix,
                reason:
                $"The {displayName} location is required for routing and pricing.",
                questionKey:
                fieldPrefix == "Pickup"
                    ? "PickupLocation"
                    : "DeliveryLocation",
                missing);

            return;
        }

        Require(
            !string.IsNullOrWhiteSpace(location.PostalCode),
            field: $"{fieldPrefix}.PostalCode",
            reason:
            $"The {displayName} postal code is required for routing and pricing.",
            questionKey:
            fieldPrefix == "Pickup"
                ? "PickupPostalCode"
                : "DeliveryPostalCode",
            missing);

        Require(
            !string.IsNullOrWhiteSpace(location.City),
            field: $"{fieldPrefix}.City",
            reason:
            $"The {displayName} city is required to validate the postal code and location.",
            questionKey:
            fieldPrefix == "Pickup"
                ? "PickupCity"
                : "DeliveryCity",
            missing);
    }

    // =========================================================
    // TRANSPORT MODES
    // =========================================================

    private static void ValidateTransportModeRequirements(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        if (shipment.TransportMode is null)
            return;

        switch (shipment.TransportMode.Value)
        {
            case TransportMode.Road:
                ValidateRoad(shipment, missing);
                break;

            case TransportMode.Air:
                ValidateAir(shipment, missing);
                break;

            case TransportMode.Ocean:
                ValidateOcean(shipment, missing);
                break;

            case TransportMode.Rail:
                ValidateRail(shipment, missing);
                break;

            case TransportMode.Courier:
            case TransportMode.Express:
                ValidateCourierOrExpress(shipment, missing);
                break;

            case TransportMode.Multimodal:
                ValidateMultimodal(shipment, missing);
                break;
        }
    }

    // =========================================================
    // ROAD
    // =========================================================

    private static void ValidateRoad(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        Require(
            shipment.ShipmentType is not null,
            field: "ShipmentType",
            reason:
                "The shipment type is required to determine the correct road freight service.",
            questionKey: "ShipmentType",
            missing);

        switch (shipment.ShipmentType)
        {
            case ShipmentType.Ftl:
                Require(
                    shipment.GrossWeightKg is > 0,
                    field: "GrossWeightKg",
                    reason:
                        "The total gross weight is required for an FTL quotation.",
                    questionKey: "TotalGrossWeight",
                    missing);

                Recommend(
                    shipment.Packages?.Count > 0,
                    field: "Packages",
                    reason:
                        "Package details help us assess loading requirements.",
                    questionKey: "Packages",
                    missing);

                Recommend(
                    shipment.LoadingMeters is > 0,
                    field: "LoadingMeters",
                    reason:
                        "Loading meters may help determine the required vehicle capacity.",
                    questionKey: "LoadingMeters",
                    missing);

                break;

            case ShipmentType.Ltl:
            case ShipmentType.Pallet:
                RequirePackages(shipment, missing);

                ValidateWeightInformation(
                    shipment,
                    RequirementLevel.Required,
                    missing);

                ValidatePackageDimensions(
                    shipment,
                    RequirementLevel.Recommended,
                    missing);

                break;

            case ShipmentType.Parcel:
                RequirePackages(shipment, missing);

                ValidateWeightInformation(
                    shipment,
                    RequirementLevel.Required,
                    missing);

                ValidatePackageDimensions(
                    shipment,
                    RequirementLevel.Recommended,
                    missing);

                break;
        }
    }

    // =========================================================
    // AIR
    // =========================================================

    private static void ValidateAir(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        RequirePackages(shipment, missing);

        ValidateWeightInformation(
            shipment,
            RequirementLevel.Required,
            missing);

        ValidatePackageDimensions(
            shipment,
            RequirementLevel.Required,
            missing);

        Recommend(
            !string.IsNullOrWhiteSpace(shipment.HsCode),
            field: "HsCode",
            reason:
                "The HS code can be relevant for customs handling.",
            questionKey: "HsCode",
            missing);

        Recommend(
            shipment.CustomsRequired is not null,
            field: "CustomsRequired",
            reason:
                "It is helpful to know whether customs clearance is required.",
            questionKey: "CustomsRequired",
            missing);
    }

    // =========================================================
    // OCEAN
    // =========================================================

    private static void ValidateOcean(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        Require(
            shipment.ShipmentType is not null,
            field: "ShipmentType",
            reason:
                "The shipment type is required to determine the correct ocean freight service.",
            questionKey: "ShipmentType",
            missing);

        /*
         * Weight is required for every ocean shipment.
         *
         * Important:
         * This validation happens BEFORE the ShipmentType switch.
         *
         * Otherwise a shipment with ShipmentType == null would only
         * result in a ShipmentType question, while a clearly described
         * package without weight would not be reported.
         *
         * Example:
         * "1 crate 100 x 100 x 100 cm, FOB Rotterdam to CFR New York"
         *
         * -> Packages[0].Weight must still be reported as missing.
         */
        ValidateWeightInformation(
            shipment,
            RequirementLevel.Required,
            missing);

        switch (shipment.ShipmentType)
        {
            case ShipmentType.Fcl:
                Require(
                    !string.IsNullOrWhiteSpace(shipment.ContainerType),
                    field: "ContainerType",
                    reason:
                        "The container type is required for an FCL quotation.",
                    questionKey: "ContainerType",
                    missing);

                break;

            case ShipmentType.Lcl:
                RequirePackages(shipment, missing);

                Require(
                    shipment.VolumeM3 is > 0 ||
                    PackagesHaveCompleteDimensions(shipment),
                    field: "VolumeM3",
                    reason:
                        "The shipment volume or complete package dimensions are required for LCL pricing.",
                    questionKey: "VolumeOrDimensions",
                    missing);

                break;

            case ShipmentType.BreakBulk:
                RequirePackages(shipment, missing);

                ValidatePackageDimensions(
                    shipment,
                    RequirementLevel.Required,
                    missing);

                break;

            case ShipmentType.Bulk:
                // GrossWeightKg / weight information has already
                // been validated above.
                break;
        }

        RecommendTradeTermInformation(
            shipment,
            missing);

        Recommend(
            !string.IsNullOrWhiteSpace(shipment.HsCode),
            field: "HsCode",
            reason:
                "The HS code may be required for customs-related services.",
            questionKey: "HsCode",
            missing);
    }

    // =========================================================
    // RAIL
    // =========================================================

    private static void ValidateRail(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        Require(
            shipment.ShipmentType is not null,
            field: "ShipmentType",
            reason:
                "The shipment type is required to determine the correct rail service.",
            questionKey: "ShipmentType",
            missing);

        /*
         * Weight is required independently of ShipmentType.
         */
        ValidateWeightInformation(
            shipment,
            RequirementLevel.Required,
            missing);

        if (shipment.ShipmentType == ShipmentType.Fcl)
        {
            Require(
                !string.IsNullOrWhiteSpace(shipment.ContainerType),
                field: "ContainerType",
                reason:
                    "The container type is required for containerized rail freight.",
                questionKey: "ContainerType",
                missing);
        }
        else if (shipment.ShipmentType is not null)
        {
            RequirePackages(shipment, missing);
        }
    }

    // =========================================================
    // COURIER / EXPRESS
    // =========================================================

    private static void ValidateCourierOrExpress(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        RequirePackages(shipment, missing);

        ValidateWeightInformation(
            shipment,
            RequirementLevel.Required,
            missing);

        ValidatePackageDimensions(
            shipment,
            RequirementLevel.Required,
            missing);

        Recommend(
            shipment.DeliveryDate is not null,
            field: "DeliveryDate",
            reason:
                "The requested delivery date helps determine the appropriate express service.",
            questionKey: "DeliveryDate",
            missing);
    }

    // =========================================================
    // MULTIMODAL
    // =========================================================

    private static void ValidateMultimodal(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        RequirePackages(shipment, missing);

        ValidateWeightInformation(
            shipment,
            RequirementLevel.Required,
            missing);

        Recommend(
            shipment.VolumeM3 is > 0 ||
            PackagesHaveCompleteDimensions(shipment),
            field: "VolumeM3",
            reason:
                "Volume information helps determine suitable transport equipment.",
            questionKey: "VolumeOrDimensions",
            missing);
    }

    // =========================================================
    // TRADE TERMS
    // =========================================================

    private static void RecommendTradeTermInformation(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        var hasOriginTerm =
            !string.IsNullOrWhiteSpace(
                shipment.OriginTerm?.Incoterm);

        var hasDestinationTerm =
            !string.IsNullOrWhiteSpace(
                shipment.DestinationTerm?.Incoterm);

        Recommend(
            hasOriginTerm || hasDestinationTerm,
            field: "TradeTerms",
            reason:
                "Trade terms help determine the commercial scope of the requested service.",
            questionKey: "TradeTerms",
            missing);
    }

    // =========================================================
    // CONDITIONAL REQUIREMENTS
    // =========================================================

    private static void ValidateConditionalRequirements(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        if (shipment.DangerousGoods == true)
        {
            Require(
                !string.IsNullOrWhiteSpace(
                    shipment.DangerousGoodsDetails),
                field: "DangerousGoodsDetails",
                reason:
                    "Dangerous goods details are required to check transport acceptance and legal requirements.",
                questionKey: "DangerousGoodsDetails",
                missing);
        }

        if (shipment.TemperatureControlled == true)
        {
            Require(
                shipment.MinimumTemperatureCelsius is not null ||
                shipment.MaximumTemperatureCelsius is not null,
                field: "TemperatureRange",
                reason:
                    "The required temperature range is needed to arrange temperature-controlled transport.",
                questionKey: "TemperatureRange",
                missing);
        }

        if (shipment.CustomsRequired == true)
        {
            Recommend(
                !string.IsNullOrWhiteSpace(shipment.HsCode),
                field: "HsCode",
                reason:
                    "The HS code is useful for customs processing.",
                questionKey: "HsCode",
                missing);

            var hasTradeTerm =
                !string.IsNullOrWhiteSpace(
                    shipment.OriginTerm?.Incoterm) ||
                !string.IsNullOrWhiteSpace(
                    shipment.DestinationTerm?.Incoterm);

            Recommend(
                hasTradeTerm,
                field: "TradeTerms",
                reason:
                    "Trade terms help determine customs and transport responsibilities.",
                questionKey: "TradeTerms",
                missing);
        }
    }

    // =========================================================
    // PACKAGES
    // =========================================================

    private static void RequirePackages(
        ShipmentDto shipment,
        List<MissingInformation> missing)
    {
        if (shipment.Packages is null ||
            shipment.Packages.Count == 0)
        {
            Add(
                RequirementLevel.Required,
                field: "Packages",
                reason:
                    "Package information is required to evaluate and quote this shipment.",
                questionKey: "Packages",
                missing);

            return;
        }

        for (var i = 0; i < shipment.Packages.Count; i++)
        {
            var package = shipment.Packages[i];
            var number = i + 1;

            Require(
                package.Quantity is > 0,
                field: $"Packages[{i}].Quantity",
                reason:
                    $"The quantity of package group {number} is required.",
                questionKey: "PackageQuantity",
                missing);

            Require(
                !string.IsNullOrWhiteSpace(package.PackageType),
                field: $"Packages[{i}].PackageType",
                reason:
                    $"The packaging type of package group {number} is required.",
                questionKey: "PackageType",
                missing);
        }
    }

    private static void ValidatePackageDimensions(
        ShipmentDto shipment,
        RequirementLevel level,
        List<MissingInformation> missing)
    {
        if (shipment.Packages is null ||
            shipment.Packages.Count == 0)
        {
            return;
        }

        for (var i = 0; i < shipment.Packages.Count; i++)
        {
            var package = shipment.Packages[i];
            var number = i + 1;

            var dimensionsComplete =
                package.LengthCm is > 0 &&
                package.WidthCm is > 0 &&
                package.HeightCm is > 0;

            if (dimensionsComplete)
                continue;

            Add(
                level,
                field: $"Packages[{i}].Dimensions",
                reason:
                    $"Complete dimensions are required for package group {number}.",
                questionKey: "PackageDimensions",
                missing);
        }
    }

    private static void ValidatePackageWeights(
        ShipmentDto shipment,
        RequirementLevel level,
        List<MissingInformation> missing)
    {
        if (shipment.Packages is null ||
            shipment.Packages.Count == 0)
        {
            return;
        }

        for (var i = 0; i < shipment.Packages.Count; i++)
        {
            var package = shipment.Packages[i];
            var number = i + 1;

            var weightAvailable =
                package.WeightPerPackageKg is > 0 ||
                package.TotalWeightKg is > 0;

            if (weightAvailable)
                continue;

            Add(
                level,
                field: $"Packages[{i}].Weight",
                reason:
                    $"Weight information is required for package group {number}.",
                questionKey: "PackageWeight",
                missing);
        }
    }

    /// <summary>
    /// Validates that usable weight information exists.
    ///
    /// Rules:
    ///
    /// 1. If GrossWeightKg is available, the requirement is satisfied.
    ///
    /// 2. Otherwise, if packages exist, every package group must have either
    ///    WeightPerPackageKg or TotalWeightKg.
    ///
    /// 3. If packages exist but one package group has no weight, the missing
    ///    field is Packages[i].Weight with QuestionKey PackageWeight.
    ///
    /// 4. If no package information exists at all, a shipment-level
    ///    TotalGrossWeight request is generated. RequirePackages may additionally
    ///    request package details depending on the transport type.
    /// </summary>
    private static void ValidateWeightInformation(
        ShipmentDto shipment,
        RequirementLevel level,
        List<MissingInformation> missing)
    {
        if (shipment.GrossWeightKg is > 0)
            return;

        if (shipment.Packages is { Count: > 0 })
        {
            ValidatePackageWeights(
                shipment,
                level,
                missing);

            return;
        }

        Add(
            level,
            field: "GrossWeightKg",
            reason:
                "Weight information is required to evaluate and price the shipment.",
            questionKey: "TotalGrossWeight",
            missing);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static bool PackagesHaveCompleteDimensions(
        ShipmentDto shipment)
    {
        return shipment.Packages is { Count: > 0 } &&
               shipment.Packages.All(x =>
                   x is
                   {
                       LengthCm: > 0,
                       WidthCm: > 0,
                       HeightCm: > 0
                   });
    }

    private static void Require(
        bool condition,
        string field,
        string reason,
        string questionKey,
        ICollection<MissingInformation> missing)
    {
        if (!condition)
        {
            Add(
                RequirementLevel.Required,
                field,
                reason,
                questionKey,
                missing);
        }
    }

    private static void Recommend(
        bool condition,
        string field,
        string reason,
        string questionKey,
        ICollection<MissingInformation> missing)
    {
        if (!condition)
        {
            Add(
                RequirementLevel.Recommended,
                field,
                reason,
                questionKey,
                missing);
        }
    }

    private static void Add(
        RequirementLevel level,
        string field,
        string reason,
        string questionKey,
        ICollection<MissingInformation> missing)
    {
        missing.Add(new MissingInformation
        {
            Field = field,
            Level = level,
            Reason = reason,
            QuestionKey = questionKey
        });
    }
}