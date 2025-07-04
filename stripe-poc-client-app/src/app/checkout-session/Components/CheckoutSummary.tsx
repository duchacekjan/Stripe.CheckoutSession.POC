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
  setIsLoading: (loading: boolean) => void;
  setHasVoucher: (hasVoucher: boolean) => void;
}

const CheckoutSummary: React.FC<CheckoutSummaryProps> = ({
                                                           setHasPerformance,
                                                           bookingProtection,
                                                           setBookingProtection,
                                                           setIsLoading,
                                                           setHasVoucher
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
      setIsLoading(true);
      api.orders.getBasketContent(currentBasketId)
        .then(response => {
          handleTicketChanged(response);
        })
        .catch(error => {
          setIsLoading(false);
          console.error("Error fetching tickets:", error);
          // Handle error, maybe redirect or show a message
        });
    } else {
      console.log("No basket ID found. Redirecting to seat plan.");
      setIsLoading(false);
    }
  }, [bookingProtection]);

  const handleTicketChanged = (response: GetTicketsResponse) => {
    setTickets(response.tickets);
    setBasketTotal(response.totalPrice.toFixed(2))
    const allTickets = Object.values(response.tickets).flat();
    setHasPerformance(allTickets.some(ticket => ticket.performanceId > 0));
    setHasVoucher(allTickets.some(ticket => ticket.performanceId === -1));
    setBookingProtection(allTickets.some(ticket => ticket.performanceId === -2));
    setIsLoading(false);
  }

  const handleRemovedTicket = async (ticketsToRemove: number[]) => {

    setIsLoading(true);
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

      const ticketsResponse = await api.orders.getBasketContent(basketId!)
      handleTicketChanged(ticketsResponse);
    } catch (error) {
      console.error("Error updating tickets:", error);
      setIsLoading(false);
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
        padding: '8px',
        backgroundColor: '#f0f9ff',
        borderRadius: '6px',
        flexGrow: 1,
        border: '1px solid #0ea5e9',
        display: 'flex',
        flexDirection: 'column'
      }}>
        <div style={{
          color: '#0c4a6e',
          marginBottom: '8px',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'flex-start',
          gap: '8px',
          height: 'calc(100vh - 150px)',
          overflowY: 'auto'
        }}>
          <h4>Basket summary</h4>
          {Object.values(tickets).map((orderItemTickets, index) => (
            <GroupedTicket tickets={orderItemTickets}
                           key={`${orderItemTickets[0].performanceId}-${orderItemTickets[0].priceId}-${index}`}
                           ticketsRemoved={removedTickets => handleRemovedTicket(removedTickets.map(s => s.seatId))}/>
          ))}
        </div>
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          gap: '4px',
          marginTop: '8px',
          color: '#0c4a6e',
          width: '100%'
        }}>
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
  );
};
export default CheckoutSummary;