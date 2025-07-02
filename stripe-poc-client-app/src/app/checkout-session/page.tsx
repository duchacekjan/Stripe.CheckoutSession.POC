"use client";

import React, {useEffect, useState} from "react";
import {Appearance, loadStripe} from '@stripe/stripe-js';
import {CheckoutProvider} from '@stripe/react-stripe-js';
import CheckoutSessionForm from "@/app/checkout-session/Components/CheckoutSessionForm";
import {getCurrentBasketId} from "@/utils/basketIdProvider";
import {useRouter} from "next/navigation";
import CheckoutSummary from "@/app/checkout-session/Components/CheckoutSummary";
import {useApi} from "@/utils/api";

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLIC_KEY || '', {
  betas: ['custom_checkout_server_updates_1', 'custom_checkout_adaptive_pricing_2'],
});

const CheckoutSessionPage: React.FC = () => {
  const [basketId, setBasketId] = useState<string | null>(null);
  const [hasPerformance, setHasPerformance] = useState<boolean>(false);
  const [bookingProtection, setBookingProtection] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isOpened, setIsOpened] = useState<boolean>(false);
  const router = useRouter();
  const api = useApi();

  useEffect(() => {
    const currentBasketId = getCurrentBasketId();
    if (currentBasketId) {
      setBasketId(currentBasketId);
    } else {
      console.log("No basket ID found. Redirecting to seat plan.");
      router.push('/');
    }
  }, []);

  useEffect(() => {
    if (!sessionId) {
      setIsOpened(false);
      return;
    }
    console.log("Fetching checkout session status for session ID:", sessionId);
    api.checkoutSessions.status(sessionId)
      .then((response) => {
        console.log("Checkout session status response:", response.status);
        if (response.status === 'open') {
          setIsOpened(true);
          setIsLoading(false);
        } else {
          setIsOpened(false);
          setIsLoading(false);
        }
      })
      .catch((error) => {
        setError(`Failed to fetch checkout session status. Please try again. ${error}`);
        setIsOpened(false);
      })
  }, [sessionId]);

  const fetchClientSecret = async () => {
    setIsLoading(true);
    try {
      const response = await api.checkoutSessions.create(basketId!);
      console.log("Checkout session created with response:", response);
      setSessionId(response.sessionId);
      return response.clientSecret;
    } catch (error) {
      setError(`Failed to create checkout session. Please try again. ${error}`);
      setIsLoading(false);
      return Promise.reject(error);
    }
  }

  const handleReload = () => {
    setError(null);
    window.location.reload();
  }

  const appearance: Appearance = {
    theme: 'stripe',
  };

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
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            minWidth: 'calc(100vh - 32px)',
            backgroundColor: 'white',
            borderRadius: '8px',
            padding: '24px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: '16px',
            boxShadow: '0 10px 25px rgba(0, 0, 0, 0.2)'
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

      <div style={{maxWidth: '1100px', margin: '0 auto', padding: '0 8px'}}>
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
          display: 'grid',
          minHeight: 'calc(100vh - 35px)',
          gridTemplateColumns: '1fr 290px',
          gap: '8px',
          backgroundColor: 'white',
          borderRadius: '8px',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
          padding: '8px'
        }}>
          {basketId && (
            <CheckoutProvider
              stripe={stripePromise}
              options={{
                fetchClientSecret: fetchClientSecret,
                elementsOptions: {
                  appearance: appearance
                }
              }}
            >
              {isOpened && (
                <>
                  <div style={{width: '400px'}}>
                    <CheckoutSessionForm
                      basketId={basketId!}
                      hasPerformance={hasPerformance}
                      bookingProtection={bookingProtection}
                      setBookingProtection={setBookingProtection}
                    />
                  </div>
                  <CheckoutSummary
                    setHasPerformance={setHasPerformance}
                    bookingProtection={bookingProtection}
                    setBookingProtection={setBookingProtection}/>
                </>
              )}

            </CheckoutProvider>
          )}
        </div>
      </div>
    </div>
  )
    ;
};

export default CheckoutSessionPage;