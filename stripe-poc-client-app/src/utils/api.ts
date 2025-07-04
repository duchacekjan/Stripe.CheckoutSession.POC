import {CheckoutSessionApi} from "@/utils/checkoutSessionApi";
import { useRef } from "react";
import {InventoryApi} from "@/utils/inventoryApi";
import {OrdersApi} from "@/utils/ordersApi";
import {VouchersApi} from "@/utils/vouchersApi";

export class ApiClient {
  checkoutSessions: CheckoutSessionApi = new CheckoutSessionApi();
  inventory: InventoryApi = new InventoryApi();
  orders: OrdersApi = new OrdersApi();
  vouchers: VouchersApi = new VouchersApi();
}

// Singleton instance
let apiClientInstance: ApiClient | null = null;

const getApiClient = (): ApiClient => {
  if (!apiClientInstance) {
    apiClientInstance = new ApiClient();
  }
  return apiClientInstance;
};

// React hook to get the singleton ApiClient
export const useApi = (): ApiClient => {
  const apiClientRef = useRef<ApiClient | null>(null);

  if (!apiClientRef.current) {
    apiClientRef.current = getApiClient();
  }

  return apiClientRef.current;
};
