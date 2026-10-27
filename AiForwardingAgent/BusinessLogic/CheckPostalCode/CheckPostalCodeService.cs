namespace AiForwardingAgent.BusinessLogic.CheckPostalCode;

using System.Globalization;
using System.Net.Http.Json;
using System.Text;

public sealed class CheckPostalCodeService(
    HttpClient httpClient,
    IConfiguration configuration) : ICheckPostalCodeService
{
    private readonly string _geoNamesUsername = configuration["GeoNames:Username"]
                                                ?? throw new InvalidOperationException(
                                                    "GeoNames:Username wurde nicht konfiguriert.");

    // Ab diesem Wert gilt eine Stadt als "Similar".
    // 0.85 = 85 %
    private const double SimilarityThreshold = 0.85;

    public async Task<CheckPostalCodeResult> CheckAsync(
        string postalCode,
        string city,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            return Invalid("Keine Postleitzahl angegeben.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Invalid("Kein Ort angegeben.");
        }

        postalCode = postalCode.Trim();
        city = city.Trim();

        var url =
            "https://secure.geonames.org/postalCodeSearchJSON" +
            $"?postalcode={Uri.EscapeDataString(postalCode)}" +
            "&maxRows=100" +
            "&style=FULL" +
            $"&username={Uri.EscapeDataString(_geoNamesUsername)}";

        try
        {
            var response =
                await httpClient.GetFromJsonAsync<GeoNamesPostalCodeResponse>(
                    url,
                    cancellationToken);

            if (response?.PostalCodes == null ||
                response.PostalCodes.Count == 0)
            {
                return new CheckPostalCodeResult
                {
                    IsValid = false,
                    PostalCode = postalCode,
                    MatchQuality = MatchQuality.NoMatch,
                    Similarity = 0
                };
            }

            // Sicherheitshalber nur exakt passende PLZ berücksichtigen.
            var postalCodeMatches = response.PostalCodes
                .Where(x => PostalCodesEqual(
                    x.PostalCode,
                    postalCode))
                .ToList();

            if (postalCodeMatches.Count == 0)
            {
                return new CheckPostalCodeResult
                {
                    IsValid = false,
                    PostalCode = postalCode,
                    MatchQuality = MatchQuality.NoMatch,
                    Similarity = 0
                };
            }

            /*
             * 1. Zuerst nach einem exakten bzw.
             *    normalisierten Match suchen.
             */
            var exactMatch = postalCodeMatches
                .FirstOrDefault(x =>
                    IsExactCityMatch(
                        city,
                        x.PlaceName));

            if (exactMatch != null)
            {
                return CreateResult(
                    exactMatch,
                    MatchQuality.Exact,
                    1.0);
            }

            /*
             * 2. Kein Exact Match:
             *    Kandidaten über Fuzzy Matching bewerten.
             */
            var bestMatch = postalCodeMatches
                .Select(x => new
                {
                    PostalCode = x,
                    Similarity = CalculateSimilarity(
                        city,
                        x.PlaceName ?? string.Empty)
                })
                .OrderByDescending(x => x.Similarity)
                .First();

            if (bestMatch.Similarity >= SimilarityThreshold)
            {
                return CreateResult(
                    bestMatch.PostalCode,
                    MatchQuality.Similar,
                    bestMatch.Similarity);
            }

            /*
             * PLZ existiert zwar, aber der Ort passt nicht.
             *
             * Den besten Kandidaten geben wir trotzdem zurück,
             * damit die Anwendung z.B. anzeigen kann:
             *
             * "Meinten Sie Jesteburg?"
             */
            return new CheckPostalCodeResult
            {
                IsValid = false,

                CountryCode =
                    bestMatch.PostalCode.CountryCode,

                CountryName =
                    GetCountryName(
                        bestMatch.PostalCode.CountryCode),

                PostalCode =
                    bestMatch.PostalCode.PostalCode,

                City =
                    bestMatch.PostalCode.PlaceName,

                State =
                    bestMatch.PostalCode.AdminName1,

                Latitude =
                    bestMatch.PostalCode.Latitude,

                Longitude =
                    bestMatch.PostalCode.Longitude,

                MatchQuality =
                    MatchQuality.NoMatch,

                Similarity =
                    bestMatch.Similarity
            };
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return Error(
                "Zeitüberschreitung beim GeoNames-Service.");
        }
        catch (HttpRequestException ex)
        {
            return Error(
                $"GeoNames konnte nicht erreicht werden: {ex.Message}");
        }
    }

    private static CheckPostalCodeResult CreateResult(
        GeoNamesPostalCode match,
        MatchQuality quality,
        double similarity)
    {
        return new CheckPostalCodeResult
        {
            // Exact UND Similar werden akzeptiert.
            IsValid =
                quality == MatchQuality.Exact ||
                quality == MatchQuality.Similar,

            CountryCode = match.CountryCode,
            CountryName = GetCountryName(match.CountryCode),

            PostalCode = match.PostalCode,
            City = match.PlaceName,

            State = match.AdminName1,

            Latitude = match.Latitude,
            Longitude = match.Longitude,

            MatchQuality = quality,
            Similarity = similarity
        };
    }

    private static CheckPostalCodeResult Invalid(
        string message)
    {
        return new CheckPostalCodeResult
        {
            IsValid = false,
            MatchQuality = MatchQuality.NoMatch,
            Similarity = 0,
            ErrorMessage = message
        };
    }

    private static CheckPostalCodeResult Error(
        string message)
    {
        return new CheckPostalCodeResult
        {
            IsValid = false,
            MatchQuality = MatchQuality.NoMatch,
            Similarity = 0,
            ErrorMessage = message
        };
    }

    // --------------------------------------------------
    // PLZ Vergleich
    // --------------------------------------------------

    private static bool PostalCodesEqual(
        string? first,
        string? second)
    {
        if (string.IsNullOrWhiteSpace(first) ||
            string.IsNullOrWhiteSpace(second))
        {
            return false;
        }

        return NormalizePostalCode(first) ==
               NormalizePostalCode(second);
    }

    private static string NormalizePostalCode(
        string postalCode)
    {
        return postalCode
            .Replace(" ", "")
            .Replace("-", "")
            .Trim()
            .ToUpperInvariant();
    }

    // --------------------------------------------------
    // City Matching
    // --------------------------------------------------

    private static bool IsExactCityMatch(
        string input,
        string? geoNamesCity)
    {
        if (string.IsNullOrWhiteSpace(geoNamesCity))
            return false;

        return NormalizeCity(input) ==
               NormalizeCity(geoNamesCity);
    }

    private static string NormalizeCity(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value
            .Trim()
            .ToLowerInvariant();

        /*
         * Deutsche Schreibweisen normalisieren:
         *
         * Lueneburg -> Lüneburg
         * Muenchen  -> München
         */
        value = value
            .Replace("ae", "ä")
            .Replace("oe", "ö")
            .Replace("ue", "ü")
            .Replace("ß", "ss");

        // Unicode vereinheitlichen.
        value = value.Normalize(
            NormalizationForm.FormD);

        var builder = new StringBuilder();

        foreach (var character in value)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            /*
             * Akzente entfernen.
             *
             * München -> munchen
             * München und Munchen werden dadurch gleich.
             */
            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        value = builder
            .ToString()
            .Normalize(NormalizationForm.FormC);

        // Sonderzeichen/mehrfache Leerzeichen entfernen.
        builder.Clear();

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    // --------------------------------------------------
    // Fuzzy Matching / Levenshtein
    // --------------------------------------------------

    private static double CalculateSimilarity(
        string first,
        string second)
    {
        first = NormalizeCity(first);
        second = NormalizeCity(second);

        if (first == second)
            return 1.0;

        if (first.Length == 0 ||
            second.Length == 0)
        {
            return 0.0;
        }

        var distance =
            CalculateLevenshteinDistance(
                first,
                second);

        var maxLength =
            Math.Max(
                first.Length,
                second.Length);

        return 1.0 -
               (double)distance / maxLength;
    }

    private static int CalculateLevenshteinDistance(
        string first,
        string second)
    {
        var rows = first.Length + 1;
        var columns = second.Length + 1;

        var matrix = new int[rows, columns];

        for (var i = 0; i < rows; i++)
            matrix[i, 0] = i;

        for (var j = 0; j < columns; j++)
            matrix[0, j] = j;

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < columns; j++)
            {
                var cost =
                    first[i - 1] == second[j - 1]
                        ? 0
                        : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[
            first.Length,
            second.Length];
    }

    // --------------------------------------------------
    // ISO CountryCode -> CountryName
    // --------------------------------------------------

    private static string? GetCountryName(
        string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return null;

        try
        {
            var region =
                new RegionInfo(
                    countryCode.ToUpperInvariant());

            return region.EnglishName;
        }
        catch
        {
            return null;
        }
    }
}