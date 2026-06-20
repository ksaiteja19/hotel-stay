# SkyRoute — Hotel Stay Availability

A hotel availability and reservation feature that queries two simulated providers
(PremierStays, BudgetNests), normalises their results into a unified format, and
lets a traveller reserve a room with document validation.

```
hotel-stay/
├── README.md              # you are here
├── spec.md                # data models & interface contracts (committed pre-implementation)
├── HotelStay.Api/         # .NET 8 Minimal API
├── HotelStay.Tests/       # xUnit unit + integration tests
├── hotel-stay-ui/         # React + TypeScript frontend
├── prompts.md             # AI prompts used, with notes on decisions
└── reflection.md          # what I'd improve with more time
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Node.js 18+](https://nodejs.org/) and npm

## Run the backend

```bash
cd HotelStay.Api
dotnet run
```

The API starts on **http://localhost:5000** (configured in `appsettings.json`).
Automatically redirects to `http://localhost:5000/index.html` for the Swagger document in
development mode.

### Run the tests

```bash
cd HotelStay.Tests
dotnet test
```

Covers provider stub behaviour (determinism, filtering, normalisation), document
validation rules, and full HTTP-level integration tests for all three endpoints
(search, reserve, get-reservation), including every validation error path.

## Run the frontend

```bash
cd hotel-stay-ui
npm install
npm run dev
```

Opens on **http://localhost:5173**. The dev server is pre-configured (in
`vite.config.ts` and CORS on the API) to talk to the backend at
`http://localhost:5000`. To point at a different API URL, copy `.env.example`
to `.env` and set `VITE_API_URL`.

## Quick smoke test

1. Start the API (`dotnet run` in `HotelStay.Api`).
2. Start the UI (`npm run dev` in `hotel-stay-ui`).
3. Search **Mumbai**, any dates, any room type → results from both providers.
4. Reserve a room with **National ID** → succeeds (domestic).
5. Search **London** instead, try to reserve with **National ID** → rejected
   with a 422 and a clear message; switch to **Passport** → succeeds.
6. Confirmation screen shows a `SKY-XXXXXXXX` reference number; refreshing
   `GET /hotels/reservation/{reference}` returns the same details (in-memory,
   so it resets if the API restarts).

## Assumptions

- **Cities**: 4 domestic (Mumbai, Delhi, Bangalore, Hyderbad) and 4 international
  (London, New York, Paris, Dubai) are hard-coded in `CityRegistry`. The brief
  asked for at least 2 domestic / 3 international — I added four domestic
  city for symmetry with the destination dropdown.
- **No persistence**: reservations live in an in-memory, thread-safe
  dictionary (`ReservationStore`), per the brief's explicit scope exclusion.
  They're lost on API restart — this is by design, not an oversight.
- **Currency**: rates are presented in ₹ (INR) throughout, since no currency
  was specified and the brief's domestic cities are Indian.
- **Domestic + Passport**: a traveller going domestic may still present a
  Passport (it's a strictly higher-trust document); only "National ID for an
  international destination" is rejected.
- **Stub determinism**: both providers seed their `Random` from a hash of
  `(destination, dates)`, so the same search always returns the same rooms —
  important for demoing and for repeatable tests — while still varying
  output across different searches.
- **BudgetNests availability**: ~33% chance each of Deluxe/Suite being
  unavailable per search (Standard is always available), simulating the
  spec's "may return `available: false`" behaviour deterministically per seed.
- **Room IDs encode room type** (e.g. `PS-LONDON-Deluxe-042`) so the reserve
  endpoint can validate/derive details without a live search-session cache —
  kept deliberately simple for a stub-only system with no persistence.

## Extending with a third provider

Implement `IHotelProvider` (see `spec.md` §4) and register it in
`Program.cs`:

```csharp
builder.Services.AddSingleton<IHotelProvider, YourNewProvider>();
```

No changes needed to the search endpoint, normalisation logic, or frontend —
the unified `AvailableRoom` shape and provider fan-out already handle N
providers.
