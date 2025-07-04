"use client";

import React, {useEffect, useState} from "react";
import {Appearance, loadStripe} from '@stripe/stripe-js';
import {CheckoutProvider} from '@stripe/react-stripe-js';
import CheckoutSessionForm from "@/app/checkout-session/Components/CheckoutSessionForm";
import {getCurrentBasketId, setCurrentBasketId} from "@/utils/basketIdProvider";
import {useRouter} from "next/navigation";
import CheckoutSummary from "@/app/checkout-session/Components/CheckoutSummary";
import {useApi} from "@/utils/api";
import {CheckoutSessionCreateResponse} from "@/types/CheckoutSession";

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLIC_KEY || '', {
  betas: ['custom_checkout_server_updates_1', 'custom_checkout_adaptive_pricing_2'],
});

const CheckoutSessionPage: React.FC = () => {
  const [basketId, setBasketId] = useState<string | null>(null);
  const [hasPerformance, setHasPerformance] = useState<boolean>(false);
  const [bookingProtection, setBookingProtection] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [loadingQueue, setLoadingQueue] = useState<boolean[]>([]);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const router = useRouter();
  const api = useApi();

  const handleSession = (response: CheckoutSessionCreateResponse | null) => {
    console.log("Checkout session created with response:", response);
    if (response === null) {
      console.warn("No session created. Redirecting to seat plan.");
      setCurrentBasketId(null);
      router.push('/');
    } else if (response.status === 'completed') {
      console.warn("Checkout session already completed. Redirecting to order successful page.");
      router.push(`/order-successful?session_id=${response.sessionId}`);
    } else {
      setClientSecret(response.clientSecret);
    }
  }

  useEffect(() => {
    const currentBasketId = getCurrentBasketId();
    if (currentBasketId) {
      setBasketId(currentBasketId);
      api.checkoutSessions.create(currentBasketId)
        .then((response) => {
          handleSession(response)
        })
        .catch((error) => {
          setError(`Failed to create checkout session. Please try again. ${error}`);
          setIsLoading(false);
        });
    } else {
      console.log("No basket ID found. Redirecting to seat plan.");
      router.push('/');
    }
  }, []);

  const handleReload = () => {
    setError(null);
    window.location.reload();
  }

  const appearance: Appearance = {
    theme: 'stripe',
  };

  const handleChangeBookingProtection = (protection: boolean) => {
    console.log("PAGE => booking protection to:", protection);
    setBookingProtection(protection)
  }

  const handleShopMore = () => {
    router.push('/');
  }

  const handleBuyVoucher = () => {
    router.push('/vouchers');
  }

  const handleLoadingQueue = (isLoading: boolean) => {
    const newQueue = [...loadingQueue];
    if (isLoading) {
      newQueue.push(true);
    } else {
      newQueue.pop();
    }
    setLoadingQueue(newQueue);
    setIsLoading(newQueue.length > 0);
  }

  return (
    <div style={{minHeight: '100vh', backgroundColor: '#f9fafb', padding: '16px 0'}}>
      {/* Loading Overlay */}
      {!basketId || !stripePromise || isLoading && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          backgroundColor: 'rgba(0, 0, 0, 0.2)',
          display: 'flex',
          flexDirection: 'column',
          gap: '16px',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          {/* Spinner */}
          <div style={{
            width: '40px',
            height: '40px',
            border: '3px solid #e5e7eb',
            borderTop: '3px solid #3b82f6',
            borderRadius: '50%',
            animation: 'spin 1s linear infinite'
          }}></div>
          <div style={{
            fontSize: '18px',
            color: '#374151',
            fontWeight: '500'
          }}>
            Loading...
          </div>
        </div>
      )}

      {/* CSS for spinner animation */}
      <style jsx>{`
          @keyframes spin {
              0% {
                  transform: rotate(0deg);
              }
              100% {
                  transform: rotate(360deg);
              }
          }
      `}</style>

      <div style={{maxWidth: '800px', margin: '0 auto', padding: '0 8px'}}>
        {/* Error Panel */}
        {error && (
          <div style={{
            marginBottom: '16px',
            padding: '16px',
            backgroundColor: '#fef2f2',
            borderRadius: '6px',
            border: '1px solid #dc2626',
            display: 'flex',
            alignItems: 'center',
            gap: '8px'
          }}>
            <div style={{
              fontSize: '18px',
              fontWeight: '500',
              color: '#dc2626'
            }}>
              {error}
            </div>
            <button
              type={'button'}
              onClick={() => handleReload()}
              title="Reload page"
              style={{
                marginLeft: 'auto',
                padding: '4px 8px',
                backgroundColor: 'transparent',
                border: '1px solid #dc2626',
                borderRadius: '4px',
                color: '#dc2626',
                cursor: 'pointer',
                fontSize: '14px'
              }}
            >
              ×
            </button>
          </div>
        )}

        <div style={{
          height: 'calc(100vh - 35px)',
          backgroundColor: 'white',
          borderRadius: '8px',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
          padding: '8px'
        }}>
          {clientSecret && (
            <CheckoutProvider
              stripe={stripePromise}
              options={{
                fetchClientSecret: () => Promise.resolve(clientSecret),
                elementsOptions: {
                  appearance: appearance
                }
              }}
            >
              <div style={{display: 'flex', flexDirection: 'column', gap: '8px'}}>
                <div style={{display: 'flex', justifyContent: 'space-between'}}>
                  <div>
                    <h2 style={{color: '#0c4a6e'}}>Checkout</h2>
                  </div>
                  <div style={{display: 'flex', flexDirection: 'row-reverse', gap: '8px'}}>
                    <div style={{display: 'flex', gap: '8px'}}>
                      <button type={'button'}
                              onClick={handleShopMore}>
                        Shop more
                      </button>
                      <button type={'button'}
                              onClick={handleBuyVoucher}>
                        Buy voucher
                      </button>
                    </div>
                  </div>
                </div>
                <div style={{
                  display: 'grid',
                  gridTemplateColumns: '1fr 290px',
                  gap: '8px'
                }}>
                  <CheckoutSessionForm
                    basketId={basketId!}
                    hasPerformance={hasPerformance}
                    bookingProtection={bookingProtection}
                    setBookingProtection={handleChangeBookingProtection}
                    isLoading={isLoading}
                    setIsLoading={handleLoadingQueue}
                  />
                  <CheckoutSummary
                    setHasPerformance={setHasPerformance}
                    bookingProtection={bookingProtection}
                    setBookingProtection={handleChangeBookingProtection}
                    setIsLoading={handleLoadingQueue}/>
                </div>
              </div>
            </CheckoutProvider>
          )}
        </div>
      </div>
    </div>
  );
};

export default CheckoutSessionPage;