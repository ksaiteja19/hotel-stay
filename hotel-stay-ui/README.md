# hotel-stay-ui

React + TypeScript frontend for SkyRoute Hotel Stay, built with Vite.

## Run

```bash
npm install
npm run dev
```

Starts on http://localhost:5173 and talks to the API at
`http://localhost:5000` by default. Override with a `.env` file (see
`.env.example`):

```
VITE_API_URL=http://localhost:5000
```

## Structure

```
src/
├── components/
│   ├── SearchForm.tsx        # destination, dates, optional room type
│   ├── ResultsList.tsx       # sortable results, empty state
│   ├── RoomCard.tsx          # boarding-pass styled result card
│   ├── ReservationForm.tsx   # guest + document details, client validation
│   └── Confirmation.tsx      # reference number, full booking summary
├── services/
│   └── api.ts                 # typed fetch wrappers + error handling
├── types/
│   └── hotel.ts                # mirrors backend spec.md models exactly
├── styles/
│   ├── tokens.css              # design tokens (color, type, layout)
│   └── app.css                 # component styles
├── App.tsx                    # view state machine: search → reserve → confirmation
└── main.tsx                   # entry point
```

## Design

The visual identity is built around a physical boarding pass / travel
ticket: each result card has a perforated divider between the room details
and a torn-off price stub, echoing the way real travel documents separate
information from the transaction. Reference numbers and prices use a
monospace face (ticket-stub numerals); headings use Fraunces, a display
serif with the right amount of character for a travel brand without
tipping into cliché.

## Build

```bash
npm run build
```

Type-checks with `tsc -b` then bundles with Vite into `dist/`.
