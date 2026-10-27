namespace AiForwardingAgent.BusinessLogic.CheckPostalCode;

public enum MatchQuality
{
    Exact,
    Similar,
    NoMatch
}

public sealed class CheckPostalCodeResult
{
    public bool IsValid { get; init; }

    public string? CountryCode { get; init; }

    public string? CountryName { get; init; }

    public string? PostalCode { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public MatchQuality MatchQuality { get; init; }

    /// <summary>
    /// Ähnlichkeit zwischen 0 und 1.
    /// Exact = 1.0
    /// </summary>
    public double Similarity { get; init; }

    public string? ErrorMessage { get; init; }
}