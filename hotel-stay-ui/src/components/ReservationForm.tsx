import { FormEvent, useState } from 'react'
import type { AvailableRoom, City, DocumentType } from '../types/hotel'

interface Props {
  room: AvailableRoom
  destination: City | undefined
  checkIn: string
  checkOut: string
  onSubmit: (values: { guestName: string; documentType: DocumentType; documentNumber: string }) => void
  onCancel: () => void
  isSubmitting: boolean
  serverError: string | null
}

export default function ReservationForm({
  room,
  destination,
  checkIn,
  checkOut,
  onSubmit,
  onCancel,
  isSubmitting,
  serverError,
}: Props) {
  const isInternational = destination?.type === 'International'

  const [guestName, setGuestName] = useState('')
  const [documentType, setDocumentType] = useState<DocumentType>(
    isInternational ? 'Passport' : 'NationalId',
  )
  const [documentNumber, setDocumentNumber] = useState('')
  const [clientError, setClientError] = useState<string | null>(null)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setClientError(null)

    if (!guestName.trim()) {
      setClientError('Enter the guest name.')
      return
    }
    if (!documentNumber.trim()) {
      setClientError('Enter the document number.')
      return
    }
    // Client-side mirror of server validation
    if (isInternational && documentType === 'NationalId') {
      setClientError('International destinations require a Passport.')
      return
    }

    onSubmit({ guestName: guestName.trim(), documentType, documentNumber: documentNumber.trim() })
  }

  return (
    <div className="reservation-panel">
      <button className="link-back" onClick={onCancel} type="button">
        ← Back to results
      </button>

      <div className="reservation-summary">
        <span className="reservation-summary__provider">{room.provider}</span>
        <h2 className="reservation-summary__type">
          {room.roomType} · {destination?.name ?? ''}
        </h2>
        <p className="reservation-summary__dates">
          {checkIn} → {checkOut} · {room.nights} {room.nights === 1 ? 'night' : 'nights'}
        </p>
        <p className="reservation-summary__price">
          ₹{room.totalPrice.toLocaleString('en-IN')} total
        </p>
      </div>

      <form className="reservation-form" onSubmit={handleSubmit}>
        <div className="reservation-form__field">
          <label htmlFor="guestName">Guest name</label>
          <input
            id="guestName"
            type="text"
            value={guestName}
            onChange={(e) => setGuestName(e.target.value)}
            placeholder="As it appears on your document"
          />
        </div>

        <div className="reservation-form__field">
          <label htmlFor="documentType">Document type</label>
          <select
            id="documentType"
            value={documentType}
            onChange={(e) => setDocumentType(e.target.value as DocumentType)}
          >
            <option value="NationalId">National ID</option>
            <option value="Passport">Passport</option>
          </select>
          {isInternational && (
            <p className="reservation-form__hint">
              {destination?.name} requires a passport.
            </p>
          )}
        </div>

        <div className="reservation-form__field">
          <label htmlFor="documentNumber">Document number</label>
          <input
            id="documentNumber"
            type="text"
            value={documentNumber}
            onChange={(e) => setDocumentNumber(e.target.value)}
            placeholder="e.g. A1234567"
          />
        </div>

        {(clientError || serverError) && (
          <p className="reservation-form__error" role="alert">
            {clientError ?? serverError}
          </p>
        )}

        <button type="submit" className="btn btn--primary" disabled={isSubmitting}>
          {isSubmitting ? 'Confirming…' : 'Confirm reservation'}
        </button>
      </form>
    </div>
  )
}
