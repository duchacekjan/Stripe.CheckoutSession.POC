"use client";

import {CheckoutContextValue, CurrencySelectorElement, PaymentElement, useCheckout } from "@stripe/react-stripe-js";
import { useRouter } from "next/navigation";
import { useState } from "react";

const validateEmail = async (email:string, checkout: CheckoutContextValue) => {
  const updateResult = await checkout.updateEmail(email);
  const isValid = updateResult.type !== "error";

  return { isValid, message: !isValid ? updateResult.error.message : null };
}

// @ts-ignore
const EmailInput = ({ email, setEmail, error, setError }) => {
  const checkout = useCheckout();

  const handleBlur = async () => {
    if (!email) {
      return;
    }

    const { isValid, message } = await validateEmail(email, checkout);
    if (!isValid) {
      setError(message);
    }
  };

  const handleChange = (e:any) => {
    setError(null);
    setEmail(e.target.value);
  };

  return (
    <>
      <label>
        Email
        <input
          id="email"
          type="text"
          value={email}
          onChange={handleChange}
          onBlur={handleBlur}
          style={{ fontSize: '12pt' }}
          placeholder="you@example.com"
        />
      </label>
      {error && <div id="email-errors">{error}</div>}
    </>
  );
};

const CheckoutSessionForm: React.FC = () => {
  const router = useRouter();
  const [email, setEmail] = useState<string>('');
  const [emailError, setEmailError] = useState<string|null>(null);
  const [message, setMessage] = useState<string|null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  
  const checkout = useCheckout();
  // checkout.onChange('change', (session) => {
  //   // Handle changes to the checkout session
  // });
  
  const handleShopMore = () => {
    router.push('/');
  }

  const handleBuyVoucher = () => {
    router.push('/vouchers');
  }

  const handleSubmit = async (e:any) => {
    e.preventDefault();

    setIsLoading(true);

    const { isValid, message } = await validateEmail(email, checkout);
    if (!isValid) {
      setEmailError(message);
      setMessage(message);
      setIsLoading(false);
      return;
    }

    const confirmResult = await checkout.confirm();

    // This point will only be reached if there is an immediate error when
    // confirming the payment. Otherwise, your customer will be redirected to
    // your `return_url`. For some payment methods like iDEAL, your customer will
    // be redirected to an intermediate site first to authorize the payment, then
    // redirected to the `return_url`.
    if (confirmResult.type === 'error') {
      setMessage(confirmResult.error.message);
    }

    setIsLoading(false);
  };

  return (
    <form onSubmit={handleSubmit}>
      <div style={{ display: 'flex', gap: '8px'}}>
        <button style={{ marginBottom: '8px' }}
                onClick={handleShopMore}>
          Shop more
        </button>
        <button style={{ marginBottom: '8px' }}
                onClick={handleBuyVoucher}>
          Buy voucher
        </button>
      </div>
      <div style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '8px',
        backgroundColor: 'white',
        borderRadius: '8px',
        border: '1px solid #e5e7eb',
        padding: '8px',
        marginBottom: '8px'
      }}>
        <h4>Contact info</h4>
        <EmailInput
          email={email}
          setEmail={setEmail}
          error={emailError}
          setError={setEmailError}
        />
      </div>
      
      <h4>Payment</h4>
      <PaymentElement id="payment-element" />
      <div style={{display: 'flex', gap: '8px', marginTop: '8px'}}>
        <button disabled={isLoading} id="submit">
          {isLoading ? (
            <div className="spinner"></div>
          ) : (
            `Pay ${checkout.total.total.amount} now`
          )}
        </button>
        {/*<CurrencySelectorElement />*/}
      </div>
      {/* Show any error or success messages */}
      {message && <div id="payment-message">{message}</div>}
    </form>
  );
}

export default CheckoutSessionForm;