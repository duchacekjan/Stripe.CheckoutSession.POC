export interface CreateOrderRequest {
  seatIds: number[];
}

export interface CreateOrderResponse {
  basketId: string;
  totalPrice: number;
}

export interface AddSeatsToOrderRequest {
  seatIds: number[];
}

export interface AddSeatsToOrderResponse {
  basketId: string;
  totalPrice: number;
}

export interface GetTicketsResponse {
  tickets: Ticket[];
}

export interface RemoveTicketsRequest {
  sessionId: string;
  seatIds: number[];
}

export interface RemoveTicketsResponse {
  status: UpdateStatus;
  message?: string;
}

export enum UpdateStatus {
  Updated = 'updated',
  Emptied = 'emptied',
  Error = 'error'
}

export interface Ticket {
  eventId: number;
  eventName: string;
  performanceId: number;
  performanceDate: string;
  priceId: number;
  price: number;
  seatId: number;
  seatRow: string;
  seatNumber: number;
}