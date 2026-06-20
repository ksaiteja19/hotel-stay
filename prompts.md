# AI Prompts & Decisions

This file captures the significant prompts used while building this project and the
judgement calls made along the way, per the brief's AI Tooling Guidelines.

## 1. Analysis & spec design

**Prompt (paraphrased):** "Given this brief [pasted], design `spec.md` before writing
any code — unified data models for AvailableRoom/ReservationRequest/Confirmation,
the IHotelProvider interface, city list, and document validation rules."

**Decision:** Chose a single unified `AvailableRoom` record with nullable
`StarRating`/`Amenities` rather than two separate room DTOs (one per provider) merged
at the API layer. This keeps the frontend ignorant of provider-specific shapes — it
only ever sees one type — and makes adding a third provider purely additive: any new
provider just needs to map its raw response into the same `AvailableRoom` shape.

**Decision:** Room IDs encode provider + destination + room type (e.g.
`PS-LONDON-Deluxe-042`). Since there's no persistence or session/cart concept, the
reserve endpoint needs *some* way to recover room details (rate, room type) from just
a `roomId` + `provider` pair without re-querying the provider. Encoding type in the ID
is a deliberate stub-system simplification — flagged honestly in `reflection.md` as
something a real system wouldn't do (a real system would look up the room by ID
against the actual provider, or cache the search result server-side).

## 2. Provider stub design

**Prompt (paraphrased):** "Write two IHotelProvider stub implementations.
PremierStays: PascalCase, full detail, always available. BudgetNests: snake_case,
minimal detail, sometimes unavailable. Both must be deterministic for a given
search and cover representative scenarios."

**Decision:** Used a `Random` seeded from a hash of `(destination, dates)` rather
than a fixed/hardcoded room catalogue. This gives believable variation across
different searches (rates differ between London and Mumbai, dates 3 nights vs 5
nights) while remaining perfectly reproducible for the same inputs — important both
for demoing live and for the determinism unit tests.

**Decision:** BudgetNests' "may return available: false" requirement is modelled with
fixed odds per room type (Standard always available, Deluxe ~33% unavailable, Suite
~25% unavailable) rather than a manually curated fixture list. Asked the AI tool to
sanity-check the odds wouldn't make tests flaky — confirmed the probability of zero
filtering across 30 distinct seeds is astronomically small, so the "at least one
filtered search" test is reliable without being hardcoded to a magic seed value.

## 3. Document validation

**Prompt (paraphrased):** "International destinations require Passport; domestic
accepts National ID. Validate both client and server side, 422 on mismatch."

**Decision:** Allowed Passport for domestic destinations too (a passport is a
strictly higher-trust document than a national ID), only rejecting "National ID for
an international destination." This wasn't explicit in the brief; flagged as an
assumption in README.md rather than silently building it in.

## 4. Catching JSON serialization/deserialization bugs

**Prompt (paraphrased, mid-build):** "Review Program.cs and the test project for
enum serialization issues — minimal APIs use System.Text.Json by default, which
serializes enums as integers, not strings."

The first draft had `RoomType`/`CancellationPolicy`/`DocumentType` as plain C# enums
with no `JsonStringEnumConverter` configured. That would have shipped numeric enum
values (`0`, `1`, `2`) to the frontend, which expects string literals (`"Standard"`,
`"FreeCancellation"`) per `spec.md` and the TypeScript types. Fixed by registering
`JsonStringEnumConverter` via `ConfigureHttpJsonOptions` in `Program.cs`.

**A second, more serious bug in the same area surfaced only when the tests were
actually run** (this environment couldn't run `dotnet test` directly — see
`reflection.md` §1 — so this one wasn't caught until the person running the
challenge executed it themselves and shared the failure output). The integration
test client's shared `JsonSerializerOptions` registered the enum converter but never
set `PropertyNameCaseInsensitive = true`. ASP.NET's default HTTP JSON options
serialize to **camelCase** (`roomId`, `ratePerNight`), but the C# records use
**PascalCase** properties (`RoomId`, `RatePerNight`), and `System.Text.Json` is
case-sensitive by default. Every non-enum property was silently deserializing to its
type's default (`""`, `0`, `null`) instead of throwing — which is the genuinely
dangerous part: `Search_RoomTypeFilter_OnlyReturnsMatchingRooms` failed loudly (every
room showed `RoomType = Standard` instead of `Suite`, because the JSON's `roomType`
key never bound to anything), but `Search_ResultsOrderedByTotalPriceAscending` was
passing for the wrong reason — `TotalPrice` deserializing to `0` for every room makes
`[0,0,0,0]` trivially "sorted." A false-positive green test is worse than a failing
one. Fixed with `PropertyNameCaseInsensitive = true` on the shared test options
object, which covers both directions regardless of which side's naming policy
changes later.

**Takeaway documented for real:** a model-level review (reading the code) caught the
enum-as-integer issue, but the case-sensitivity issue could only be caught by
actually executing the tests against a real server — config-shaped bugs like JSON
option mismatches between a server and its test client are exactly the category that
survives code review and only shows up at runtime.

## 5. C# collection-expression ternary bug

**Caught during self-review (not from a specific prompt):** the first draft of
`PremierStaysProvider.SearchAsync` had:
```csharp
var roomTypes = roomType.HasValue
    ? [roomType.Value]
    : new[] { RoomType.Standard, RoomType.Deluxe, RoomType.Suite };
```
Mixing a C# 12 collection expression (`[...]`) with `new[] {...}` across a ternary's
two branches has no common natural type for `var` to infer, since collection
expressions need a target type. Fixed by declaring the variable as `RoomType[]`
explicitly so both branches have something to convert to.

## 6. Frontend design direction

**Prompt (paraphrased):** "Design the result cards around a coherent visual idea
rather than generic SaaS cards — something that reflects the 'travel document'
nature of a hotel booking, and makes the provider asymmetry (full vs minimal detail)
visually legible."

**Decision:** Landed on a boarding-pass motif — each room result is a ticket-style
card with a perforated divider between the descriptive section (provider, amenities,
policy) and a torn-off price stub. This gave a natural home for the
PremierStays-vs-BudgetNests detail asymmetry: PremierStays cards show star ratings
and amenity chips in the body; BudgetNests cards show an italic "rate & cancellation
policy only" line instead, which is honest about what the provider actually returns
rather than padding it out.

## 7. General AI usage notes

- Used AI assistance across the full SDLC as instructed: drafting `spec.md` before
  any code, generating the C# domain models/providers/endpoints, generating the
  React components and design system, and generating the xUnit test suite (unit +
  integration).
- Every AI-generated file was read and hand-traced for compile correctness in this
  session, since no `dotnet`/network access was available to actually run `dotnet
  build` / `dotnet test` / `npm install` in this environment. Two real bugs were
  caught this way (the collection-expression ternary, and the enum JSON
  serialization mismatch) — both are documented above and in `reflection.md`.
- An IDE-integrated AI tool (per the brief's requirement) should be used to actually
  run `dotnet build`, `dotnet test`, and `npm run build` once this repo is on a
  machine with the .NET SDK and network access, to catch anything this manual review
  missed.
