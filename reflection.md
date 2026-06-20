# Reflection

What I'd improve with more time, roughly in priority order.

## 1. Verify the build for real

I wasn't able to run `dotnet build`, `dotnet test`, or `npm install`/`npm run build`
in the environment this was authored in (no .NET SDK, no network access for npm). I
hand-traced every file for compile correctness and caught two real bugs this way (a
ternary mixing C# 12 collection expressions with `new[]`, and missing
`JsonStringEnumConverter` causing enum fields to serialize as integers instead of the
strings the frontend expects) — but hand-tracing is not a substitute for an actual
compiler and test run. First thing I'd do with a working toolchain: `dotnet build`,
`dotnet test`, `npm run build`, and a manual click-through of the full search →
reserve → confirm flow.

## 2. Room ID encodes too much

The reserve endpoint derives room type and re-derives a "rate" from the `roomId` +
`provider` string convention (`PS-LONDON-Deluxe-042` → Deluxe, PremierStays rate
table). This works for a stub system but is fragile — it's effectively parsing a
quasi-schema out of a string. A more realistic design would have the search endpoint
return a short-lived, server-held quote (e.g. cache the actual `AvailableRoom` by a
generated quote ID for a few minutes), and have `/hotels/reserve` look that quote up
rather than reconstructing it. I kept it simple given "no persistence," but a
short-TTL in-memory quote cache wouldn't have violated that constraint and would have
been more honest about real-world reservation flows (where the price you searched is
the price you should pay, not a re-derived approximation).

## 3. Provider stub realism

Both providers generate rooms from a seeded `Random` rather than a small curated
fixture set per city. This gives good variation and determinism, but a curated
fixture (e.g. a JSON file per provider with 4-5 named hotels per city) would read
more like "representative scenarios" — actual hotel names, more plausible amenity
combinations per star tier — and would make manual demoing feel less synthetic. I'd
add this if the brief's "deterministic and cover representative scenarios" bar were
interpreted more literally as "named test fixtures" rather than "reproducible
algorithm."

## 4. No loading skeletons, only a spinner

The frontend's loading state is a single centered spinner. With more time I'd build
skeleton room cards (matching the boarding-pass shape) so the layout doesn't jump
between the empty/loading/loaded states — small thing, but it's the difference
between a demo feeling like a real product and feeling like a prototype.

## 5. Test coverage gaps

- No test currently asserts the *exact* set of valid room IDs the reserve endpoint
  will accept vs silently defaulting to Standard for an unrecognised ID pattern
  (`DeriveRoomType` falls back to `Standard` rather than rejecting). A stricter
  version would 400 on an unrecognised `roomId` shape instead of guessing.
- No frontend tests at all (no Vitest/RTL setup) — given more time I'd add component
  tests for the client-side document validation logic in `ReservationForm`
  specifically, since that's the one piece of business logic duplicated between
  client and server and most likely to drift out of sync.
- No test for concurrent reservation writes beyond relying on
  `ConcurrentDictionary`'s thread safety implicitly — a stress test hitting
  `/hotels/reserve` concurrently would give more confidence.

## 6. Accessibility pass

Forms have labels wired up correctly and the empty/error states use `role="alert"`,
but I haven't done a full keyboard-navigation or screen-reader pass over the
boarding-pass card layout — the perforation divider is `aria-hidden`, which is
correct, but the star rating's `aria-label` could be more descriptive, and the sort
dropdown's interaction with screen readers when results re-order hasn't been
verified.

## 7. Currency/locale

Defaulted to ₹ (INR) throughout since the domestic cities are Indian and no currency
was specified. A more complete version would derive currency from destination (₹ for
domestic, the local currency or USD for international) rather than using one currency
symbol for every search.

## 8. What went well

- Keeping `IHotelProvider` as the only seam between the endpoint and the two stubs
  paid off — `Program.cs` registers providers as a simple DI list, and the search
  endpoint fans out with `Task.WhenAll` over `IEnumerable<IHotelProvider>` without
  knowing how many there are. Adding a third provider really is just one
  `AddSingleton` line, as the brief's extensibility requirement asked for.
- Writing `spec.md` before any implementation meant the frontend's TypeScript types
  and the backend's C# records were authored independently against the same contract
  and matched up cleanly on the first integration pass (after the enum-serialization
  fix).
