namespace HotelStay.Api.Models;

public static class CityRegistry
{
    private static readonly Dictionary<string, DestinationType> _cities = new(StringComparer.OrdinalIgnoreCase)
    {
        // Domestic
        { "mumbai",    DestinationType.Domestic },
        { "delhi",     DestinationType.Domestic },
        { "bangalore", DestinationType.Domestic },
        { "hyderabad", DestinationType.Domestic },
        // International
        { "london",   DestinationType.International },
        { "newyork",  DestinationType.International },
        { "paris",    DestinationType.International },
        { "dubai",    DestinationType.International },
    };

    public static bool TryGetType(string city, out DestinationType type) =>
        _cities.TryGetValue(city, out type);

    public static IReadOnlyDictionary<string, DestinationType> All => _cities;

    /// <summary>
    /// Returns null if valid, or an error message if the document is not accepted for this destination.
    /// Domestic: both Passport and NationalId accepted.
    /// International: only Passport accepted.
    /// </summary>
    public static string? ValidateDocument(string destination, DocumentType documentType)
    {
        if (!TryGetType(destination, out var destType))
            return $"Unknown destination '{destination}'.";

        if (destType == DestinationType.International && documentType == DocumentType.NationalId)
            return "Document mismatch: international destinations require a Passport.";

        return null; // valid
    }
}
