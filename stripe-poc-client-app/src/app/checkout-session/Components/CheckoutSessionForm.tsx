"use client";

import {CheckoutContextValue, CurrencySelectorElement, PaymentElement, useCheckout} from "@stripe/react-stripe-js";
import {useState} from "react";
import CustomerElements from "@/app/checkout-session/Components/CustomerElements";
import {useApi} from "@/utils/api";

interface CheckoutSessionFormProps {
  basketId: string;
  hasPerformance: boolean;
  bookingProtection: boolean;
  setBookingProtection: (protection: boolean) => void;
  isLoading: boolean;
  setIsLoading: (loading: boolean) => void;
}

const validateEmail = async (email: string, checkout: CheckoutContextValue) => {
  const updateResult = await checkout.updateEmail(email);
  const isValid = updateResult.type !== "error";

  return {isValid, message: !isValid ? updateResult.error.message : null};
}

const CheckoutSessionForm: React.FC<CheckoutSessionFormProps> = ({
                                                                   basketId,
                                                                   hasPerformance,
                                                                   bookingProtection,
                                                                   setBookingProtection,
                                                                   isLoading,
                                                                   setIsLoading
                                                                 }) => {
  const api = useApi();
  const [email, setEmail] = useState<string>('jan.duchacek@itixo.com');
  const [emailError, setEmailError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const checkout = useCheckout();
  //TODO this code from docs is not working, need to investigate
  // checkout.onChange('change', (session) => {
  //   // Handle changes to the checkout session
  // });

  const handleSubmit = async (e: any) => {

    e.preventDefault();
    console.warn("Submitting checkout form with basketId:", basketId);

    setEmailError(null);
    setMessage(null);
    setIsLoading(true);

    const {isValid, message} = await validateEmail(email, checkout);
    if (!isValid) {
      setEmailError(message);
      setMessage(message);
      setIsLoading(false);
      return;
    }

    try {
      await api.orders.finalizeOrder(basketId);
      const confirmResult = await checkout.confirm();

      // This point will only be reached if there is an immediate error when
      // confirming the payment. Otherwise, your customer will be redirected to
      // your `return_url`. For some payment methods like iDEAL, your customer will
      // be redirected to an intermediate site first to authorize the payment, then
      // redirected to the `return_url`.
      if (confirmResult.type === 'error') {
        setMessage(confirmResult.error.message);
        await api.orders.setPaymentFailed(basketId);
      }
    } catch (error) {
      setMessage(`Failed to finalize order. Please try again. ${error}`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleBookingProtectionChange = async (protection: boolean) => {
    console.log("FORM => booking protection to:", protection);
    try {
      await api.orders.updatedBookingProtection(basketId, protection);
      await checkout.runServerUpdate(() => api.checkoutSessions.update(basketId))
      setBookingProtection(protection);
    } catch (error) {
      setMessage("Failed to update booking protection. Please try again later.");
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div style={{
        display: 'flex', 
        flexDirection: 'column', 
        gap: '8px'}}>
        <CustomerElements
          email={email}
          setEmail={setEmail}
          error={emailError}
          setError={setEmailError}
          validateEmail={validateEmail}
          hasPerformance={hasPerformance}
          bookingProtection={bookingProtection}
          setBookingProtection={handleBookingProtectionChange}
        />

        <h4>Payment</h4>
        <PaymentElement id="payment-element"/>
        <CurrencySelectorElement/>
        <button disabled={isLoading} id="submit">
          Pay {checkout.total.total.amount} now
        </button>
        {/* Show any error or success messages */}
        {message && <div style={{color: 'red', fontSize: '11pt'}}>{message}</div>}
      </div>
    </form>
  );
}

export default CheckoutSessionForm;