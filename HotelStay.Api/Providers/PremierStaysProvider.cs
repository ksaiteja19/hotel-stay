using HotelStay.Api.Models;

namespace HotelStay.Api.Providers;

/// <summary>
/// Stub for PremierStays. Returns full-detail rooms in PascalCase (simulated internally).
/// Always available. Deterministic — seeded on destination + dates.
/// </summary>
public sealed class PremierStaysProvider : IHotelProvider
{
    public string ProviderName => "PremierStays";

    private static readonly string[][] _amenitySets =
    [
        ["WiFi", "Breakfast", "Pool"],
        ["WiFi", "Gym"],
        ["WiFi", "Spa", "Breakfast", "Parking"],
        ["WiFi", "Pool", "Bar"],
    ];

    public Task<IEnumerable<AvailableRoom>> SearchAsync(
        string destination,
        DateOnly checkIn,
        DateOnly checkOut,
        RoomType? roomType,
        CancellationToken ct = default)
    {
        int nights = checkOut.DayNumber - checkIn.DayNumber;
        int seed = Math.Abs(destination.GetHashCode() ^ checkIn.GetHashCode());
        var rng = new Random(seed);

        RoomType[] roomTypes = roomType.HasValue
            ? [roomType.Value]
            : [RoomType.Standard, RoomType.Deluxe, RoomType.Suite];

        var results = roomTypes.Select((rt, idx) =>
        {
            decimal rate = rt switch
            {
                RoomType.Standard => 1299 + rng.Next(0, 40),
                RoomType.Deluxe   => 1799 + rng.Next(0, 60),
                RoomType.Suite    => 2499 + rng.Next(0, 100),
                _                 => 100
            };

            var policy = idx % 2 == 0
                ? CancellationPolicy.FreeCancellation
                : CancellationPolicy.NonRefundable;

            int starRating = 3 + (idx % 3);
            var amenities = (IReadOnlyList<string>)_amenitySets[(seed + idx) % _amenitySets.Length];

            return new AvailableRoom(
                RoomId: $"PS-{destination.ToUpperInvariant()}-{rt}-{seed % 1000:D3}",
                Provider: ProviderName,
                RoomType: rt,
                RatePerNight: rate,
                TotalPrice: rate * nights,
                Nights: nights,
                CancellationPolicy: policy,
                StarRating: starRating,
                Amenities: amenities
            );
        });

        return Task.FromResult(results);
    }
}
