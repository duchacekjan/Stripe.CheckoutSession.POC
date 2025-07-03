import {
  AddSeatsToOrderRequest,
  AddSeatsToOrderResponse,
  CreateOrderRequest,
  CreateOrderResponse, GetPaidOrdersResponse,
  GetTicketsResponse, PaidOrder, RemoveTicketsRequest, RemoveTicketsResponse, SetPaidResponse, Ticket
} from "@/types/Orders";
import apiClient from "@/utils/axiosInstance";
import {BuyVoucherResponse} from "@/types/Vouchers";

export class OrdersApi {
  async create(seatIds: number[]): Promise<string> {
    const request: CreateOrderRequest = {
      seatIds: seatIds
    };
    const response = await apiClient.post<CreateOrderResponse>('/orders/create', request);
    return response.data.basketId;
  }

  async addSeats(basketId: string, seatIds: number[]): Promise<string> {
    const request: AddSeatsToOrderRequest = {
      seatIds: seatIds
    }
    const response = await apiClient.post<AddSeatsToOrderResponse>(`/orders/${basketId}/add-seats`, request);
    return response.data.basketId;
  }

  async getTickets(basketId: string): Promise<GetTicketsResponse> {
    const response = await apiClient.get<GetTicketsResponse>(`/orders/${basketId}/tickets`);
    return response.data;
  }

  async removeTickets(basketId: string, seatIds: number[]): Promise<RemoveTicketsResponse> {
    const request: RemoveTicketsRequest = {
      seatIds: seatIds
    }
    const response = await apiClient.post<RemoveTicketsResponse>(`/orders/${basketId}/remove-tickets`, request);
    return response.data;
  }

  async buyVoucher(price: number, basketId?: string): Promise<string> {
    const request = {
      price: price,
      basketId: basketId
    };
    const response = await apiClient.post<BuyVoucherResponse>('/vouchers/buy', request);
    return response.data.basketId;
  }

  async setPaid(basketId: string): Promise<string[]> {
    const response = await apiClient.post<SetPaidResponse>(`/orders/${basketId}/set-paid`, null);
    return response.data.voucherCodes;
  }

  async updatedBookingProtection(basketId: string, enableProtection: boolean): Promise<void> {
    const request = {
      enableProtection: enableProtection
    };
    await apiClient.post<void>(`/orders/${basketId}/set-booking-protection`, request);
  }

  async getPaidOrders(): Promise<PaidOrder[]> {
    const response = await apiClient.get<GetPaidOrdersResponse>('/orders/paid');
    return response.data.paidOrders;
  }

  async refund(basketId: string, refundedAmount: number): Promise<void> {
    const request = {
      refundedAmount: refundedAmount
    };
    await apiClient.post<void>(`/orders/${basketId}/refund`, request);
  }
  
  async finalizeOrder(basketId: string): Promise<void> {
    await apiClient.post<void>(`/orders/${basketId}/finalize`, null);
  }
  
  async setPaymentFailed(basketId: string): Promise<void> {
    await apiClient.post<void>(`/orders/${basketId}/set-payment-failed`, null);
  }
}