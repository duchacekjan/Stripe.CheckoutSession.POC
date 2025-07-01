"use client";

import {useRouter} from "next/navigation";
import {useEffect, useState} from "react";
import {checkoutSessionStatus, setPaid} from "@/utils/api";
import {getCurrentBasketId, setCurrentBasketId} from "@/utils/basketIdProvider";
import VouchersSummary from "@/app/order-successful/Components/VouchersSummary";
import Actions from "@/app/order-successful/Components/Actions";

const OrderSuccessful: React.FC = () => {
  const router = useRouter();

  const [status, setStatus] = useState<string | null>(null);
  const [customerEmail, setCustomerEmail] = useState<string>('');
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [vouchers, setVouchers] = useState<string[]>([]);

  useEffect(() => {
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    const sessionId = urlParams.get('session_id');

    if (sessionId) {
      checkoutSessionStatus(sessionId)
        .then((response) => {
          setStatus(response.status);
          setCustomerEmail(response.email ?? '');
          console.log("Checkout session status:", response.status);
          if (response.status === 'complete' && response.basketId) {
            setCurrentBasketId(null);
            setPaid(response.basketId)
              .then(response => {
                setVouchers(response.voucherCodes);
              })
              .catch(error => {
                console.log(error);
              });
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

  const redirectTo = (target: string) => {
    router.push(target); // or any other page
  };

  useEffect(() => {
    // Handle payment success/failure logic

    if (status === 'open') {
      redirectToSeatPlan();
    }
  }, [router, status]);


  if (status === 'complete') {
    return (
      <>
        <div style={{display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '20px'}}>
          <div>
            <p>
              We appreciate your business! <br/>
              A confirmation email will be sent to <span style={{fontWeight: 'bold'}}>{customerEmail}</span>.<br/>

              If you have any questions, please email <a href="mailto:orders@example.com">orders@example.com</a>.
            </p>
            <Actions redirectTo={redirectTo}/>
          </div>
          {vouchers.length > 0 && (
            <div>
              <VouchersSummary voucherCodes={vouchers}/>
            </div>
          )}
        </div>
      </>
    )
  } else if (status === 'open') {
    return null;
  }

  return (
    <div style={{display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '20px'}}>
      <div>
        <p>
          Current status: {status}. <br/>
          If you have any questions, please email <a href="mailto:orders@example.com">orders@example.com</a>. <br/>
          {errorMessage && <span>Error: {errorMessage}</span>}
        </p>
        <Actions redirectTo={redirectTo}/>
      </div>

    </div>
  )
}

export default OrderSuccessful;