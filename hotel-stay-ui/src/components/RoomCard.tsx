import type { AvailableRoom } from '../types/hotel'

interface Props {
  room: AvailableRoom
  onSelect: (room: AvailableRoom) => void
}

const policyLabel: Record<AvailableRoom['cancellationPolicy'], string> = {
  FreeCancellation: 'Free cancellation · 48h',
  Flexible: 'Flexible · 24h',
  NonRefundable: 'Non-refundable',
}

const policyClass: Record<AvailableRoom['cancellationPolicy'], string> = {
  FreeCancellation: 'policy-tag policy-tag--free',
  Flexible: 'policy-tag policy-tag--flex',
  NonRefundable: 'policy-tag policy-tag--none',
}

export default function RoomCard({ room, onSelect }: Props) {
  return (
    <article className="room-card">
      <div className="room-card__main">
        <div className="room-card__header">
          <span className="room-card__provider">{room.provider}</span>
          {room.starRating && (
            <span className="room-card__stars" aria-label={`${room.starRating} star rating`}>
              {'★'.repeat(room.starRating)}
              <span className="room-card__stars-empty">{'★'.repeat(5 - room.starRating)}</span>
            </span>
          )}
        </div>

        <h3 className="room-card__type">{room.roomType}</h3>

        {room.amenities && room.amenities.length > 0 ? (
          <ul className="room-card__amenities">
            {room.amenities.map((a) => (
              <li key={a}>{a}</li>
            ))}
          </ul>
        ) : (
          <p className="room-card__minimal">Rate &amp; cancellation policy only</p>
        )}

        <span className={policyClass[room.cancellationPolicy]}>
          {policyLabel[room.cancellationPolicy]}
        </span>
      </div>

      <div className="room-card__perforation" aria-hidden="true">
        <span className="room-card__notch room-card__notch--top" />
        <span className="room-card__dashes" />
        <span className="room-card__notch room-card__notch--bottom" />
      </div>

      <div className="room-card__stub">
        <div className="room-card__rate">
          <span className="room-card__rate-amount">
            ₹{room.ratePerNight.toLocaleString('en-IN', { minimumFractionDigits: 0 })}
          </span>
          <span className="room-card__rate-label">per night</span>
        </div>
        <div className="room-card__total">
          <span className="room-card__total-amount">
            ₹{room.totalPrice.toLocaleString('en-IN', { minimumFractionDigits: 0 })}
          </span>
          <span className="room-card__total-label">
            total · {room.nights} {room.nights === 1 ? 'night' : 'nights'}
          </span>
        </div>
        <button className="btn btn--primary room-card__select" onClick={() => onSelect(room)}>
          Reserve
        </button>
      </div>
    </article>
  )
}
