using HotelStay.Api.Models;

namespace HotelStay.Api.Providers;

/// <summary>
/// Stub for BudgetNests. Minimal detail (rate + policy only). snake_case format simulated internally.
/// Some rooms returned with available=false — these are filtered out before returning.
/// Deterministic seeding on destination + dates.
/// </summary>
public sealed class BudgetNestsProvider : IHotelProvider
{
    public string ProviderName => "BudgetNests";

    // Represents the raw snake_case response BudgetNests would return
    private sealed record BudgetNestsRawRoom(
        string room_id,
        string room_type,
        decimal rate_per_night,
        string cancellation_policy,
        bool available
    );

    public Task<IEnumerable<AvailableRoom>> SearchAsync(
        string destination,
        DateOnly checkIn,
        DateOnly checkOut,
        RoomType? roomType,
        CancellationToken ct = default)
    {
        int nights = checkOut.DayNumber - checkIn.DayNumber;
        int seed = Math.Abs(destination.GetHashCode() ^ checkOut.GetHashCode() ^ 0xDEAD);
        var rng = new Random(seed);

        // Simulate raw stub responses (snake_case with available flag)
        var rawRooms = new List<BudgetNestsRawRoom>
        {
            new($"BN-{destination.ToUpperInvariant()}-STD-{seed % 100:D3}", "standard",
                999 + rng.Next(0, 30), "flexible",       available: true),
            new($"BN-{destination.ToUpperInvariant()}-DLX-{seed % 100:D3}", "deluxe",
                1499 + rng.Next(0, 40), "non_refundable", available: rng.Next(0, 3) != 0),
            new($"BN-{destination.ToUpperInvariant()}-STE-{seed % 100:D3}", "suite",
                1999 + rng.Next(0, 60), "flexible",       available: rng.Next(0, 4) != 0),
        };

        // Normalise and filter
        var results = rawRooms
            .Where(r => r.available)
            .Select(r => Normalise(r, nights))
            .Where(r => roomType == null || r.RoomType == roomType);

        return Task.FromResult(results);
    }

    private AvailableRoom Normalise(BudgetNestsRawRoom raw, int nights)
    {
        var roomType = raw.room_type.ToLowerInvariant() switch
        {
            "standard" => RoomType.Standard,
            "deluxe"   => RoomType.Deluxe,
            "suite"    => RoomType.Suite,
            _          => RoomType.Standard
        };

        var policy = raw.cancellation_policy.ToLowerInvariant() switch
        {
            "flexible"       => CancellationPolicy.Flexible,
            "non_refundable" => CancellationPolicy.NonRefundable,
            _                => CancellationPolicy.NonRefundable
        };

        return new AvailableRoom(
            RoomId: raw.room_id,
            Provider: ProviderName,
            RoomType: roomType,
            RatePerNight: raw.rate_per_night,
            TotalPrice: raw.rate_per_night * nights,
            Nights: nights,
            CancellationPolicy: policy,
            StarRating: null,
            Amenities: null
        );
    }
}
