import {GetEventsResponse, GetSeatsResponse} from "@/types/Inventory";
import apiClient from "@/utils/axiosInstance";

export class InventoryApi {
  async getEvents(): Promise<GetEventsResponse> {
    const response = await apiClient.get<GetEventsResponse>('/inventory/events');
    return response.data;
  }

  async getSeats(performanceId: number): Promise<GetSeatsResponse> {
    const response = await apiClient.get<GetSeatsResponse>(`/inventory/performances/${performanceId}/seats`);
    return response.data;
  }
}