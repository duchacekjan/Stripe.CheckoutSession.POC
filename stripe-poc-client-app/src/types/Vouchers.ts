export interface BuyVoucherRequest {
  basketId?: string;
  price: number;
}

export interface BuyVoucherResponse {
  basketId: string;
}

export interface ValidateVoucherRequest{
  basketId: string;
  voucherCode: string;
}

export interface ValidateVoucherResponse {
  isValid: boolean;
  message?: string;
  discount?: number;
}