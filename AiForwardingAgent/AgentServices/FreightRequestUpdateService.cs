using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AiForwardingAgent.AgentServices.Models;


namespace AiForwardingAgent.AgentServices;

public interface IFreightRequestUpdateService
{
    FreightRequestUpdateResult Apply(
        FreightRequestDto freightRequest,
        FreightRequestReplyAnalysisResult analysis);
}

public sealed class FreightRequestUpdateResult
{
    public required FreightRequestDto FreightRequest { get; init; }

    public List<ShipmentFieldUpdate> AppliedUpdates { get; init; } = [];

    public List<RejectedFieldUpdate> RejectedUpdates { get; init; } = [];

    public bool RequiresManualReview { get; init; }
}

public sealed class RejectedFieldUpdate
{
    public required string Field { get; init; }
    public required string Reason { get; init; }
}




public sealed class FreightRequestUpdateService : IFreightRequestUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Regex PackageFieldRegex = new(
        @"^Packages\[(\d+)\]\.(\w+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public FreightRequestUpdateResult Apply(FreightRequestDto freightRequest, FreightRequestReplyAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(freightRequest);
        ArgumentNullException.ThrowIfNull(analysis);

        var copy = Clone(freightRequest);

        var applied = new List<ShipmentFieldUpdate>();
        var rejected = new List<RejectedFieldUpdate>();

        if (!analysis.IsRelevantReply)
        {
            return new FreightRequestUpdateResult
            {
                FreightRequest = copy,
                RequiresManualReview = analysis.RequiresManualReview
            };
        }

        var shipment = copy.ShipmentInformation ?? new ShipmentDto();

        foreach (var update in analysis.Updates)
        {
            try
            {
                var updatedShipment = ApplyUpdate(shipment, update, analysis.Conflicts);
                shipment = updatedShipment;
                applied.Add(update);
            }
            catch (Exception ex) when (
                ex is ArgumentException or
                FormatException or
                InvalidOperationException or
                OverflowException)
            {
                rejected.Add(new RejectedFieldUpdate
                {
                    Field = update.Field,
                    Reason = ex.Message
                });
            }
        }

        copy.ShipmentInformation = shipment;

        // Only non-required questions can be declined; required ones must still be answered.
        var requiredKeys = (copy.MissingInformation ?? [])
            .Where(x => x.Level == RequirementLevel.Required)
            .Select(x => x.QuestionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        copy.DeclinedQuestionKeys = (copy.DeclinedQuestionKeys ?? [])
            .Concat(analysis.DeclinedQuestions.Select(x => x.QuestionKey))
            .Where(x => !string.IsNullOrWhiteSpace(x) && !requiredKeys.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FreightRequestUpdateResult
        {
            FreightRequest = copy,
            AppliedUpdates = applied,
            RejectedUpdates = rejected,
            RequiresManualReview =
                analysis.RequiresManualReview ||
                rejected.Count > 0
        };
    }

    private static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);

        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                "Could not clone freight request.");
    }

    private static ShipmentDto ApplyUpdate(ShipmentDto shipment, ShipmentFieldUpdate update,
        IReadOnlyCollection<InformationConflict> conflicts)
    {
        if (string.IsNullOrWhiteSpace(update.Field))
            throw new ArgumentException("Field is required.");

        if (string.IsNullOrWhiteSpace(update.Value))
            throw new ArgumentException("Value is required.");

        var packageMatch = PackageFieldRegex.Match(update.Field);

        if (packageMatch.Success)
        {
            return ApplyPackageUpdate(
                shipment,
                packageMatch,
                update,
                conflicts);
        }

        return ApplyShipmentUpdate(shipment, update, conflicts);
    }

    private static ShipmentDto ApplyShipmentUpdate(
        ShipmentDto shipment,
        ShipmentFieldUpdate update,
        IReadOnlyCollection<InformationConflict> conflicts)
    {
        // Wir erzeugen eine veränderbare JSON-Repräsentation.
        // Die erlaubten Pfade werden explizit geprüft.
        var node = JsonSerializer.SerializeToNode(shipment, JsonOptions) ?? throw new InvalidOperationException("Invalid shipment.");

        var (property, value) = ParseShipmentField(update);

        var target = (JsonObject)node;
        var leaf = property;

        var dot = property.IndexOf('.');
        if (dot > 0)
        {
            var parent = property[..dot];
            leaf = property[(dot + 1)..];

            if (node[parent] is not JsonObject nested)
            {
                nested = new JsonObject();
                node[parent] = nested;
            }

            target = nested;
        }

        var existingValue = target[leaf]?.ToString();

        EnsureOverwriteAllowed(update.Field, existingValue, value, conflicts);

        target[leaf] = value;
        
        if (dot > 0
            && (property[..dot] is "Pickup" or "Delivery")
            && (leaf is "PostalCode" or "City" or "Country"))
        {
            foreach (var stale in new[]
                     {
                         "ResolvedPostalCode", "ResolvedCity", "CountryCode", "CountryName",
                         "State", "Latitude", "Longitude",
                         "PostalCodeMatchQuality", "PostalCodeSimilarity"
                     })
            {
                target[stale] = null;
            }
        }

        return node.Deserialize<ShipmentDto>(JsonOptions) ?? throw new InvalidOperationException("Could not deserialize updated shipment.");
    }

    private static ShipmentDto ApplyPackageUpdate(ShipmentDto shipment, Match match, ShipmentFieldUpdate update,
        IReadOnlyCollection<InformationConflict> conflicts)
    {
        var index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

        var property = match.Groups[2].Value;

        var currentCount = shipment.Packages?.Count ?? 0;

        // Nur der direkt nächste Index darf eine neue Gruppe anlegen (keine Lücken).
        if (index > currentCount)
        {
            throw new InvalidOperationException(
                $"Package index {index} does not exist and cannot be created (next free index: {currentCount}).");
        }

        var (allowedProperty, value) = ParsePackageField(property, update.Value);

        var node = JsonSerializer.SerializeToNode(shipment, JsonOptions)
                   ?? throw new InvalidOperationException("Invalid shipment.");

        var packages = node["Packages"] as JsonArray;
        if (packages is null)
        {
            packages = new JsonArray();
            node["Packages"] = packages;
        }

        if (index == packages.Count)
            packages.Add(new JsonObject());

        var package = packages[index]?.AsObject()
                      ?? throw new InvalidOperationException($"Package {index} is null.");
        var existingValue = package[allowedProperty]?.ToString();

        EnsureOverwriteAllowed(
            update.Field,
            existingValue,
            value,
            conflicts);

        package[allowedProperty] = value;

        return node.Deserialize<ShipmentDto>(JsonOptions)
            ?? throw new InvalidOperationException(
                "Could not deserialize updated shipment.");
    }

    private static (string Property, JsonNode? Value) ParseShipmentField(ShipmentFieldUpdate update)
    {
        return update.Field switch
        {
            "GoodsDescription" =>
                (update.Field, JsonValue.Create(update.Value)),

            "GrossWeightKg" or
            "VolumeM3" or
            "LoadingMeters" or
            "MinimumTemperatureCelsius" or
            "MaximumTemperatureCelsius" =>
                (update.Field, JsonValue.Create(ParsePositiveDecimal(update.Value))),

            "DangerousGoods" or
            "TemperatureControlled" or
            "Stackable" or
            "CustomsRequired" or
            "QuotationRequested" =>
                (update.Field, JsonValue.Create(ParseBoolean(update.Value))),

            "ContainerType" or
            "DangerousGoodsDetails" or
            "HsCode" or
            "CustomerReference" or
            "SpecialInstructions" =>
                (update.Field, JsonValue.Create(update.Value)),

            "TransportMode" =>
                (update.Field, JsonValue.Create(
                    ParseEnum<TransportMode>(update.Value).ToString())),

            "ShipmentType" =>
                (update.Field, JsonValue.Create(
                    ParseEnum<ShipmentType>(update.Value).ToString())),

            "PickupDate" or "DeliveryDate" =>
                (update.Field, JsonValue.Create(
                    ParseDate(update.Value).ToString("yyyy-MM-dd"))),
            
            "Pickup.PostalCode" or "Pickup.City" or "Pickup.Country" or
                "Delivery.PostalCode" or "Delivery.City" or "Delivery.Country" or
                "OriginTerm.Incoterm" or "OriginTerm.NamedPlace" or
                "DestinationTerm.Incoterm" or "DestinationTerm.NamedPlace" =>
                (update.Field, JsonValue.Create(update.Value)),

            _ => throw new ArgumentException(
                $"Unsupported shipment field: {update.Field}")
        };
    }

    private static (string Property, JsonNode? Value) ParsePackageField(string property, string value)
    {
        return property switch
        {
            "Quantity" =>
                (property, JsonValue.Create(ParsePositiveInt(value))),

            "LengthCm" or
            "WidthCm" or
            "HeightCm" or
            "WeightPerPackageKg" or
            "TotalWeightKg" =>
                (property, JsonValue.Create(ParsePositiveDecimal(value))),

            "PackageType" or "GoodsDescription" =>
                (property, JsonValue.Create(value)),

            "Stackable" =>
                (property, JsonValue.Create(ParseBoolean(value))),

            _ => throw new ArgumentException(
                $"Unsupported package field: {property}")
        };
    }

    private static void EnsureOverwriteAllowed(
        string field,
        string? existingValue,
        JsonNode? newValue,
        IReadOnlyCollection<InformationConflict> conflicts)
    {
        if (string.IsNullOrWhiteSpace(existingValue))
            return;

        if (existingValue == newValue?.ToString())
            return;

        var explicitCorrection = conflicts.Any(x =>
            string.Equals(
                x.Field,
                field,
                StringComparison.Ordinal));

        if (!explicitCorrection)
        {
            throw new InvalidOperationException(
                $"Field '{field}' already contains a value. " +
                "An explicit correction is required.");
        }
    }

    private static decimal ParsePositiveDecimal(string value)
    {
        if (!decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var result) || result <= 0)
        {
            throw new FormatException(
                $"Invalid positive decimal value: {value}");
        }

        return result;
    }

    private static int ParsePositiveInt(string value)
    {
        if (!int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var result) || result <= 0)
        {
            throw new FormatException(
                $"Invalid positive integer value: {value}");
        }

        return result;
    }

    private static bool ParseBoolean(string value)
    {
        if (!bool.TryParse(value, out var result))
            throw new FormatException(
                $"Invalid boolean value: {value}");

        return result;
    }

    private static T ParseEnum<T>(string value)
        where T : struct, Enum
    {
        if (!Enum.TryParse<T>(
                value,
                ignoreCase: true,
                out var result) ||
            !Enum.IsDefined(result))
        {
            throw new FormatException(
                $"Invalid {typeof(T).Name}: {value}");
        }

        return result;
    }

    private static DateOnly ParseDate(string value)
    {
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            throw new FormatException(
                $"Invalid date: {value}");
        }

        return result;
    }
}