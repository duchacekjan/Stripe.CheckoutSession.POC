'use client';

import React, {useEffect, useState} from "react";
import {getCurrentBasketId, setCurrentBasketId} from "@/utils/basketIdProvider";
import {getOrderTickets, removeOrderTickets} from "@/utils/api";
import {Ticket, UpdateStatus} from "@/types/Orders";
import GroupedTicket from "@/app/checkout-session/Components/GroupedTicket";
import {useCheckout} from "@stripe/react-stripe-js";
import {useRouter} from "next/navigation";

interface CheckoutSummaryProps {
  setHasPerformance: (hasPerformance: boolean) => void;
  bookingProtection: boolean;
  setBookingProtection: (protection: boolean) => void;
}

const CheckoutSummary: React.FC<CheckoutSummaryProps> = ({setHasPerformance, bookingProtection, setBookingProtection}) => {
  const checkout = useCheckout();
  const router = useRouter();
  const [basketId, setBasketId] = useState<string | null>(null);
  const [tickets, setTickets] = useState<Ticket[]>([]);

  useEffect(() => {
    const currentBasketId = getCurrentBasketId();
    if (currentBasketId) {
      setBasketId(currentBasketId);
      getOrderTickets(currentBasketId)
        .then(response => {
          setTickets(response.tickets);
          setHasPerformance(response.tickets.some(ticket => ticket.performanceId > 0));
          setBookingProtection(response.tickets.some(ticket => ticket.performanceId === -2));
        })
        .catch(error => {
          console.error("Error fetching tickets:", error);
          // Handle error, maybe redirect or show a message
        });
    } else {
      console.log("No basket ID found. Redirecting to seat plan.");
    }
  }, [bookingProtection]);

  const groupedTickets = tickets.reduce((acc, ticket) => {
    const rowKey = `${ticket.performanceId}-${ticket.priceId}`;
    if (!acc[rowKey]) {
      acc[rowKey] = [];
    }
    acc[rowKey].push(ticket);
    return acc;
  }, {} as Record<string, Ticket[]>);

  const updateCheckout = async (ticketsToRemove: number[]) => {
    const response = await removeOrderTickets(basketId!, checkout.id, ticketsToRemove);
    if (response.status === UpdateStatus.Emptied) {
      setCurrentBasketId(null);
      router.push('/');
      return Promise.reject(response);
    }
    return response;
  }
  const handleRemovedTicket = async (ticketsToRemove: number[]) => {

    try {
      const response = await checkout.runServerUpdate(() => updateCheckout(ticketsToRemove));
      if (response.type !== 'success') {
        // set error state
        return;
      }

      const ticketsResponse = await getOrderTickets(basketId!)
      setTickets(ticketsResponse.tickets);
    } catch (error) {
      console.error("Error updating tickets:", error);
    }
  };

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
            {Object.values(groupedTickets).map((tickets, index) => (
              <GroupedTicket tickets={tickets}
                             key={`${tickets[0].performanceId}-${tickets[0].priceId}-${index}`}
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
                £ {tickets.reduce((total, ticket) => total + ticket.price, 0).toFixed(2)}
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