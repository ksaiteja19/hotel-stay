import { useEffect, useState } from 'react'
import type { AvailableRoom, City, DocumentType, ReservationConfirmation as ConfirmationType } from './types/hotel'
import { ApiException, getCities, reserveRoom, searchHotels } from './services/api'
import SearchForm, { SearchValues } from './components/SearchForm'
import ResultsList from './components/ResultsList'
import ReservationForm from './components/ReservationForm'
import Confirmation from './components/Confirmation'
import './styles/app.css'

type View = 'search' | 'reserve' | 'confirmation'

export default function App() {
  const [view, setView] = useState<View>('search')
  const [cities, setCities] = useState<City[]>([])

  const [searchValues, setSearchValues] = useState<SearchValues | null>(null)
  const [rooms, setRooms] = useState<AvailableRoom[] | null>(null)
  const [isSearching, setIsSearching] = useState(false)
  const [searchError, setSearchError] = useState<string | null>(null)

  const [selectedRoom, setSelectedRoom] = useState<AvailableRoom | null>(null)
  const [isReserving, setIsReserving] = useState(false)
  const [reserveError, setReserveError] = useState<string | null>(null)

  const [confirmation, setConfirmation] = useState<ConfirmationType | null>(null)

  useEffect(() => {
    getCities()
      .then(setCities)
      .catch(() => setCities([]))
  }, [])

  async function handleSearch(values: SearchValues) {
    setIsSearching(true)
    setSearchError(null)
    setSearchValues(values)
    try {
      const results = await searchHotels(values)
      setRooms(results)
    } catch (err) {
      setRooms(null)
      setSearchError(err instanceof ApiException ? err.message : 'Something went wrong while searching. Try again.')
    } finally {
      setIsSearching(false)
    }
  }

  function handleSelectRoom(room: AvailableRoom) {
    setSelectedRoom(room)
    setReserveError(null)
    setView('reserve')
  }

  async function handleReserve(values: {
    guestName: string
    documentType: DocumentType
    documentNumber: string
  }) {
    if (!selectedRoom || !searchValues) return
    setIsReserving(true)
    setReserveError(null)
    try {
      const conf = await reserveRoom({
        roomId: selectedRoom.roomId,
        provider: selectedRoom.provider,
        destination: searchValues.destination,
        checkIn: searchValues.checkIn,
        checkOut: searchValues.checkOut,
        guestName: values.guestName,
        documentType: values.documentType,
        documentNumber: values.documentNumber,
      })
      setConfirmation(conf)
      setView('confirmation')
    } catch (err) {
      setReserveError(
        err instanceof ApiException ? err.message : 'Something went wrong confirming the reservation. Try again.',
      )
    } finally {
      setIsReserving(false)
    }
  }

  function handleNewSearch() {
    setView('search')
    setRooms(null)
    setSelectedRoom(null)
    setConfirmation(null)
    setSearchError(null)
    setReserveError(null)
  }

  function handleBackToResults() {
    setView('search')
    setSelectedRoom(null)
    setReserveError(null)
  }

  const destination = searchValues
    ? cities.find((c) => c.code === searchValues.destination)
    : undefined

  return (
    <div className="app">
      <header className="app-header">
        <div className="app-header__inner">
          <span className="app-header__mark">✈</span>
          <div>
            <p className="app-header__brand">SkyRoute</p>
            <p className="app-header__tagline">Hotel Stay</p>
          </div>
        </div>
      </header>

      <main className="app-main">
        {view === 'search' && (
          <>
            <SearchForm onSearch={handleSearch} isSearching={isSearching} initialValues={searchValues ?? undefined} />

            {searchError && (
              <div className="error-banner" role="alert">
                {searchError}
              </div>
            )}

            {isSearching && (
              <div className="loading-state">
                <div className="loading-state__spinner" aria-hidden="true" />
                <p>Checking PremierStays and BudgetNests…</p>
              </div>
            )}

            {!isSearching && rooms && !searchError && (
              <ResultsList rooms={rooms} onSelect={handleSelectRoom} />
            )}
          </>
        )}

        {view === 'reserve' && selectedRoom && searchValues && (
          <ReservationForm
            room={selectedRoom}
            destination={destination}
            checkIn={searchValues.checkIn}
            checkOut={searchValues.checkOut}
            onSubmit={handleReserve}
            onCancel={handleBackToResults}
            isSubmitting={isReserving}
            serverError={reserveError}
          />
        )}

        {view === 'confirmation' && confirmation && (
          <Confirmation confirmation={confirmation} onNewSearch={handleNewSearch} />
        )}
      </main>

      <footer className="app-footer">
        <p>Demo data only · PremierStays &amp; BudgetNests are simulated providers</p>
      </footer>
    </div>
  )
}
