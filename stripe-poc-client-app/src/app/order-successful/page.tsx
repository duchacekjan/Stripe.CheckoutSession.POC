"use client";

import {useRouter} from "next/navigation";
import {useEffect, useState} from "react";
import {checkoutSessionStatus} from "@/utils/api";
import { setCurrentBasketId } from "@/utils/basketIdProvider";

const OrderSuccessful: React.FC = () => {
  const router = useRouter();

  const [status, setStatus] = useState<string | null>(null);
  const [customerEmail, setCustomerEmail] = useState<string>('');
  const [errorMessage, setErrorMessage] = useState<string>('');

  useEffect(() => {
    console.log("OrderSuccessful component mounted");
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    const sessionId = urlParams.get('session_id');

    if (sessionId) {
      checkoutSessionStatus(sessionId)
        .then((response) => {
          setStatus(response.status);
          setCustomerEmail(response.email ?? '');
          console.log("Checkout session status:", response.status);
          if(response.status === 'complete') {
            setCurrentBasketId(null);
          }
        })
        .catch((error) => {
          setErrorMessage(error.toString());
        });
    }
  }, []);


  const redirectToSeatPlan = () => {
    router.push('/'); // or any other page
  };
  
  useEffect(() => {
    // Handle payment success/failure logic

    if (status === 'open') {
      redirectToSeatPlan();
    }
  }, [router, status]);


  if (status === 'complete') {
    return (
      <section id="success">
        <p>
          We appreciate your business! A confirmation email will be sent to {customerEmail}.

          If you have any questions, please email <a href="mailto:orders@example.com">orders@example.com</a>.
        </p>
        <button onClick={redirectToSeatPlan}>
          Continue Shopping
        </button>
      </section>
    )
  } else if (status === 'open') {
    return null;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '20px' }}>
      <div>
        <p>
          Current status: {status}.
          If you have any questions, please email <a href="mailto:orders@example.com">orders@example.com</a>.
          {errorMessage && <span>Error: {errorMessage}</span>}
        </p>
        <button onClick={redirectToSeatPlan}>
          Continue Shopping
        </button>
      </div>
      
    </div>
  )
}

export default OrderSuccessful;