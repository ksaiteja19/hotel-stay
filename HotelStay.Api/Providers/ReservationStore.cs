using System.Collections.Concurrent;
using HotelStay.Api.Models;

namespace HotelStay.Api.Providers;

/// <summary>
/// Thread-safe in-memory store for reservations.
/// No persistence — by design (spec: no persistence required).
/// </summary>
public sealed class ReservationStore
{
    private readonly ConcurrentDictionary<string, ReservationConfirmation> _store = new();

    public void Save(ReservationConfirmation confirmation) =>
        _store[confirmation.ReferenceNumber] = confirmation;

    public ReservationConfirmation? Find(string referenceNumber) =>
        _store.TryGetValue(referenceNumber, out var c) ? c : null;

    /// <summary>Generates a reference number in the format SKY-XXXXXXXX (8 hex chars).</summary>
    public static string GenerateReference() =>
        $"SKY-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
