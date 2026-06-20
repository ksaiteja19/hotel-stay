using HotelStay.Api.Models;

namespace HotelStay.Api.Providers;

public interface IHotelProvider
{
    string ProviderName { get; }

    Task<IEnumerable<AvailableRoom>> SearchAsync(
        string destination,
        DateOnly checkIn,
        DateOnly checkOut,
        RoomType? roomType,
        CancellationToken ct = default);
}
