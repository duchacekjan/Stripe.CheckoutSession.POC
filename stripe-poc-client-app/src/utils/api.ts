import apiClient from "./axiosInstance";
import {GetEventsResponse, GetSeatsResponse} from "@/types/Inventory";
import {
  AddSeatsToOrderRequest,
  AddSeatsToOrderResponse,
  CreateOrderRequest,
  CreateOrderResponse,
  GetTicketsResponse,
  RemoveTicketsRequest,
  RemoveTicketsResponse
} from "@/types/Orders";
import {
  CheckoutSessionCreateRequest,
  CheckoutSessionCreateResponse,
  CheckoutSessionStatusRequest,
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
  const request: CheckoutSessionStatusRequest = {
    sessionId: sessionId,
  }
  const response = await apiClient.post<CheckoutSessionStatusResponse>('/stripe/checkout-session/status', request);
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