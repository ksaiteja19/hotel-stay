import { useMemo, useState } from 'react'
import type { AvailableRoom } from '../types/hotel'
import RoomCard from './RoomCard'

interface Props {
  rooms: AvailableRoom[]
  onSelect: (room: AvailableRoom) => void
}

type SortOrder = 'price-asc' | 'price-desc'

export default function ResultsList({ rooms, onSelect }: Props) {
  const [sortOrder, setSortOrder] = useState<SortOrder>('price-asc')

  const sorted = useMemo(() => {
    const copy = [...rooms]
    copy.sort((a, b) =>
      sortOrder === 'price-asc' ? a.totalPrice - b.totalPrice : b.totalPrice - a.totalPrice,
    )
    return copy
  }, [rooms, sortOrder])

  if (rooms.length === 0) {
    return (
      <div className="empty-state">
        <p className="empty-state__title">No rooms match this search.</p>
        <p className="empty-state__body">
          Try a different date range, or check another room type.
        </p>
      </div>
    )
  }

  return (
    <section className="results">
      <div className="results__bar">
        <span className="results__count">
          {sorted.length} {sorted.length === 1 ? 'room' : 'rooms'} found
        </span>
        <label className="results__sort">
          Sort by
          <select value={sortOrder} onChange={(e) => setSortOrder(e.target.value as SortOrder)}>
            <option value="price-asc">Total price: low to high</option>
            <option value="price-desc">Total price: high to low</option>
          </select>
        </label>
      </div>

      <div className="results__grid">
        {sorted.map((room) => (
          <RoomCard key={`${room.provider}-${room.roomId}`} room={room} onSelect={onSelect} />
        ))}
      </div>
    </section>
  )
}
