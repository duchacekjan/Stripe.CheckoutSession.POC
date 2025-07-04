import apiClient from "@/utils/axiosInstance";
import {BuyVoucherRequest, BuyVoucherResponse, ValidateVoucherRequest, ValidateVoucherResponse} from "@/types/Vouchers";

export class VouchersApi {

  async buy(price: number, basketId?: string): Promise<string> {
    const request: BuyVoucherRequest = {
      price: price,
      basketId: basketId
    };
    const response = await apiClient.post<BuyVoucherResponse>('/vouchers/buy', request);
    return response.data.basketId;
  }

  async validate(basketId: string, code: string): Promise<ValidateVoucherResponse> {
    const request: ValidateVoucherRequest = {
      basketId: basketId,
      voucherCode: code
    };
    const response = await apiClient.post<ValidateVoucherResponse>('/vouchers/validate', request);
    return response.data;
  }
}