using HotelStay.Api.Models;
using HotelStay.Api.Providers;
using Microsoft.AspNetCore.Mvc;

namespace HotelStay.Api.Endpoints;

public static class HotelEndpoints
{
    public static IEndpointRouteBuilder MapHotelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/hotels/search", SearchAsync)
           .WithName("SearchHotels")
           .WithOpenApi();

        app.MapPost("/hotels/reserve", ReserveAsync)
           .WithName("ReserveRoom")
           .WithOpenApi();

        app.MapGet("/hotels/reservation/{reference}", GetReservationAsync)
           .WithName("GetReservation")
           .WithOpenApi();

        app.MapGet("/hotels/cities", GetCities)
           .WithName("GetCities")
           .WithOpenApi();

        return app;
    }

    private static async Task<IResult> SearchAsync(
        [FromQuery] string? destination,
        [FromQuery] string? checkIn,
        [FromQuery] string? checkOut,
        [FromQuery] string? roomType,
        IEnumerable<IHotelProvider> providers,
        CancellationToken ct)
    {
        // Validate required params
        if (string.IsNullOrWhiteSpace(destination))
            return Results.BadRequest(new { error = "Missing required parameter: destination." });
        if (string.IsNullOrWhiteSpace(checkIn))
            return Results.BadRequest(new { error = "Missing required parameter: checkIn." });
        if (string.IsNullOrWhiteSpace(checkOut))
            return Results.BadRequest(new { error = "Missing required parameter: checkOut." });

        if (!CityRegistry.TryGetType(destination, out _))
            return Results.BadRequest(new { error = $"Unknown destination '{destination}'. Use /hotels/cities for valid values." });

        if (!DateOnly.TryParse(checkIn, out var checkInDate))
            return Results.BadRequest(new { error = "Invalid checkIn date format. Use YYYY-MM-DD." });
        if (!DateOnly.TryParse(checkOut, out var checkOutDate))
            return Results.BadRequest(new { error = "Invalid checkOut date format. Use YYYY-MM-DD." });
        if (checkOutDate <= checkInDate)
            return Results.BadRequest(new { error = "checkOut must be after checkIn." });

        RoomType? parsedRoomType = null;
        if (!string.IsNullOrWhiteSpace(roomType))
        {
            if (!Enum.TryParse<RoomType>(roomType, ignoreCase: true, out var rt))
                return Results.BadRequest(new { error = $"Invalid roomType '{roomType}'. Valid values: Standard, Deluxe, Suite." });
            parsedRoomType = rt;
        }

        // Fan out to all providers concurrently
        var tasks = providers.Select(p => p.SearchAsync(destination, checkInDate, checkOutDate, parsedRoomType, ct));
        var results = await Task.WhenAll(tasks);

        var rooms = results
            .SelectMany(r => r)
            .OrderBy(r => r.TotalPrice)
            .ToList();

        return Results.Ok(rooms);
    }

    private static IResult ReserveAsync(
        [FromBody] ReservationRequest? request,
        ReservationStore store)
    {
        if (request is null)
            return Results.BadRequest(new { error = "Request body is required." });

        if (string.IsNullOrWhiteSpace(request.RoomId))
            return Results.BadRequest(new { error = "RoomId is required." });
        if (string.IsNullOrWhiteSpace(request.Provider))
            return Results.BadRequest(new { error = "Provider is required." });
        if (string.IsNullOrWhiteSpace(request.Destination))
            return Results.BadRequest(new { error = "Destination is required." });
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return Results.BadRequest(new { error = "GuestName is required." });
        if (string.IsNullOrWhiteSpace(request.DocumentNumber))
            return Results.BadRequest(new { error = "DocumentNumber is required." });
        if (request.CheckOut <= request.CheckIn)
            return Results.BadRequest(new { error = "checkOut must be after checkIn." });

        // Server-side document validation
        var docError = CityRegistry.ValidateDocument(request.Destination, request.DocumentType);
        if (docError is not null)
            return Results.UnprocessableEntity(new { error = docError });

        // Derive room type from roomId convention (PS-DEST-RoomType-xxx / BN-DEST-RoomType-xxx)
        var roomType = DeriveRoomType(request.RoomId);
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        // Deterministic rate from provider name + roomId (mirrors stub logic)
        var rate = DeriveRate(request.Provider, request.RoomId, roomType);
        var totalPrice = rate * nights;

        var cancellationPolicy = DeriveCancellationPolicy(request.Provider, request.RoomId);

        var confirmation = new ReservationConfirmation(
            ReferenceNumber: ReservationStore.GenerateReference(),
            Provider: request.Provider,
            RoomType: roomType,
            Destination: request.Destination,
            CheckIn: request.CheckIn,
            CheckOut: request.CheckOut,
            GuestName: request.GuestName,
            TotalPrice: totalPrice,
            CancellationPolicy: cancellationPolicy
        );

        store.Save(confirmation);
        return Results.Created($"/hotels/reservation/{confirmation.ReferenceNumber}", confirmation);
    }

    private static IResult GetReservationAsync(
        string reference,
        ReservationStore store)
    {
        var confirmation = store.Find(reference);
        return confirmation is not null
            ? Results.Ok(confirmation)
            : Results.NotFound(new { error = $"Reservation '{reference}' not found." });
    }

    private static IResult GetCities()
    {
        var cities = CityRegistry.All.Select(kv => new
        {
            code = kv.Key,
            name = ToDisplayName(kv.Key),
            type = kv.Value.ToString()
        }).OrderBy(c => c.type).ThenBy(c => c.name);

        return Results.Ok(cities);
    }

    // --- helpers ---

    private static RoomType DeriveRoomType(string roomId)
    {
        var upper = roomId.ToUpperInvariant();
        if (upper.Contains("-STD-") || upper.Contains("-STANDARD-")) return RoomType.Standard;
        if (upper.Contains("-DLX-") || upper.Contains("-DELUXE-"))   return RoomType.Deluxe;
        if (upper.Contains("-STE-") || upper.Contains("-SUITE-"))    return RoomType.Suite;
        return RoomType.Standard;
    }

    private static decimal DeriveRate(string provider, string roomId, RoomType roomType) =>
        provider switch
        {
            "PremierStays" => roomType switch
            {
                RoomType.Standard => 100m,
                RoomType.Deluxe   => 170m,
                RoomType.Suite    => 290m,
                _                 => 100m
            },
            _ => roomType switch       // BudgetNests
            {
                RoomType.Standard => 75m,
                RoomType.Deluxe   => 130m,
                RoomType.Suite    => 220m,
                _                 => 75m
            }
        };

    private static CancellationPolicy DeriveCancellationPolicy(string provider, string roomId) =>
        provider == "PremierStays"
            ? CancellationPolicy.FreeCancellation
            : CancellationPolicy.Flexible;

    private static string ToDisplayName(string code) => code switch
    {
        "newyork"   => "New York",
        "mumbai"    => "Mumbai",
        "delhi"     => "Delhi",
        "bangalore" => "Bangalore",
        "hyderabad" => "Hyderbad",
        "london"    => "London",
        "paris"     => "Paris",
        "dubai"     => "Dubai",
        _           => code
    };
}
