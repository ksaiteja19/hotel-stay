export type RoomType = 'Standard' | 'Deluxe' | 'Suite'

export type RoomTypeFilter = RoomType | ''

export type CancellationPolicy = 'FreeCancellation' | 'Flexible' | 'NonRefundable'

export type DocumentType = 'Passport' | 'NationalId'

export type DestinationType = 'Domestic' | 'International'

export interface City {
  code: string
  name: string
  type: DestinationType
}

export interface AvailableRoom {
  roomId: string
  provider: string
  roomType: RoomType
  ratePerNight: number
  totalPrice: number
  nights: number
  cancellationPolicy: CancellationPolicy
  starRating: number | null
  amenities: string[] | null
}

export interface ReservationRequest {
  roomId: string
  provider: string
  destination: string
  checkIn: string
  checkOut: string
  guestName: string
  documentType: DocumentType
  documentNumber: string
}

export interface ReservationConfirmation {
  referenceNumber: string
  provider: string
  roomType: RoomType
  destination: string
  checkIn: string
  checkOut: string
  guestName: string
  totalPrice: number
  cancellationPolicy: CancellationPolicy
}

export interface ApiError {
  error: string
}
