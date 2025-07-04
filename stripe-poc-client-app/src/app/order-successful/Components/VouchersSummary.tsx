"use client";
const VouchersSummary: React.FC<{ voucherCodes: string[] }> = ({voucherCodes}) => {
  return (
    <div style={{
      display: "flex",
      flexDirection: "column",
    }}>
      <div style={{
        display: 'flex'
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
            <h4>Bought vouchers</h4>
            {voucherCodes.map((voucherCode) =>
                (
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      padding: '2px',
                      backgroundColor: '#f3f4f6',
                      borderRadius: '4px',
                      marginRight: '8px',
                      cursor: 'pointer'
                    }}
                    key={voucherCode}>
                      <span style={{
                        marginRight: '8px',
                        textAlign: 'center',
                        fontSize: '12pt',
                        fontWeight: 'bold'
                      }}>
                        {voucherCode}
                      </span>
                  </div>
                )
            )}
          </div>
        </div>
      </div>
    </div>);
}

export default VouchersSummary;