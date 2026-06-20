namespace HotelStay.Api.Models;

public enum RoomType { Standard, Deluxe, Suite }

public enum CancellationPolicy { FreeCancellation, Flexible, NonRefundable }

public enum DocumentType { Passport, NationalId }

public enum DestinationType { Domestic, International }

public record AvailableRoom(
    string RoomId,
    string Provider,
    RoomType RoomType,
    decimal RatePerNight,
    decimal TotalPrice,
    int Nights,
    CancellationPolicy CancellationPolicy,
    int? StarRating,
    IReadOnlyList<string>? Amenities
);

public record ReservationRequest(
    string RoomId,
    string Provider,
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    string GuestName,
    DocumentType DocumentType,
    string DocumentNumber
);

public record ReservationConfirmation(
    string ReferenceNumber,
    string Provider,
    RoomType RoomType,
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    string GuestName,
    decimal TotalPrice,
    CancellationPolicy CancellationPolicy
);
