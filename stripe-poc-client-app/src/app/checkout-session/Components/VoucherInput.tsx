'use client';

import {useEffect, useState} from "react";
import {useApi} from "@/utils/api";
import {getCurrentBasketId} from "@/utils/basketIdProvider";
import axios from "axios";

interface VoucherInputProps {
  hasVoucher: boolean;
  voucherApplied: () => Promise<void>;
}

const VoucherInput: React.FC<VoucherInputProps> = ({
                                                     hasVoucher,
                                                     voucherApplied
                                                   }) => {
  const api = useApi();
  const [error, setError] = useState<string | null>(null);
  const [basketId, setBasketId] = useState<string | null>(null);
  const [code, setCode] = useState<string>('');
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    const currentBasketId = getCurrentBasketId();
    if (currentBasketId) {
      setBasketId(currentBasketId);
    } else {
      console.warn("No basket ID found. Voucher input will not be displayed.");
    }
  }, []);

  const handleChange = (e: any) => {
    setError(null);
    setSuccess(null);
    setCode(e.target.value);
  };

  const handleApply = async () => {
    setError(null);
    setSuccess(null);

    if (!code) {
      return;
    }

    try {
      const response = await api.vouchers.validate(basketId!, code);
      if (!response.isValid) {
        setError(response.message || "Invalid voucher code");
      } else {
        await api.vouchers.redeem(basketId!, code);
        await voucherApplied();
        setSuccess("Voucher applied successfully!");
        setCode('');
      }
    } catch (error) {
      if (axios.isAxiosError(error)) {
        setError(`Failed to apply voucher: ${error.response?.data?.message || error.message}`);
      } else {
        setError(`Failed to apply voucher: ${error}`);
      }
    }
  }

  if (hasVoucher || !basketId) {
    return null;
  }

  return (
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
      <h4>Promo code</h4>
      <div style={{display: 'flex', flexDirection: 'column', gap: '4px'}}>
        <div style={{display: 'flex', gap: '4px'}}>
          <input
            id="code"
            type="text"
            value={code}
            onChange={handleChange}
            height="100%"
            style={{fontSize: '12pt', flexGrow: 1, borderRadius: '4px', border: '1px solid #e5e7eb', padding: '4px'}}
            placeholder="Enter your promo code"
          />
          <button type={"button"}
                  onClick={handleApply}>
            Apply
          </button>
        </div>
        {error && <div style={{color: 'red', fontSize: '11pt'}}>{error}</div>}
        {success && <div style={{color: 'green', fontSize: '11pt'}}>{success}</div>}
      </div>
    </div>);
}

export default VoucherInput;