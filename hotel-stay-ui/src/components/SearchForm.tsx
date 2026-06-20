import { FormEvent, useEffect, useState } from 'react'
import type { City, RoomTypeFilter } from '../types/hotel'
import { getCities } from '../services/api'

export interface SearchValues {
  destination: string
  checkIn: string
  checkOut: string
  roomType: RoomTypeFilter
}

interface Props {
  onSearch: (values: SearchValues) => void
  isSearching: boolean
  initialValues?: SearchValues
}

const today = new Date().toISOString().slice(0, 10)
const inThreeDays = new Date(Date.now() + 3 * 86400000).toISOString().slice(0, 10)

export default function SearchForm({ onSearch, isSearching, initialValues }: Props) {
  const [cities, setCities] = useState<City[]>([])
  const [destination, setDestination] = useState(initialValues?.destination ?? '')
  const [checkIn, setCheckIn] = useState(initialValues?.checkIn ?? today)
  const [checkOut, setCheckOut] = useState(initialValues?.checkOut ?? inThreeDays)
  const [roomType, setRoomType] = useState<RoomTypeFilter>(initialValues?.roomType ?? '')
  const [clientError, setClientError] = useState<string | null>(null)

  useEffect(() => {
    getCities()
      .then(setCities)
      .catch(() => setCities([]))
  }, [])

  const domestic = cities.filter((c) => c.type === 'Domestic')
  const international = cities.filter((c) => c.type === 'International')

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setClientError(null)

    if (!destination) {
      setClientError('Choose a destination to search.')
      return
    }
    if (!checkIn || !checkOut) {
      setClientError('Choose both check-in and check-out dates.')
      return
    }
    if (checkOut <= checkIn) {
      setClientError('Check-out must be after check-in.')
      return
    }

    onSearch({ destination, checkIn, checkOut, roomType })
  }

  return (
    <form className="search-form" onSubmit={handleSubmit}>
      <div className="search-form__field search-form__field--wide">
        <label htmlFor="destination">Destination</label>
        <select
          id="destination"
          value={destination}
          onChange={(e) => setDestination(e.target.value)}
        >
          <option value="">Where to?</option>
          {domestic.length > 0 && (
            <optgroup label="Domestic · National ID accepted">
              {domestic.map((c) => (
                <option key={c.code} value={c.code}>
                  {c.name}
                </option>
              ))}
            </optgroup>
          )}
          {international.length > 0 && (
            <optgroup label="International · Passport required">
              {international.map((c) => (
                <option key={c.code} value={c.code}>
                  {c.name}
                </option>
              ))}
            </optgroup>
          )}
        </select>
      </div>

      <div className="search-form__field">
        <label htmlFor="checkIn">Check-in</label>
        <input
          id="checkIn"
          type="date"
          value={checkIn}
          min={today}
          onChange={(e) => setCheckIn(e.target.value)}
        />
      </div>

      <div className="search-form__field">
        <label htmlFor="checkOut">Check-out</label>
        <input
          id="checkOut"
          type="date"
          value={checkOut}
          min={checkIn}
          onChange={(e) => setCheckOut(e.target.value)}
        />
      </div>

      <div className="search-form__field">
        <label htmlFor="roomType">Room type</label>
        <select
          id="roomType"
          value={roomType}
          onChange={(e) => setRoomType(e.target.value as RoomTypeFilter)}
        >
          <option value="">Any</option>
          <option value="Standard">Standard</option>
          <option value="Deluxe">Deluxe</option>
          <option value="Suite">Suite</option>
        </select>
      </div>

      <button type="submit" className="btn btn--primary search-form__submit" disabled={isSearching}>
        {isSearching ? 'Searching…' : 'Search stays'}
      </button>

      {clientError && (
        <p className="search-form__error" role="alert">
          {clientError}
        </p>
      )}
    </form>
  )
}
