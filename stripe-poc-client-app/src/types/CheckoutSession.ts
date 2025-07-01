export interface CheckoutSessionCreateRequest {
  basketId: string;
}

export interface CheckoutSessionCreateResponse {
  clientSecret: string;
}

export interface CheckoutSessionStatusResponse {
  status: string;
  email?: string;
  basketId?: string;
}