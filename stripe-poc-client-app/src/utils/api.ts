import apiClient from "./axiosInstance";
import {GetEventsResponse, GetSeatsResponse} from "@/types/Inventory";
import {
  AddSeatsToOrderRequest,
  AddSeatsToOrderResponse,
  CreateOrderRequest,
  CreateOrderResponse, GetPaidOrdersResponse,
  GetTicketsResponse,
  RemoveTicketsRequest,
  RemoveTicketsResponse, SetPaidResponse
} from "@/types/Orders";
import {
  CheckoutSessionCreateRequest,
  CheckoutSessionCreateResponse,
  CheckoutSessionStatusResponse
} from "@/types/CheckoutSession";
import {BuyVoucherResponse} from "@/types/Vouchers";

export const createCheckoutSession = async (basketId: string): Promise<CheckoutSessionCreateResponse> => {
  const request: CheckoutSessionCreateRequest = {
    basketId: basketId
  }
  const response = await apiClient.post<CheckoutSessionCreateResponse>('/stripe/checkout-session/create', request);
  return response.data;
}

export const checkoutSessionStatus = async (sessionId: string): Promise<CheckoutSessionStatusResponse> => {
  const response = await apiClient.get<CheckoutSessionStatusResponse>(`/stripe/checkout-session/${sessionId}/status`);
  return response.data;
}

export const getEvents = async (): Promise<GetEventsResponse> => {
  const response = await apiClient.get<GetEventsResponse>('/inventory/events');
  return response.data;
}

export const getSeats = async (performanceId: number): Promise<GetSeatsResponse> => {
  const response = await apiClient.get<GetSeatsResponse>(`/inventory/performances/${performanceId}/seats`);
  return response.data;
}

export const createOrder = async (seatIds: number[]): Promise<CreateOrderResponse> => {
  const request: CreateOrderRequest = {
    seatIds: seatIds
  }
  const response = await apiClient.post<CreateOrderResponse>('/orders/create', request);
  return response.data;
}

export const addSeatsToOrder = async (basketId: string, seatIds: number[]): Promise<AddSeatsToOrderResponse> => {
  const request: AddSeatsToOrderRequest = {
    seatIds: seatIds
  }
  const response = await apiClient.post<AddSeatsToOrderResponse>(`/orders/${basketId}/add-seats`, request);
  return response.data;
}

export const getOrderTickets = async (basketId: string): Promise<GetTicketsResponse> => {
  const response = await apiClient.get<GetTicketsResponse>(`/orders/${basketId}/tickets`);
  return response.data;
}

export const removeOrderTickets = async (basketId: string, sessionId: string, seatIds: number[]): Promise<RemoveTicketsResponse> => {
  const request: RemoveTicketsRequest = {
    sessionId: sessionId,
    seatIds: seatIds
  }
  console.log("Removing tickets with request:", request);
  const response = await apiClient.post<RemoveTicketsResponse>(`/orders/${basketId}/remove-tickets`, request);
  return response.data;
}

export const buyVoucher = async (price: number, basketId?: string): Promise<BuyVoucherResponse> => {
  const request = {
    price: price,
    basketId: basketId
  };
  const response = await apiClient.post<BuyVoucherResponse>('/vouchers/buy', request);
  return response.data;
}

export const setPaid = async (basketId: string): Promise<SetPaidResponse> => {
  const response = await apiClient.post<SetPaidResponse>(`/orders/${basketId}/set-paid`, null);
  return response.data;
}

export const updatedBookingProtection = async (basketId: string, enableProtection: boolean): Promise<void> => {
  const request = {
    enableProtection: enableProtection
  };
  await apiClient.post<void>(`/orders/${basketId}/set-booking-protection`, request);
}

export const getPaidOrders = async (): Promise<GetPaidOrdersResponse> => {
  const response = await apiClient.get<GetPaidOrdersResponse>('/orders/paid');
  return response.data;
}

export const refundOrder = async (basketId: string, refundedAmount: number): Promise<void> => {
  const request = {
    refundedAmount: refundedAmount
  };
  await apiClient.post<void>(`/orders/${basketId}/refund`, request);
}