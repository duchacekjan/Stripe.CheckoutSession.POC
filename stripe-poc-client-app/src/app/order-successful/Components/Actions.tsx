'use client';

interface ActionsProps {
  redirectTo: (target: string) => void;
}

const Actions: React.FC<ActionsProps> = ({redirectTo}) => {
  return (
    <div style={{display: 'flex', gap: '8px'}}>
      <button onClick={() => redirectTo('/')} style={{marginTop: '8px'}}>
        Continue Shopping
      </button>
      <button onClick={() => redirectTo('/refund')} style={{marginTop: '8px', backgroundColor: 'red'}}>
        Refund
      </button>
    </div>
  );
}

export default Actions;