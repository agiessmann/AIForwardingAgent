using System.Text.Json.Serialization;
using AiForwardingAgent.AgentServices;
using AiForwardingAgent.BusinessLogic.CheckPostalCode;

namespace AiForwardingAgent.BusinessLogic.RouteLocationResolver;

public interface ILocationResolver
{
    Task<ResolvedLocation> ResolvePlaceAsync(
        string place,
        LocationSource source,
        CancellationToken cancellationToken = default);

    Task<ResolvedLocation> ResolveLocationAsync(
        LocationDto location,
        LocationSource source,
        CancellationToken cancellationToken = default);
}

public class LocationResolver(HttpClient httpClient, IConfiguration configuration) : ILocationResolver
{
    private readonly string _geoNamesUsername = configuration["GeoNames:Username"]
                                                ?? throw new InvalidOperationException(
                                                    "GeoNames:Username wurde nicht konfiguriert.");

    
    public async Task<ResolvedLocation> ResolvePlaceAsync(
        string place,
        LocationSource source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(place))
        {
            return new ResolvedLocation
            {
                Source = source,
                Status = LocationResolutionStatus.NotResolved
            };
        }

        var resolved = await ResolveByPlaceNameAsync(
            place,
            country: null,
            source,
            cancellationToken);

        return resolved ?? new ResolvedLocation
        {
            SourceValue = place,
            City = place,
            Source = source,
            Status = LocationResolutionStatus.NotResolved
        };
    }

    public async Task<ResolvedLocation> ResolveLocationAsync(
        LocationDto location,
        LocationSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!string.IsNullOrWhiteSpace(location.PostalCode))
        {
            var resolved = await ResolveByPostalCodeAsync(
                location.PostalCode,
                location.Country,
                location.City,
                source,
                cancellationToken);

            if (resolved is not null)
            {
                return resolved;
            }
        }

        if (!string.IsNullOrWhiteSpace(location.City))
        {
            var resolved = await ResolveByPlaceNameAsync(
                location.City,
                location.Country,
                source,
                cancellationToken);

            if (resolved is not null)
            {
                return resolved;
            }
        }

        return new ResolvedLocation
        {
            Company = location.Company,
            Address = location.Address,
            PostalCode = location.PostalCode,
            City = location.City,
            State = location.State,
            Country = location.Country,

            Source = source,
            SourceValue = BuildSourceValue(location),

            Status = LocationResolutionStatus.PartiallyResolved
        };
    }
    
    private async Task<ResolvedLocation?> ResolveByPlaceNameAsync(
    string place,
    string? country,
    LocationSource source,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(place))
            return null;

        var url =
            $"searchJSON" +
            $"?q={Uri.EscapeDataString(place)}" +
            $"&maxRows=20" +
            $"&featureClass=P" +
            $"&style=FULL" +
            $"&username={Uri.EscapeDataString(_geoNamesUsername)}";

        if (!string.IsNullOrWhiteSpace(country))
        {
            url +=
                $"&country={Uri.EscapeDataString(country)}";
        }

        using var response = await httpClient.GetAsync(
            url,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<GeoNamesSearchResponse>(
                cancellationToken: cancellationToken);

        if (result?.GeoNames is null ||
            result.GeoNames.Count == 0)
        {
            return null;
        }

        var matches = result.GeoNames
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Name) &&
                !string.IsNullOrWhiteSpace(x.CountryCode))
            .ToList();

        if (matches.Count == 0)
            return null;

        var exactMatches = matches
            .Where(x =>
                string.Equals(
                    x.Name,
                    place,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    x.ToponymName,
                    place,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var candidates =
            exactMatches.Count > 0
                ? exactMatches
                : matches;

        if (candidates.Count > 1 &&
            string.IsNullOrWhiteSpace(country))
        {
            return new ResolvedLocation
            {
                SourceValue = place,
                City = place,
                Source = source,
                Status = LocationResolutionStatus.Ambiguous
            };
        }

        var selected = candidates
            .OrderByDescending(x => x.Population ?? 0)
            .First();

        return new ResolvedLocation
        {
            City = selected.Name,
            State = selected.AdminName1,
            Country = selected.CountryName,
            CountryCode = selected.CountryCode,

            SourceValue = place,
            Source = source,

            Status = LocationResolutionStatus.Resolved
        };
    }
    
    private async Task<ResolvedLocation?> ResolveByPostalCodeAsync(
    string postalCode,
    string? country,
    string? city,
    LocationSource source,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return null;

        var url =
            $"postalCodeSearchJSON" +
            $"?postalcode={Uri.EscapeDataString(postalCode)}" +
            $"&maxRows=50" +
            $"&style=FULL" +
            $"&username={Uri.EscapeDataString(_geoNamesUsername)}";

        if (!string.IsNullOrWhiteSpace(country))
        {
            url +=
                $"&country={Uri.EscapeDataString(country)}";
        }

        using var response = await httpClient.GetAsync(
            url,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<GeoNamesPostalCodeSearchResponse>(
                cancellationToken: cancellationToken);

        if (result?.PostalCodes is null ||
            result.PostalCodes.Count == 0)
        {
            return null;
        }

        var candidates = result.PostalCodes
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.PostalCode) &&
                !string.IsNullOrWhiteSpace(x.CountryCode))
            .ToList();

        if (candidates.Count == 0)
            return null;

        // Falls die Stadt aus der Anfrage bekannt ist,
        // bevorzugen wir Treffer mit passendem Ortsnamen.
        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityMatches = candidates
                .Where(x =>
                    string.Equals(
                        x.PlaceName,
                        city,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cityMatches.Count == 1)
            {
                return CreateResolvedLocation(
                    cityMatches[0],
                    postalCode,
                    source);
            }

            if (cityMatches.Count > 1)
            {
                candidates = cityMatches;
            }
        }

        if (candidates.Count == 1)
        {
            return CreateResolvedLocation(
                candidates[0],
                postalCode,
                source);
        }

        // Gleiche PLZ kann theoretisch mehrere Orts-/Gebietstreffer liefern.
        // Wenn sie im Wesentlichen denselben Ort beschreiben, können wir
        // trotzdem einen eindeutigen Treffer erzeugen.
        var distinctPlaces = candidates
            .Select(x => new
            {
                PlaceName = x.PlaceName?.Trim(),
                CountryCode = x.CountryCode?.Trim().ToUpperInvariant(),
                AdminName1 = x.AdminName1?.Trim()
            })
            .Distinct()
            .ToList();

        if (distinctPlaces.Count == 1)
        {
            return CreateResolvedLocation(
                candidates[0],
                postalCode,
                source);
        }

        return new ResolvedLocation
        {
            PostalCode = postalCode,
            City = city,
            CountryCode = country,
            SourceValue = postalCode,
            Source = source,
            Status = LocationResolutionStatus.Ambiguous
        };
    }
    
    private static ResolvedLocation CreateResolvedLocation(
        GeoNamesPostalCode postalCode,
        string sourceValue,
        LocationSource source)
    {
        return new ResolvedLocation
        {
            PostalCode = postalCode.PostalCode,
            City = postalCode.PlaceName,
            State = postalCode.AdminName1,
            Country = postalCode.CountryCode,
            CountryCode = postalCode.CountryCode,

            SourceValue = sourceValue,
            Source = source,
            Status = LocationResolutionStatus.Resolved
        };
    }
    
    private static string? BuildSourceValue(
        LocationDto? location)
    {
        if (location is null)
            return null;

        var parts = new[]
        {
            location.Company,
            location.Address,
            location.PostalCode,
            location.City,
            location.State,
            location.Country
        };

        var values = parts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();

        return values.Length > 0
            ? string.Join(", ", values)
            : null;
    }
}

