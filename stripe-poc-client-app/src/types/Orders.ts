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
  tickets: Record<number, Ticket[]>;
  vouchers: Voucher[];
  totalPrice: number;
}

export interface RemoveTicketsRequest {
  seatIds: number[];
}

export interface RemoveTicketsResponse {
  status: UpdateStatus;
  message?: string;
}

export interface SetPaidResponse {
  voucherCodes: string[];
}

export interface GetPaidOrdersResponse {
  paidOrders: PaidOrder[];
}

export interface RefundRequest {
  refundedAmount: number;
}

export interface PaidOrder {
  orderId: number;
  basketId: string;
  tickets: Ticket[];
  totalPrice: number;
}

export interface UpdateSessionRequest {
  basketId: string;
}

export interface UpdateSessionResponse {
  type: 'success' | 'error'
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

export interface Voucher {
  id: number;
  code: string;
  amount: number;
}