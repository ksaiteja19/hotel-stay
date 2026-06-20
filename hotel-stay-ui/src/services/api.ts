import type {
  AvailableRoom,
  City,
  ReservationConfirmation,
  ReservationRequest,
  RoomTypeFilter,
} from '../types/hotel'

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export class ApiException extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
    this.name = 'ApiException'
  }
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let message = `Request failed with status ${res.status}`
    try {
      const body = await res.json()
      if (body?.error) message = body.error
    } catch {
      // body wasn't JSON — keep default message
    }
    throw new ApiException(message, res.status)
  }
  return res.json() as Promise<T>
}

export interface SearchParams {
  destination: string
  checkIn: string
  checkOut: string
  roomType?: RoomTypeFilter
}

export async function searchHotels(params: SearchParams): Promise<AvailableRoom[]> {
  const query = new URLSearchParams({
    destination: params.destination,
    checkIn: params.checkIn,
    checkOut: params.checkOut,
  })
  if (params.roomType) query.set('roomType', params.roomType)

  const res = await fetch(`${BASE_URL}/hotels/search?${query.toString()}`)
  return handleResponse<AvailableRoom[]>(res)
}

export async function reserveRoom(
  request: ReservationRequest,
): Promise<ReservationConfirmation> {
  const res = await fetch(`${BASE_URL}/hotels/reserve`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  return handleResponse<ReservationConfirmation>(res)
}

export async function getReservation(reference: string): Promise<ReservationConfirmation> {
  const res = await fetch(`${BASE_URL}/hotels/reservation/${encodeURIComponent(reference)}`)
  return handleResponse<ReservationConfirmation>(res)
}

export async function getCities(): Promise<City[]> {
  const res = await fetch(`${BASE_URL}/hotels/cities`)
  return handleResponse<City[]>(res)
}
