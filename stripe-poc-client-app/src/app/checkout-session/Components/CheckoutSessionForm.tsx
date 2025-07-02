"use client";

import {CheckoutContextValue, CurrencySelectorElement, PaymentElement, useCheckout} from "@stripe/react-stripe-js";
import {useRouter} from "next/navigation";
import {useState} from "react";
import CustomerElements from "@/app/checkout-session/Components/CustomerElements";
import {updatedBookingProtection} from "@/utils/api";

interface CheckoutSessionFormProps {
  basketId: string;
  hasPerformance: boolean;
  bookingProtection: boolean;
  setBookingProtection: (protection: boolean) => void;
}

const validateEmail = async (email: string, checkout: CheckoutContextValue) => {
  const updateResult = await checkout.updateEmail(email);
  const isValid = updateResult.type !== "error";

  return {isValid, message: !isValid ? updateResult.error.message : null};
}

const CheckoutSessionForm: React.FC<CheckoutSessionFormProps> = ({basketId, hasPerformance, bookingProtection, setBookingProtection}) => {
  const router = useRouter();
  const [email, setEmail] = useState<string>('jan.duchacek@itixo.com');
  const [emailError, setEmailError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
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

  const handleSubmit = async (e: any) => {
    e.preventDefault();

    setIsLoading(true);

    const {isValid, message} = await validateEmail(email, checkout);
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

  const handleBookingProtectionChange = async (protection: boolean) => {
    try {
      await updatedBookingProtection(basketId, protection);
      setBookingProtection(protection);
    } catch (error) {
      setMessage("Failed to update booking protection. Please try again later.");
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div style={{display: 'flex', flexDirection: 'column', gap: '8px'}}>
        <div style={{display: 'flex', gap: '8px'}}>
          <button style={{marginBottom: '8px'}}
                  onClick={handleShopMore}>
            Shop more
          </button>
          <button style={{marginBottom: '8px'}}
                  onClick={handleBuyVoucher}>
            Buy voucher
          </button>
        </div>
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