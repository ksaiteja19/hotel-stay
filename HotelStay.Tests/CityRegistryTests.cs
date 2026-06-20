using HotelStay.Api.Models;
using Xunit;

namespace HotelStay.Tests;

public class CityRegistryTests
{
    [Theory]
    [InlineData("mumbai",    DestinationType.Domestic)]
    [InlineData("delhi",     DestinationType.Domestic)]
    [InlineData("bangalore", DestinationType.Domestic)]
    [InlineData("hyderabad", DestinationType.Domestic)]
    [InlineData("london",    DestinationType.International)]
    [InlineData("newyork",   DestinationType.International)]
    [InlineData("paris",     DestinationType.International)]
    [InlineData("dubai",     DestinationType.International)]
    public void TryGetType_KnownCity_ReturnsCorrectType(string city, DestinationType expected)
    {
        var found = CityRegistry.TryGetType(city, out var actual);
        Assert.True(found);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryGetType_UnknownCity_ReturnsFalse()
    {
        Assert.False(CityRegistry.TryGetType("atlantis", out _));
    }

    [Theory]
    [InlineData("mumbai",    DocumentType.NationalId, null)]
    [InlineData("mumbai",    DocumentType.Passport,   null)]
    [InlineData("delhi",     DocumentType.NationalId, null)]
    [InlineData("london",    DocumentType.Passport,   null)]
    [InlineData("paris",     DocumentType.Passport,   null)]
    public void ValidateDocument_ValidCombinations_ReturnsNull(
        string destination, DocumentType docType, string? expectedError)
    {
        Assert.Equal(expectedError, CityRegistry.ValidateDocument(destination, docType));
    }

    [Theory]
    [InlineData("london",  DocumentType.NationalId)]
    [InlineData("newyork", DocumentType.NationalId)]
    [InlineData("dubai",   DocumentType.NationalId)]
    public void ValidateDocument_NationalIdForInternational_ReturnsError(
        string destination, DocumentType docType)
    {
        var error = CityRegistry.ValidateDocument(destination, docType);
        Assert.NotNull(error);
        Assert.Contains("Passport", error);
    }

    [Fact]
    public void ValidateDocument_UnknownDestination_ReturnsError()
    {
        var error = CityRegistry.ValidateDocument("atlantis", DocumentType.Passport);
        Assert.NotNull(error);
        Assert.Contains("Unknown destination", error);
    }
}
