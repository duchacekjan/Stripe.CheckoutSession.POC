'use client';

import {Voucher} from "@/types/Orders";

const VoucherCard: React.FC<{
  voucher: Voucher,
  voucherRemoved: (voucher: Voucher) => Promise<void>,
}> = ({voucher, voucherRemoved}) => {
  
  return (
    <div
      key={voucher.id}
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
        }}>Voucher</span>
          <button
            type={'button'}
            onClick={() => voucherRemoved(voucher)}
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
      </div>

      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: '8px'
      }}>
        <span>1x</span>
        <span style={{
          fontWeight: 'bold',
          fontSize: '12pt',
          color: '#22c55e'
        }}>- £{voucher.amount}</span>
      </div>
    </div>

  );
};
export default VoucherCard;