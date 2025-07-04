'use client';

import {useEffect, useState} from "react";
import {PaidOrder, Ticket} from "@/types/Orders";
import {useRouter} from "next/navigation";
import {useApi} from "@/utils/api";

const RefundPage: React.FC = () => {
  const router = useRouter();
  const api = useApi();
  const [paidOrders, setPaidOrders] = useState<PaidOrder[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<PaidOrder | null>(null);
  const [selectedTickets, setSelectedTickets] = useState<Ticket[]>([]);
  const [selectedTicketsValues, setSelectedTicketsValues] = useState<string[]>([]);
  const [status, setStatus] = useState<string | null>('refunded');

  useEffect(() => {
    setIsLoading(true);
    api.orders.getPaidOrders()
      .then((response) => {
        setPaidOrders(response)
        setIsLoading(false);
      })
      .catch((error) => {
        console.error("Error fetching paid orders:", error);
        setError(`Error fetching paid orders:${(error as any).toString()}`);
        setIsLoading(false);
      });
  }, []);

  const handleReload = () => {
    setError(null);
    window.location.reload();
  }

  const handleOrderChange = (orderId: string) => {
    if (orderId === '') {
      setSelectedOrder(null);
      setSelectedTickets([]);
      setSelectedTicketsValues([]);
      return;
    }

    const order = paidOrders.find(e => e.orderId.toString() === orderId);
    setSelectedOrder(order || null);
  };

  const handleSelectedTicketsChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const values = Array.from(e.target.selectedOptions, option => option.value);
    const newSelectedTickets = selectedOrder?.tickets.filter(ticket => values.includes(ticket.seatId.toString())) || [];
    setSelectedTickets(newSelectedTickets);
    setSelectedTicketsValues(newSelectedTickets.map(value => value.seatId.toString()));
  };

  const handleRefund = async () => {
    setIsLoading(true);
    try {
      const basketId = selectedOrder!.basketId;
      const refundedAmount = selectedTickets.reduce((total, ticket) => total + ticket.price, 0);
      await api.orders.refund(basketId, refundedAmount);
      setIsLoading(false);
      setStatus('refunded');
    } catch (error) {
      setError(`Error executing refund:${(error as any).toString()}`);
      setIsLoading(false);
    }
  }
  
  const handleRedirect = () => {
    router.push('/'); // Redirect to seat selection or any other page
  }

  return (
    <div style={{minHeight: '100vh', backgroundColor: '#f9fafb', padding: '16px 0'}}>
      {/* Loading Overlay */}
      {isLoading && (
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
      )}

      {/* status Overlay */}
      {status === 'refunded' && (
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
            <div style={{ padding: '20px', textAlign: 'center' }}>
              <h2 style={{textAlign: 'center', marginBottom: '8px'}}>Refund Successful</h2>
              <button
                onClick={handleRedirect}>
                Seat selection
              </button>
            </div>
          </div>
        </div>
      )}

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

      <div style={{maxWidth: '1100px', margin: '0 auto', padding: '0 8px'}}>
        {/* Error Panel */}
        {error && (
          <div style={{
            marginBottom: '16px',
            padding: '16px',
            backgroundColor: '#fef2f2',
            borderRadius: '6px',
            border: '1px solid #dc2626',
            display: 'flex',
            alignItems: 'center',
            gap: '8px'
          }}>
            <div style={{
              fontSize: '18px',
              fontWeight: '500',
              color: '#dc2626'
            }}>
              {error}
            </div>
            <button
              onClick={() => handleReload()}
              title="Reload page"
              style={{
                marginLeft: 'auto',
                padding: '4px 8px',
                backgroundColor: 'transparent',
                border: '1px solid #dc2626',
                borderRadius: '4px',
                color: '#dc2626',
                cursor: 'pointer',
                fontSize: '14px'
              }}
            >
              ×
            </button>
          </div>
        )}
        <div style={{display: 'flex', gap: '8px', marginBottom: '8px'}}>
          <button onClick={handleRedirect}>
            Seat selection
          </button>
        </div>
        <h3 style={{marginBottom: '8px'}}>
          Select order
        </h3>

        <div style={{
          backgroundColor: 'white',
          borderRadius: '8px',
          boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
          padding: '8px'
        }}>
          <div style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr auto auto',
            gap: '24px'
          }}>

            {/* Order Selection */}
            <div>
              <label htmlFor="event-select"
                     style={{
                       display: 'block',
                       fontSize: '14px',
                       fontWeight: '500',
                       color: '#374151',
                       marginBottom: '8px'
                     }}
              >
                Select Order
              </label>
              <select
                id="event-select"
                style={{
                  width: '100%',
                  padding: '16px',
                  fontSize: '18px',
                  border: '1px solid #d1d5db',
                  borderRadius: '6px',
                  outline: 'none',
                  backgroundColor: 'white'
                }}
                value={selectedOrder?.orderId || ''}
                onChange={(e) => handleOrderChange(e.target.value)}
              >
                <option value="">Choose an order...</option>
                {paidOrders.map((order) => (
                  <option key={order.orderId} value={order.orderId}>
                    {order.basketId} - £{order.totalPrice}
                  </option>
                ))}
              </select>
            </div>


            {/* Tickets Selection */}
            <div>
              <label htmlFor="performance-select"
                     style={{
                       display: 'block',
                       fontSize: '14px',
                       fontWeight: '500',
                       color: '#374151',
                       marginBottom: '8px'
                     }}
              >
                Select Tickets
              </label>
              <select
                id="performance-select"
                multiple
                style={{
                  width: '100%',
                  padding: '16px',
                  fontSize: '18px',
                  border: '1px solid #d1d5db',
                  borderRadius: '6px',
                  outline: 'none',
                  backgroundColor: 'white',
                  cursor: selectedOrder ? 'pointer' : 'not-allowed'
                }}
                value={selectedTicketsValues}
                onChange={handleSelectedTicketsChange}
                disabled={!selectedOrder}
              >
                {selectedOrder?.tickets.map((ticket) => (
                  <option key={ticket.seatId} value={ticket.seatId}>
                    {ticket.seatRow} {ticket.seatNumber} - £{ticket.price}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {selectedTickets.length > 0 && (
            <div style={{display: 'flex', gap: '8px', justifyContent: 'space-between', marginTop: '8px'}}>
              <button
                style={{backgroundColor: 'red'}}
                onClick={handleRefund}
                disabled={!selectedOrder?.basketId || selectedTickets.length === 0}>
                Refund £{selectedTickets.reduce((total, ticket) => total + ticket.price, 0).toFixed(2)}
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default RefundPage;