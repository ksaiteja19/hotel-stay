using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelStay.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HotelStay.Tests;

public class HotelApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    // Mirrors the JsonStringEnumConverter registered in Program.cs so the test
    // client deserializes "Standard" / "Passport" etc. instead of expecting ints.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public HotelApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // --- Search ---

    [Fact]
    public async Task Search_MissingDestination_Returns400()
    {
        var res = await _client.GetAsync("/hotels/search?checkIn=2025-09-01&checkOut=2025-09-04");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Search_MissingCheckIn_Returns400()
    {
        var res = await _client.GetAsync("/hotels/search?destination=london&checkOut=2025-09-04");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Search_CheckOutBeforeCheckIn_Returns400()
    {
        var res = await _client.GetAsync("/hotels/search?destination=london&checkIn=2025-09-10&checkOut=2025-09-01");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Search_UnknownDestination_Returns400()
    {
        var res = await _client.GetAsync("/hotels/search?destination=atlantis&checkIn=2025-09-01&checkOut=2025-09-04");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Search_ValidRequest_Returns200WithRooms()
    {
        var res = await _client.GetAsync("/hotels/search?destination=london&checkIn=2025-09-01&checkOut=2025-09-04");
        res.EnsureSuccessStatusCode();
        var rooms = await res.Content.ReadFromJsonAsync<List<AvailableRoom>>(JsonOptions);
        Assert.NotNull(rooms);
        Assert.NotEmpty(rooms);
    }

    [Fact]
    public async Task Search_ValidRequest_RoomsFromBothProviders()
    {
        var res = await _client.GetAsync("/hotels/search?destination=paris&checkIn=2025-10-01&checkOut=2025-10-05");
        res.EnsureSuccessStatusCode();
        var rooms = await res.Content.ReadFromJsonAsync<List<AvailableRoom>>(JsonOptions);
        Assert.NotNull(rooms);
        var providers = rooms!.Select(r => r.Provider).Distinct().ToList();
        Assert.Contains("PremierStays", providers);
        // BudgetNests may filter some rooms, but Standard should always be available
        Assert.Contains("BudgetNests", providers);
    }

    [Fact]
    public async Task Search_RoomTypeFilter_OnlyReturnsMatchingRooms()
    {
        var res = await _client.GetAsync("/hotels/search?destination=dubai&checkIn=2025-09-01&checkOut=2025-09-03&roomType=Suite");
        res.EnsureSuccessStatusCode();
        var rooms = await res.Content.ReadFromJsonAsync<List<AvailableRoom>>(JsonOptions);
        Assert.NotNull(rooms);
        Assert.All(rooms!, r => Assert.Equal(RoomType.Suite, r.RoomType));
    }

    [Fact]
    public async Task Search_ResultsOrderedByTotalPriceAscending()
    {
        var res = await _client.GetAsync("/hotels/search?destination=london&checkIn=2025-09-01&checkOut=2025-09-05");
        res.EnsureSuccessStatusCode();
        var rooms = await res.Content.ReadFromJsonAsync<List<AvailableRoom>>(JsonOptions);
        Assert.NotNull(rooms);
        var prices = rooms!.Select(r => r.TotalPrice).ToList();
        Assert.Equal(prices.OrderBy(p => p), prices);
    }

    // --- Reserve ---

    [Fact]
    public async Task Reserve_DomesticWithPassport_Returns201()
    {
        var req = new ReservationRequest(
            "PS-MUMBAI-Standard-001", "PremierStays", "mumbai",
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 3),
            "Raj Patel", DocumentType.Passport, "P12345678");

        var res = await _client.PostAsJsonAsync("/hotels/reserve", req, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var conf = await res.Content.ReadFromJsonAsync<ReservationConfirmation>(JsonOptions);
        Assert.NotNull(conf);
        Assert.StartsWith("SKY-", conf!.ReferenceNumber);
    }

    [Fact]
    public async Task Reserve_DomesticWithNationalId_Returns201()
    {
        var req = new ReservationRequest(
            "BN-DELHI-STD-001", "BudgetNests", "delhi",
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 4),
            "Priya Sharma", DocumentType.NationalId, "UID123456789");

        var res = await _client.PostAsJsonAsync("/hotels/reserve", req, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Reserve_InternationalWithPassport_Returns201()
    {
        var req = new ReservationRequest(
            "PS-LONDON-Standard-042", "PremierStays", "london",
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 5),
            "Alice Smith", DocumentType.Passport, "AB123456");

        var res = await _client.PostAsJsonAsync("/hotels/reserve", req, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Reserve_InternationalWithNationalId_Returns422()
    {
        var req = new ReservationRequest(
            "PS-PARIS-Standard-099", "PremierStays", "paris",
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 3),
            "Bob Jones", DocumentType.NationalId, "NID99999");

        var res = await _client.PostAsJsonAsync("/hotels/reserve", req, JsonOptions);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Reserve_MissingGuestName_Returns400()
    {
        var req = new ReservationRequest(
            "PS-LONDON-Standard-001", "PremierStays", "london",
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 3),
            "", DocumentType.Passport, "AB123456");

        var res = await _client.PostAsJsonAsync("/hotels/reserve", req, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // --- Get reservation ---

    [Fact]
    public async Task GetReservation_AfterReserve_Returns200()
    {
        var req = new ReservationRequest(
            "PS-LONDON-Deluxe-010", "PremierStays", "london",
            new DateOnly(2025, 10, 1), new DateOnly(2025, 10, 4),
            "Eve Brown", DocumentType.Passport, "EV654321");

        var reserveRes = await _client.PostAsJsonAsync("/hotels/reserve", req, JsonOptions);
        reserveRes.EnsureSuccessStatusCode();
        var conf = await reserveRes.Content.ReadFromJsonAsync<ReservationConfirmation>(JsonOptions);

        var getRes = await _client.GetAsync($"/hotels/reservation/{conf!.ReferenceNumber}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var fetched = await getRes.Content.ReadFromJsonAsync<ReservationConfirmation>(JsonOptions);
        Assert.Equal(conf.ReferenceNumber, fetched!.ReferenceNumber);
    }

    [Fact]
    public async Task GetReservation_UnknownReference_Returns404()
    {
        var res = await _client.GetAsync("/hotels/reservation/SKY-NOTFOUND");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // --- Cities ---

    [Fact]
    public async Task GetCities_ReturnsAllDefinedCities()
    {
        var res = await _client.GetAsync("/hotels/cities");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("london", body);
        Assert.Contains("mumbai", body);
    }
}
