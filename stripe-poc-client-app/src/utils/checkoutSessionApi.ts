import {CheckoutSessionCreateResponse, CheckoutSessionStatusResponse} from "@/types/CheckoutSession";
import apiClient from "@/utils/axiosInstance";
import {UpdateSessionRequest, UpdateSessionResponse} from "@/types/Orders";
import axios from "axios";

export class CheckoutSessionApi {
  async create(basketId: string): Promise<CheckoutSessionCreateResponse | null> {
    try {
      const response = await apiClient.post<CheckoutSessionCreateResponse>(`/stripe/checkout-session/${basketId}/create`, null);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  async status(sessionId: string): Promise<CheckoutSessionStatusResponse> {
    const response = await apiClient.get<CheckoutSessionStatusResponse>(`/stripe/checkout-session/${sessionId}/status`);
    return response.data;
  }

  async update(basketId: string): Promise<UpdateSessionResponse> {
    const request: UpdateSessionRequest = {
      basketId: basketId
    };
    const response = await apiClient.put<UpdateSessionResponse>(`/stripe/checkout-session/update`, request);
    return response.data;
  }
}