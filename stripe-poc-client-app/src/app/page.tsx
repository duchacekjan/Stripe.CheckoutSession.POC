"use client";

import React, {useState, useEffect} from "react";
import {Event, Performance, Seat} from "@/types/Inventory";
import {addSeatsToOrder, createOrder, getEvents, getSeats} from "@/utils/api";
import {getCurrentBasketId, setCurrentBasketId} from "@/utils/basketIdProvider";
import {useRouter} from "next/navigation";

const SeatPlan: React.FC = () => {
  const router = useRouter();
  const [events, setEvents] = useState<Event[]>([]);
  const [selectedEvent, setSelectedEvent] = useState<Event | null>(null);
  const [selectedPerformance, setSelectedPerformance] = useState<Performance | null>(null);
  const [seats, setSeats] = useState<Seat[]>([]);
  const [selectedSeats, setSelectedSeats] = useState<Seat[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [basketId, setBasketId] = useState<string | null>(null);

  useEffect(() => {
    const fetchEvents = async () => {
      try {
        setIsLoading(true);
        const response = await getEvents();
        setEvents(response.events);
      } catch (err) {
        setError('Failed to fetch events');
        console.error('Error fetching events:', err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchEvents().catch(_ => setError('An unexpected error occurred while fetching events.'));
  }, []);

  // Load basketId from localStorage on component mount
  useEffect(() => {
    const storedBasketId = getCurrentBasketId();
    setBasketId(storedBasketId);
  }, []);


  const fetchSeats = async (performanceId: number) => {
    try {
      setIsLoading(true);
      const response = await getSeats(performanceId);
      setSeats(response.seats);
    } catch (err) {
      setError('Failed to fetch seats');
      console.error('Error fetching seats:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleEventChange = (eventId: string) => {
    if (eventId === '') {
      setSelectedEvent(null);
      setSelectedPerformance(null);
      setSeats([]);
      setSelectedSeats([]);

      return;
    }

    const event = events.find(e => e.id.toString() === eventId);
    setSelectedEvent(event || null);
    setSelectedPerformance(null); // Reset performance selection when event changes
    setSeats([]);
    setSelectedSeats([]);
  };

  const handlePerformanceChange = (performanceId: string) => {
    if (performanceId === '' || !selectedEvent) {
      setSelectedPerformance(null);
      setSeats([]);
      setSelectedSeats([]);
      return;
    }

    const performance = selectedEvent.performances.find(p => p.id.toString() === performanceId);
    setSelectedPerformance(performance || null);
    if (performance) {
      fetchSeats(performance.id).catch(_ => setError('An unexpected error occurred while fetching seats.'));
    }
  };

  const formatPerformanceTime = (startTime: string, duration: number) => {
    const date = new Date(startTime);
    return `${date.toLocaleString()} (${duration} mins)`;
  };

  const handleSeatToggle = (seatId: number) => {
    const seat = seats.find(s => s.id === seatId);
    if (seat?.isAvailable !== true) return; // Don't allow toggling ordered seats

    const isSelected = selectedSeats.some(s => s.id === seatId);
    if (isSelected) {
      setSelectedSeats(selectedSeats.filter(s => s.id !== seatId));
    } else {
      if (seat) {
        setSelectedSeats([...selectedSeats, seat]);
      }
    }
  };

  const bookTickets = async () => {
    if (selectedSeats.length === 0) {
      setError('Please select at least one seat to book.');
      return;
    }

    try {
      setIsLoading(true);

      const response = basketId
        ? await addSeatsToOrder(basketId!, selectedSeats.map(seat => seat.id))
        : await createOrder(selectedSeats.map(seat => seat.id));

      setBasketId(response.basketId);
      setCurrentBasketId(response.basketId);

      setSelectedSeats([]);
      router.push('./../checkout-session');
    } catch (err) {
      setError('Failed to book tickets');
      console.error('Error booking tickets:', err);
    } finally {
      setIsLoading(false);
    }
  };

  // Group seats by row
  const seatsByRow = seats.reduce((acc, seat) => {
    const rowKey = `${seat.row} (£ ${seat.amount})`;
    if (!acc[rowKey]) {
      acc[rowKey] = [];
    }
    acc[rowKey].push(seat);
    return acc;
  }, {} as Record<string, Seat[]>);

  // Sort rows alphabetically and seats by number within each row
  const sortedRows = Object.keys(seatsByRow).sort();

  const handleReload = () => {
    setError(null);
    setCurrentBasketId(null);
    window.location.reload();
  }

  const handleRedirectToBasket = () => {
    router.push('/checkout-session');
  }

  const handleBuyVoucher = () => {
    router.push('/vouchers');
  };

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

        <h3 style={{marginBottom: '8px'}}>
          Select Event and Performance
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

            {/* Event Selection */}
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
                Select Event
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
                value={selectedEvent?.id || ''}
                onChange={(e) => handleEventChange(e.target.value)}
              >
                <option value="">Choose an event...</option>
                {events.map((event) => (
                  <option key={event.id} value={event.id}>
                    {event.name}
                  </option>
                ))}
              </select>
            </div>


            {/* Performance Selection */}
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
                Select Performance
              </label>
              <select
                id="performance-select"
                style={{
                  width: '100%',
                  padding: '16px',
                  fontSize: '18px',
                  border: '1px solid #d1d5db',
                  borderRadius: '6px',
                  outline: 'none',
                  backgroundColor: 'white',
                  cursor: selectedEvent ? 'pointer' : 'not-allowed'
                }}
                value={selectedPerformance?.id || ''}
                onChange={(e) => handlePerformanceChange(e.target.value)}
                disabled={!selectedEvent}
              >
                <option value="">
                  {selectedEvent ? 'Choose a performance...' : 'Select an event first'}
                </option>
                {selectedEvent?.performances.map((performance) => (
                  <option key={performance.id} value={performance.id}>
                    {formatPerformanceTime(performance.performanceDate, performance.durationMinutes)}
                  </option>
                ))}
              </select>
            </div>
            
            <div style={{
              display: 'flex',
              marginTop: 'auto',
              flexDirection: 'column-reverse',
              alignItems: 'flex-end',
              justifyContent: 'flex-end',
            }}>
              <button onClick={handleBuyVoucher}>Buy Voucher</button>
            </div>

            <div style={{
              display: 'flex',
              marginTop: 'auto',
              flexDirection: 'column-reverse',
              alignItems: 'flex-end',
              justifyContent: 'flex-end',
            }}>
              <button disabled={!basketId} onClick={handleRedirectToBasket}>Basket</button>
            </div>
          </div>
        </div>

        {selectedPerformance && (
          <>
            <div style={{margin: '8px 0'}}>
              <h3>Seating plan</h3>
            </div>
            <div style={{
              backgroundColor: 'white',
              borderRadius: '8px',
              boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
              padding: '8px',
              maxHeight: 'calc(100dvh - 200px)',
              overflowY: 'auto'
            }}>
              <div style={{
                display: 'flex',
                gap: '8px'
              }}>
                <div style={{flexGrow: 1}}>
                  {sortedRows.map((row) => {
                    const rowSeats = seatsByRow[row].sort((a, b) => a.number - b.number);
                    return (
                      <div key={row} style={{marginBottom: '20px'}}>
                        <div style={{
                          fontSize: '18px',
                          fontWeight: '500',
                          color: '#374151',
                          marginBottom: '12px'
                        }}>
                          Row {row}
                        </div>

                        <div style={{
                          display: 'flex',
                          flexWrap: 'wrap',
                          gap: '8px'
                        }}>
                          {rowSeats.map((seat) => (
                            <div key={seat.id}>
                              <input
                                type="checkbox"
                                id={seat.id.toString()}
                                checked={!seat.isAvailable || selectedSeats.some(s => s.id === seat.id)}
                                disabled={!seat.isAvailable}
                                onChange={() => handleSeatToggle(seat.id)}
                                title={`Row ${seat.row}, Seat ${seat.number}`}
                                style={{
                                  width: '20px',
                                  height: '20px',
                                  margin: '4px',
                                  cursor: !seat.isAvailable ? 'not-allowed' : 'pointer',
                                  opacity: !seat.isAvailable ? 0.5 : 1
                                }}
                              />
                              <label
                                htmlFor={seat.id.toString()}
                                style={{
                                  fontSize: '12px',
                                  color: !seat.isAvailable ? '#9ca3af' : '#374151',
                                  cursor: !seat.isAvailable ? 'not-allowed' : 'pointer',
                                  display: 'block',
                                  textAlign: 'center',
                                  marginTop: '2px'
                                }}
                                title={`Row ${seat.row}, Seat ${seat.number}`}
                              >
                                {seat.number}
                              </label>
                            </div>
                          ))}
                        </div>
                      </div>
                    );
                  })}
                </div>
                {selectedSeats.length > 0 && (
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
                        flexDirection: 'row',
                        flexWrap: 'wrap',
                        gap: '8px'
                      }}>
                        {selectedSeats.map((seat) => (
                          <div style={{
                            display: 'flex',
                            alignItems: 'center',
                            padding: '4px',
                            backgroundColor: 'lightgray',
                            borderRadius: '4px',
                            marginRight: '8px',
                            cursor: 'pointer',
                            gap: '2px'
                          }}
                               key={seat.id}
                               onClick={() => handleSeatToggle(seat.id)
                               }>
                            <div>
                              <span style={{fontSize: '11pt', fontWeight: 'bold'}}>
                                {seat.row} {seat.number}
                              </span>
                            </div>
                            <div>
                              <span style={{fontSize: '11pt'}}>
                                (£ {seat.amount})
                              </span>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                )}
              </div>
              {selectedSeats.length > 0 && (
                <button onClick={bookTickets}>
                  Book tickets
                </button>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default SeatPlan;