import {CheckoutSessionCreateResponse, CheckoutSessionStatusResponse} from "@/types/CheckoutSession";
import apiClient from "@/utils/axiosInstance";

export class CheckoutSessionApi {
  async create(basketId: string): Promise<CheckoutSessionCreateResponse> {
    const response = await apiClient.post<CheckoutSessionCreateResponse>(`/stripe/checkout-session/${basketId}/create`, null);
    return response.data;
  }
  
  async status(sessionId: string): Promise<CheckoutSessionStatusResponse>{
    const response = await apiClient.get<CheckoutSessionStatusResponse>(`/stripe/checkout-session/${sessionId}/status`);
    return response.data;
  }
}