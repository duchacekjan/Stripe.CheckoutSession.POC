"use client";

import React, {useState, useEffect, useMemo} from "react";
import {Appearance, loadStripe} from '@stripe/stripe-js';
import {
  CheckoutProvider
} from '@stripe/react-stripe-js';
import {createCheckoutSession} from "@/utils/api";
import CheckoutSessionForm from "@/app/checkout-session/Components/CheckoutSessionForm";
import {getCurrentBasketId, setCurrentBasketId} from "@/utils/basketIdProvider";
import {useRouter} from "next/navigation";
import CheckoutSummary from "@/app/checkout-session/Components/CheckoutSummary";

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLIC_KEY || '', {
  betas: ['custom_checkout_server_updates_1'],
});

const CheckoutSessionPage: React.FC = () => {
  const [basketId, setBasketId] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    const currentBasketId = getCurrentBasketId();
    if (currentBasketId) {
      setBasketId(currentBasketId);
    } else {
      console.log("No basket ID found. Redirecting to seat plan.");
      router.push('/');
    }
  }, []);

  const fetchClientSecret = async () => {
    try {
      const response = await createCheckoutSession(basketId!);
      return response.clientSecret;
    } catch (error) {
      console.error("Error creating checkout session:", error);
      setCurrentBasketId(null); // Clear basket ID on error
      router.push('/');
      return Promise.reject(error);
    }
  }

  const appearance: Appearance = {
    theme: 'stripe',
  };

  if (!stripePromise || !basketId) {
    return (
      <div style={{minHeight: '100vh', backgroundColor: '#f9fafb', padding: '16px 0'}}>
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
      </div>
    )
  }

  return (
    <CheckoutProvider
      stripe={stripePromise}
      options={{
        fetchClientSecret: fetchClientSecret,
        elementsOptions: {
          appearance: appearance,
          loader: 'auto'
        }
      }}
    >
      <div style={{minHeight: '100vh', backgroundColor: '#f9fafb', padding: '16px 0'}}>
        <div style={{maxWidth: '1100px', margin: '0 auto', padding: '0 8px'}}>
          <div style={{
            display: 'grid',
            gridTemplateColumns: '1fr 290px',
            gap: '8px',
            backgroundColor: 'white',
            borderRadius: '8px',
            boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
            padding: '8px'
          }}>
            <div style={{width: '400px'}}>
              <CheckoutSessionForm/>
            </div>
            <CheckoutSummary/>
          </div>
        </div>

      </div>
    </CheckoutProvider>
  );
};

export default CheckoutSessionPage;