import apiClient from "@/utils/axiosInstance";
import {
  BuyVoucherRequest,
  BuyVoucherResponse,
  RedeemVoucherRequest, RemoveRedeemedVoucherRequest,
  ValidateVoucherRequest,
  ValidateVoucherResponse
} from "@/types/Vouchers";

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

  async redeem(basketId: string, code: string): Promise<void> {
    const request: RedeemVoucherRequest = {
      basketId: basketId,
      voucherCode: code
    };
    const response = await apiClient.post<void>('/vouchers/redeem', request);
    return response.data;
  }

  async removeRedeemed(basketId: string, code: string): Promise<void> {
    const request: RemoveRedeemedVoucherRequest = {
      basketId: basketId,
      voucherCode: code
    };
    const response = await apiClient.delete<void>('/vouchers/remove', {data: request});
    return response.data;
  }
}