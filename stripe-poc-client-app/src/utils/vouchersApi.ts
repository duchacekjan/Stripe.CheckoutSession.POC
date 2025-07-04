import apiClient from "@/utils/axiosInstance";
import {BuyVoucherResponse} from "@/types/Vouchers";

export class VouchersApi {

  async buy(price: number, basketId?: string): Promise<string> {
    const request = {
      price: price,
      basketId: basketId
    };
    const response = await apiClient.post<BuyVoucherResponse>('/vouchers/buy', request);
    return response.data.basketId;
  }
}