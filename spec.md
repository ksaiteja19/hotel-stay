# Hotel Stay – Specification

> This file defines all data models, enumerations, interface contracts, and validation rules.
> It is committed **before** any implementation files.

---

## 1. Domain Enumerations

### RoomType
```
Standard | Deluxe | Suite
```

### CancellationPolicy
```
FreeCancellation   – cancellable up to 48 h before check-in (PremierStays)
Flexible           – cancellable up to 24 h before check-in (BudgetNests)
NonRefundable      – no cancellation
```

### DocumentType
```
Passport   – required for international destinations
NationalId – accepted for domestic destinations
```

---

## 2. Cities

### Domestic (National ID accepted)
| City         | Code        |
|--------------|-------------|
| Mumbai       | `mumbai`    |
| Delhi        | `delhi`     |
| Bangalore    | `bangalore` |
| Hyderabad    | `Hyderabad` |

### International (Passport required)
| City         | Code        |
|--------------|-------------|
| London       | `london`    |
| New York     | `newyork`   |
| Paris        | `paris`     |
| Dubai        | `dubai`     |

---

## 3. Unified Data Models

### AvailableRoom
```jsonc
{
  "roomId": "string",           // provider-specific unique id
  "provider": "PremierStays" | "BudgetNests",
  "roomType": "Standard" | "Deluxe" | "Suite",
  "ratePerNight": 120.00,       // numeric, 2 dp
  "totalPrice": 360.00,         // ratePerNight × nights
  "nights": 3,
  "cancellationPolicy": "FreeCancellation" | "Flexible" | "NonRefundable",
  // PremierStays only – null for BudgetNests
  "starRating": 4 | null,
  "amenities": ["WiFi", "Pool"] | null
}
```

### ReservationRequest
```jsonc
{
  "roomId": "string",
  "provider": "PremierStays" | "BudgetNests",
  "destination": "string",
  "checkIn": "2025-09-01",      // ISO 8601 date
  "checkOut": "2025-09-04",
  "guestName": "string",
  "documentType": "Passport" | "NationalId",
  "documentNumber": "string"
}
```

### ReservationConfirmation
```jsonc
{
  "referenceNumber": "SKY-XXXXXXXX",   // SKY- + 8 hex chars
  "provider": "PremierStays" | "BudgetNests",
  "roomType": "Standard" | "Deluxe" | "Suite",
  "destination": "string",
  "checkIn": "2025-09-01",
  "checkOut": "2025-09-04",
  "guestName": "string",
  "totalPrice": 360.00,
  "cancellationPolicy": "FreeCancellation" | "Flexible" | "NonRefundable"
}
```

---

## 4. Interface Contract – IHotelProvider

```csharp
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
```

Implementations: `PremierStaysProvider`, `BudgetNestsProvider`

---

## 5. API Endpoints

### GET /hotels/search
Query params:
- `destination` (required) – city code
- `checkIn` (required) – ISO 8601 date
- `checkOut` (required) – ISO 8601 date
- `roomType` (optional) – Standard | Deluxe | Suite

Responses:
- `200` – array of `AvailableRoom` (merged, filtered, normalised)
- `400` – missing required param or checkOut ≤ checkIn
- `400` – unknown destination

### POST /hotels/reserve
Body: `ReservationRequest`

Responses:
- `201` – `ReservationConfirmation`
- `400` – missing/invalid body fields
- `422` – document type mismatch (e.g. NationalId for international city)

### GET /hotels/reservation/{reference}
Responses:
- `200` – `ReservationConfirmation`
- `404` – reference not found

---

## 6. Document Validation Rules

| Destination type | Accepted document      | Rejected document |
|------------------|------------------------|-------------------|
| Domestic         | NationalId **or** Passport | –               |
| International    | Passport only          | NationalId        |

Server returns HTTP 422 with body:
```jsonc
{
  "error": "Document mismatch: international destinations require a Passport."
}
```

---

## 7. Provider Stub Behaviour

### PremierStays (PascalCase JSON, always available)
- Returns 2–3 rooms per search
- Includes StarRating, Amenities
- CancellationPolicy: `FreeCancellation` or `NonRefundable`

### BudgetNests (snake_case JSON, may be unavailable)
- Returns 2–3 rooms, some with `available: false` (filtered server-side)
- No amenities/star rating
- CancellationPolicy: `Flexible` or `NonRefundable`

---

## 8. Extensibility Note

A third provider is added by:
1. Implementing `IHotelProvider`
2. Registering in DI
3. No changes to search endpoint or normalisation logic
