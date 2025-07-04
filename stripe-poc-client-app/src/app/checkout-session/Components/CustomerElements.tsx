'use client';

import {CheckoutContextValue, useCheckout} from "@stripe/react-stripe-js";
import {useState} from "react";

interface CustomerElementsProps {
  email: string;
  setEmail: (email: string) => void;
  error: string | null;
  setError: (error: string | null) => void;
  validateEmail: (email: string, checkout: CheckoutContextValue) => Promise<{
    isValid: boolean;
    message: string | null
  }>;
  hasPerformance: boolean;
  bookingProtection: boolean;
  setBookingProtection: (protection: boolean) => Promise<void>;
}


const CustomerElements: React.FC<CustomerElementsProps> = ({
                                                             email,
                                                             setEmail,
                                                             error,
                                                             setError,
                                                             validateEmail,
                                                             hasPerformance,
                                                             bookingProtection,
                                                             setBookingProtection
                                                           }) => {
  const checkout = useCheckout();
  const [bookingProtectionValue, setBookingProtectionValue] = useState<string>(bookingProtection ? 'yes' : 'no');
  const handleBlur = async () => {
    if (!email) {
      return;
    }

    const {isValid, message} = await validateEmail(email, checkout);
    if (!isValid) {
      setError(message);
    }
  };

  const handleChange = (e: any) => {
    setError(null);
    setEmail(e.target.value);
  };

  const handleBookingProtectionChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    setBookingProtectionValue(newValue);
    const protection = newValue === 'yes';

    console.log("ELEMENTS => booking protection to:", protection);
    await setBookingProtection(protection);
  }
  return (
    <>
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
        <h4>Customer section</h4>
        <div style={{display: 'flex', flexDirection: 'column', gap: '4px'}}>
          <label htmlFor={email}> Email </label>
          <input
            id="email"
            type="text"
            value={email}
            onChange={handleChange}
            onBlur={handleBlur}
            style={{fontSize: '12pt'}}
            placeholder="you@example.com"
          />
          {error && <div style={{color: 'red', fontSize: '11pt'}}>{error}</div>}
        </div>
        {hasPerformance && (
          <div style={{display: 'flex', flexDirection: 'column', gap: '8px'}}>
            <div>
              <label style={{marginBottom: '4px', display: 'block'}}>
                Booking protection
              </label>

              <div style={{display: 'flex', gap: '8px', alignItems: 'center'}}>
                <label style={{display: 'flex', alignItems: 'center', gap: '4px'}}>
                  <input
                    type="radio"
                    name="customerType"
                    value="no"
                    checked={bookingProtectionValue === 'no'}
                    onChange={handleBookingProtectionChange}
                  />
                  No protection
                </label>
                <label style={{display: 'flex', alignItems: 'center', gap: '4px'}}>
                  <input
                    type="radio"
                    name="customerType"
                    value="yes"
                    checked={bookingProtectionValue === 'yes'}
                    onChange={handleBookingProtectionChange}
                  />
                  £5 Booking protection
                </label>
              </div>
            </div>
          </div>
        )}

      </div>
    </>
  );
};

export default CustomerElements;