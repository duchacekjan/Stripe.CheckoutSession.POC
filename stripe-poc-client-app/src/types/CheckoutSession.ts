export interface CheckoutSessionCreateResponse {
  clientSecret: string;
  sessionId: string;
}

export interface CheckoutSessionStatusResponse {
  status: string;
  email?: string;
  basketId?: string;
}