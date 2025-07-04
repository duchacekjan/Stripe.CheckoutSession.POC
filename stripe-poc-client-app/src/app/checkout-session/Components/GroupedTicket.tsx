'use client';

import {Ticket} from "@/types/Orders";

const GroupedTicket: React.FC<{
  tickets: Ticket[],
  ticketsRemoved: (ticket: Ticket[]) => void,
}> = ({tickets, ticketsRemoved}) => {
  const info = tickets.length > 0 ? tickets[0] : null;
  if (!info) {
    return null;
  }
  return (
    <div
      key={`${info.performanceId}-${info.priceId}`}
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: '8px',
        padding: '8px',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        maxWidth: '300px',
        backgroundColor: 'white',
        boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
      }}>
      {/* First row: Event name (left) and Dismiss button (right) */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: '8px'
      }}>
        <span style={{
          textAlign: 'left',
          fontSize: '12pt'
        }}>{info.eventName}</span>
        {info.performanceId > -2 && (
          <button
            type={'button'}
            onClick={() => ticketsRemoved(tickets)}
            style={{
              color: '#ef4444',
              background: 'white',
              border: '1px solid #e5e7eb',
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '24px',
              width: '32px',
              marginLeft: '4px',
              height: '32px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
            onMouseOver={(e) => {
              e.currentTarget.style.color = '#dc2626';
              e.currentTarget.style.borderColor = '#dc2626';
            }}
            onMouseOut={(e) => {
              e.currentTarget.style.color = '#ef4444';
              e.currentTarget.style.borderColor = '#e5e7eb';
            }}
          >
            ✕
          </button>
        )}
      </div>

      {/* Second row: Date */}
      {info.performanceId > 0 && (
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          flexDirection: 'row',
          flexWrap: 'wrap',
          alignItems: 'center',
          marginBottom: '8px'
        }}>
          <span style={{fontSize: '12pt'}}>{new Date(info.performanceDate).toLocaleDateString()}</span>
        </div>
      )}

      {/* Third row: Seats */}
      {info.performanceId > 0 && (
        <div style={{
          display: 'flex',
          flexDirection: 'row',
          flexWrap: 'wrap',
          width: '100%',
          alignItems: 'center',
          marginBottom: '8px'
        }}>
          {tickets.map((ticket) => (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                flexDirection: 'column',
                justifyItems: 'center',
                padding: '2px 4px',
                backgroundColor: '#f3f4f6',
                borderRadius: '4px',
                minWidth: '24px',
                marginRight: '8px',
                cursor: 'pointer'
              }}
              onClick={() => ticketsRemoved([ticket])}
              key={ticket.seatId}>
              <div>
                <span style={{fontSize: '12pt'}}>
                  {`${ticket.seatRow} ${ticket.seatNumber}`}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Third row: Seats */}
      {info.performanceId === -1 && (
        <div style={{
          display: 'flex',
          flexDirection: 'row',
          flexWrap: 'wrap',
          width: '100%',
          alignItems: 'center',
          marginBottom: '8px'
        }}>
          {tickets.map((ticket, index) => (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                flexDirection: 'column',
                justifyItems: 'center',
                padding: '2px 4px',
                backgroundColor: '#f3f4f6',
                borderRadius: '4px',
                marginRight: '8px',
                minWidth: '24px',
                cursor: 'pointer'
              }}
              onClick={() => ticketsRemoved([ticket])}
              key={ticket.seatId}>
              <div>
                <span style={{fontSize: '12pt'}}>
                  {`${index + 1}`}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Fourth row: Row and seat number */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: '8px'
      }}>
        <span>{tickets.length}x</span>
        <span style={{
          fontWeight: 'bold',
          fontSize: '12pt'
        }}>£{info.price}</span>
      </div>
    </div>

  );
};
export default GroupedTicket;