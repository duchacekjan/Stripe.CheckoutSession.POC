'use client';

import React, {useEffect, useState} from "react";
import {getCurrentBasketId, setCurrentBasketId} from "@/utils/basketIdProvider";
import {GetTicketsResponse, Ticket, UpdateStatus} from "@/types/Orders";
import GroupedTicket from "@/app/checkout-session/Components/GroupedTicket";
import {useCheckout} from "@stripe/react-stripe-js";
import {useRouter} from "next/navigation";
import {useApi} from "@/utils/api";

interface CheckoutSummaryProps {
  setHasPerformance: (hasPerformance: boolean) => void;
  bookingProtection: boolean;
  setBookingProtection: (protection: boolean) => void;
}

const CheckoutSummary: React.FC<CheckoutSummaryProps> = ({
                                                           setHasPerformance,
                                                           bookingProtection,
                                                           setBookingProtection
                                                         }) => {
  const checkout = useCheckout();
  const router = useRouter();
  const api = useApi();
  const [basketId, setBasketId] = useState<string | null>(null);
  const [tickets, setTickets] = useState<Record<number, Ticket[]>>([]);
  const [basketTotal, setBasketTotal] = useState<string>('');

  useEffect(() => {
    const currentBasketId = getCurrentBasketId();
    if (currentBasketId) {
      setBasketId(currentBasketId);
      api.orders.getTickets(currentBasketId)
        .then(response => {
          handleTicketChanged(response);
        })
        .catch(error => {
          console.error("Error fetching tickets:", error);
          // Handle error, maybe redirect or show a message
        });
    } else {
      console.log("No basket ID found. Redirecting to seat plan.");
    }
  }, [bookingProtection]);

  const updateCheckout = async (ticketsToRemove: number[]) => {
    const response = await api.orders.removeTickets(basketId!, ticketsToRemove);
    if (response.status === UpdateStatus.Emptied) {
      setCurrentBasketId(null);
      router.push('/');
      response.status = UpdateStatus.Error;
      return Promise.reject(response);
    }
    return response;
  }

  const handleTicketChanged = (response: GetTicketsResponse) => {
    setTickets(response.tickets);
    setBasketTotal(response.totalPrice.toFixed(2))
    const allTickets = Object.values(response.tickets).flat();
    setHasPerformance(allTickets.some(ticket => ticket.performanceId > 0));
    setBookingProtection(allTickets.some(ticket => ticket.performanceId === -2));
  }

  const handleRemovedTicket = async (ticketsToRemove: number[]) => {

    try {
      const removeTicketsResponse = await api.orders.removeTickets(basketId!, ticketsToRemove);
      if (removeTicketsResponse.status === UpdateStatus.Emptied) {
        setCurrentBasketId(null);
        router.push('/');
        return
      }
      const response = await checkout.runServerUpdate(() => api.checkoutSessions.update(basketId!))
      if (response.type !== 'success') {
        // set error state
        return;
      }

      const ticketsResponse = await api.orders.getTickets(basketId!)
      handleTicketChanged(ticketsResponse);
    } catch (error) {
      console.error("Error updating tickets:", error);
    }
  };

  if (!basketTotal) {
    return null;
  }

  return (
    <div style={{
      display: "flex",
      flexDirection: "column",
    }}>
      <div style={{
        display: 'flex',
        width: '280px',
      }}>
        <div style={{
          marginTop: '8px',
          padding: '8px',
          backgroundColor: '#f0f9ff',
          borderRadius: '6px',
          flexGrow: 1,
          border: '1px solid #0ea5e9'
        }}>
          <div style={{
            fontSize: '18px',
            fontWeight: '500',
            color: '#0c4a6e',
            marginBottom: '8px',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            gap: '8px'
          }}>
            <h4>Basket summary</h4>
            {Object.values(tickets).map((orderItemTickets, index) => (
              <GroupedTicket tickets={orderItemTickets}
                             key={`${orderItemTickets[0].performanceId}-${orderItemTickets[0].priceId}-${index}`}
                             ticketsRemoved={removedTickets => handleRemovedTicket(removedTickets.map(s => s.seatId))}/>
            ))}
            <div
              style={{display: 'flex', justifyContent: 'space-between', gap: '4px', marginTop: '8px', width: '100%'}}>
              <div>
                <span style={{fontSize: '12pt', fontWeight: 'bold'}}>
                  Basket Total:
                </span>
              </div>
              <div>
              <span style={{fontSize: '12pt', fontWeight: 'bold'}}>
                £ {basketTotal}
              </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
export default CheckoutSummary;