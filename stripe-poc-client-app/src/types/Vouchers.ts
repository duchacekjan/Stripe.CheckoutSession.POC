export interface BuyVoucherRequest {
  basketId?: string;
  price: number;
}

export interface BuyVoucherResponse {
  basketId: string;
}