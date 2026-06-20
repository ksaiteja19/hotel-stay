import type { ReservationConfirmation } from '../types/hotel'

interface Props {
  confirmation: ReservationConfirmation
  onNewSearch: () => void
}

const policyLabel: Record<ReservationConfirmation['cancellationPolicy'], string> = {
  FreeCancellation: 'Free cancellation up to 48h before check-in',
  Flexible: 'Flexible — cancel up to 24h before check-in',
  NonRefundable: 'Non-refundable',
}

export default function Confirmation({ confirmation, onNewSearch }: Props) {
  return (
    <div className="confirmation">
      <div className="confirmation__stamp" aria-hidden="true">
        Confirmed
      </div>

      <h2 className="confirmation__title">Reservation confirmed</h2>

      <div className="confirmation__reference">
        <span className="confirmation__reference-label">Reference number</span>
        <span className="confirmation__reference-value">{confirmation.referenceNumber}</span>
      </div>

      <dl className="confirmation__details">
        <div>
          <dt>Provider</dt>
          <dd>{confirmation.provider}</dd>
        </div>
        <div>
          <dt>Room</dt>
          <dd>{confirmation.roomType}</dd>
        </div>
        <div>
          <dt>Destination</dt>
          <dd>{confirmation.destination}</dd>
        </div>
        <div>
          <dt>Guest</dt>
          <dd>{confirmation.guestName}</dd>
        </div>
        <div>
          <dt>Dates</dt>
          <dd>
            {confirmation.checkIn} → {confirmation.checkOut}
          </dd>
        </div>
        <div>
          <dt>Total price</dt>
          <dd>₹{confirmation.totalPrice.toLocaleString('en-IN')}</dd>
        </div>
        <div>
          <dt>Cancellation</dt>
          <dd>{policyLabel[confirmation.cancellationPolicy]}</dd>
        </div>
      </dl>

      <button className="btn btn--primary" onClick={onNewSearch}>
        Search another stay
      </button>
    </div>
  )
}
