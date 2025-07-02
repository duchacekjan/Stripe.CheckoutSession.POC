export interface GetEventsResponse {
  events: Event[];
}

export interface GetSeatsResponse {
  seats: Seat[];
}

export interface Event {
  id: number;
  name: string;
  performances: Performance[];
}

export interface Performance {
  id: number;
  performanceDate: string;
  durationMinutes: number;
}

export interface Seat {
  id: number;
  row: string;
  number: number;
  priceId: number;
  amount: number;
  isAvailable: boolean;
  priceName: string;
}