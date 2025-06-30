const BASKET_ID_KEY = 'basketId';

export function getCurrentBasketId(): string | null {
  return localStorage.getItem(BASKET_ID_KEY);
}

export function setCurrentBasketId(basketId: string | null) {
  return basketId
    ? localStorage.setItem(BASKET_ID_KEY, basketId)
    : localStorage.removeItem(BASKET_ID_KEY);
}