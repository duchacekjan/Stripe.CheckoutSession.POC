"use client";
import {useEffect, useState} from 'react';
import {getCurrentBasketId, setCurrentBasketId} from "@/utils/basketIdProvider";
import {buyVoucher} from "@/utils/api";
import {useRouter} from "next/navigation";

const Vouchers: React.FC = () => {
  const router = useRouter();
  const [voucherValue, setVoucherValue] = useState<number>(160);
  const [basketId, setBasketId] = useState<string | null>(null);

  // Load basketId from localStorage on component mount
  useEffect(() => {
    const storedBasketId = getCurrentBasketId();
    setBasketId(storedBasketId);
  }, []);

  const handleSliderChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setVoucherValue(Number(event.target.value));
  };

  const handleAddToCart = async () => {
    try {
      const response = await buyVoucher(voucherValue, basketId ?? undefined);
      setCurrentBasketId(response.basketId);
      router.push('/checkout-session');
    } catch (error) {
      console.error("Error adding voucher to cart:", error);
    }
  };
  
  const handleSeatPlan = () => {
    router.push('/');
  };
  const handleBasket = () => {
    if (basketId) {
      router.push('/basket');
    } else {
      console.warn("No basket ID found. Redirecting to seat plan.");
      router.push('/');
    }
  };

  return (
    <div style={{display: 'flex', flexDirection: 'column', gap: '8px'}}>
      <div style={{padding: '20px'}}>
        <label htmlFor="voucher-slider" style={{
          display: 'block',
          marginBottom: '10px',
          fontWeight: 'bold'
        }}>
          Voucher Value: £{voucherValue}
        </label>

        <input
          id="voucher-slider"
          type="range"
          min="160"
          max="500"
          step="5"
          value={voucherValue}
          onChange={handleSliderChange}
          style={{
            width: '100%',
            height: '8px',
            borderRadius: '4px',
            background: '#ddd',
            outline: 'none',
            cursor: 'pointer'
          }}
        />

        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          fontSize: '12px',
          color: '#666',
          marginTop: '5px'
        }}>
          <span>£160</span>
          <span>£500</span>
        </div>
      </div>
      <div style={{padding: '20px', display: 'flex', gap: '8px'}}>
        <button onClick={handleAddToCart}>Add to cart</button>
        <button onClick={handleSeatPlan}>Seat plan</button>
        <button onClick={handleBasket} disabled={!basketId}>Basket</button>
      </div>
    </div>
  );
};

export default Vouchers;