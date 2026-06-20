using HotelStay.Api.Models;
using HotelStay.Api.Providers;
using Xunit;

namespace HotelStay.Tests;

public class PremierStaysProviderTests
{
    private readonly PremierStaysProvider _provider = new();

    [Fact]
    public async Task Search_ReturnsRoomsForAllTypes_WhenNoFilter()
    {
        var rooms = (await _provider.SearchAsync("london", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 4), null)).ToList();
        Assert.Equal(3, rooms.Count);
        Assert.Contains(rooms, r => r.RoomType == RoomType.Standard);
        Assert.Contains(rooms, r => r.RoomType == RoomType.Deluxe);
        Assert.Contains(rooms, r => r.RoomType == RoomType.Suite);
    }

    [Fact]
    public async Task Search_FiltersToRequestedRoomType()
    {
        var rooms = (await _provider.SearchAsync("london", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 4), RoomType.Deluxe)).ToList();
        Assert.All(rooms, r => Assert.Equal(RoomType.Deluxe, r.RoomType));
    }

    [Fact]
    public async Task Search_TotalPriceEqualsRateTimesNights()
    {
        var rooms = (await _provider.SearchAsync("paris", new DateOnly(2025, 10, 1), new DateOnly(2025, 10, 5), null)).ToList();
        Assert.All(rooms, r => Assert.Equal(r.RatePerNight * 4, r.TotalPrice));
    }

    [Fact]
    public async Task Search_AllRoomsHaveStarRatingAndAmenities()
    {
        var rooms = (await _provider.SearchAsync("dubai", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 3), null)).ToList();
        Assert.All(rooms, r =>
        {
            Assert.NotNull(r.StarRating);
            Assert.NotNull(r.Amenities);
            Assert.NotEmpty(r.Amenities!);
        });
    }

    [Fact]
    public async Task Search_IsDeterministic()
    {
        var r1 = await _provider.SearchAsync("mumbai", new DateOnly(2025, 7, 1), new DateOnly(2025, 7, 3), null);
        var r2 = await _provider.SearchAsync("mumbai", new DateOnly(2025, 7, 1), new DateOnly(2025, 7, 3), null);
        Assert.Equal(
            r1.Select(r => r.RatePerNight),
            r2.Select(r => r.RatePerNight));
    }

    [Fact]
    public async Task Search_ProviderNameIsCorrect()
    {
        var rooms = (await _provider.SearchAsync("london", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 2), null)).ToList();
        Assert.All(rooms, r => Assert.Equal("PremierStays", r.Provider));
    }
}

public class BudgetNestsProviderTests
{
    private readonly BudgetNestsProvider _provider = new();

    [Fact]
    public async Task Search_NeverReturnsMoreThanThreeRoomsPerSearch()
    {
        // Raw stub always generates 3 candidate rooms (standard/deluxe/suite);
        // after filtering unavailable ones, count must never exceed that.
        foreach (var city in new[] { "london", "paris", "dubai", "newyork", "mumbai", "delhi" })
        {
            var rooms = (await _provider.SearchAsync(city, new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 3), null)).ToList();
            Assert.True(rooms.Count <= 3, $"{city} returned {rooms.Count} rooms, expected at most 3");
            Assert.True(rooms.Count >= 1, $"{city} returned no rooms — standard should always be available");
        }
    }

    [Fact]
    public async Task Search_StandardRoomIsAlwaysAvailable()
    {
        // By stub design, standard is always available=true regardless of seed
        foreach (var city in new[] { "london", "paris", "dubai", "newyork" })
        {
            var rooms = (await _provider.SearchAsync(city, new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 3), RoomType.Standard)).ToList();
            Assert.Single(rooms);
        }
    }

    [Fact]
    public async Task Search_AcrossManySeedsAtLeastOneRoomTypeGetsFiltered()
    {
        // Statistical check across many distinct seeds: deluxe/suite have a
        // designed chance of unavailability, so over enough searches we
        // should observe at least one search returning fewer than 3 rooms.
        bool sawFilteredSearch = false;
        for (int i = 0; i < 30; i++)
        {
            var checkIn = new DateOnly(2025, 1, 1).AddDays(i);
            var rooms = await _provider.SearchAsync($"city{i}", checkIn, checkIn.AddDays(2), null);
            if (rooms.Count() < 3)
            {
                sawFilteredSearch = true;
                break;
            }
        }
        Assert.True(sawFilteredSearch, "Expected at least one search out of 30 to have a filtered (unavailable) room");
    }

    [Fact]
    public async Task Search_NoBudgetNestsRoomHasAmenitiesOrStarRating()
    {
        var rooms = (await _provider.SearchAsync("delhi", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 4), null)).ToList();
        Assert.All(rooms, r =>
        {
            Assert.Null(r.StarRating);
            Assert.Null(r.Amenities);
        });
    }

    [Fact]
    public async Task Search_PoliciesAreFlexibleOrNonRefundable()
    {
        var rooms = (await _provider.SearchAsync("london", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 4), null)).ToList();
        Assert.All(rooms, r =>
            Assert.True(r.CancellationPolicy == CancellationPolicy.Flexible ||
                        r.CancellationPolicy == CancellationPolicy.NonRefundable));
    }

    [Fact]
    public async Task Search_TotalPriceEqualsRateTimesNights()
    {
        var rooms = (await _provider.SearchAsync("mumbai", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 6), null)).ToList();
        Assert.All(rooms, r => Assert.Equal(r.RatePerNight * 5, r.TotalPrice));
    }

    [Fact]
    public async Task Search_ProviderNameIsCorrect()
    {
        var rooms = (await _provider.SearchAsync("delhi", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 2), null)).ToList();
        Assert.All(rooms, r => Assert.Equal("BudgetNests", r.Provider));
    }
}

public class ReservationStoreTests
{
    [Fact]
    public void Save_And_Find_RoundTrip()
    {
        var store = new ReservationStore();
        var confirmation = new ReservationConfirmation(
            "SKY-ABCD1234", "PremierStays", RoomType.Deluxe, "london",
            new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 4), "Alice",
            510m, CancellationPolicy.FreeCancellation);

        store.Save(confirmation);
        var found = store.Find("SKY-ABCD1234");

        Assert.Equal(confirmation, found);
    }

    [Fact]
    public void Find_UnknownReference_ReturnsNull()
    {
        var store = new ReservationStore();
        Assert.Null(store.Find("SKY-NOTEXIST"));
    }

    [Fact]
    public void GenerateReference_StartsWithSky_And_IsUnique()
    {
        var refs = Enumerable.Range(0, 100).Select(_ => ReservationStore.GenerateReference()).ToList();
        Assert.All(refs, r => Assert.StartsWith("SKY-", r));
        Assert.Equal(100, refs.Distinct().Count());
    }
}
