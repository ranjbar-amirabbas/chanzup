import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { gameService, WheelSpinResponse, WheelSegment } from '../services/gameService';

const WheelSpin: React.FC = () => {
  const [spinning, setSpinning] = useState(false);
  const [spinResult, setSpinResult] = useState<WheelSpinResponse | null>(null);
  const [error, setError] = useState('');
  const [playerBalance, setPlayerBalance] = useState(0);
  const wheelRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const location = useLocation();
  
  const { campaignId, sessionId, campaign } = location.state || {};

  // Demo wheel segments for visualization
  const demoSegments: WheelSegment[] = [
    { id: '1', label: 'Free Coffee', color: '#FF6B6B', probability: 0.3, startAngle: 0, endAngle: 108 },
    { id: '2', label: 'No Prize', color: '#4ECDC4', probability: 0.2, startAngle: 108, endAngle: 180 },
    { id: '3', label: '10% Discount', color: '#45B7D1', probability: 0.25, startAngle: 180, endAngle: 270 },
    { id: '4', label: 'Free Pastry', color: '#96CEB4', probability: 0.15, startAngle: 270, endAngle: 324 },
    { id: '5', label: 'No Prize', color: '#FFEAA7', probability: 0.1, startAngle: 324, endAngle: 360 }
  ];

  useEffect(() => {
    if (!campaignId || !sessionId) {
      navigate('/dashboard');
      return;
    }
    loadPlayerBalance();
  }, [campaignId, sessionId, navigate]);

  const loadPlayerBalance = async () => {
    try {
      const balance = await gameService.getPlayerBalance();
      setPlayerBalance(balance.balance);
    } catch (err) {
      console.error('Failed to load player balance:', err);
    }
  };

  const handleSpin = async () => {
    if (spinning || !campaignId || !sessionId) return;

    try {
      setSpinning(true);
      setError('');

      const result = await gameService.spinWheel({
        campaignId,
        sessionId
      });

      // Animate wheel spin
      if (wheelRef.current) {
        const finalAngle = result.animation.finalAngle;
        const spinDuration = result.animation.duration;
        
        wheelRef.current.style.transition = `transform ${spinDuration}ms cubic-bezier(0.23, 1, 0.32, 1)`;
        wheelRef.current.style.transform = `rotate(${finalAngle + 1440}deg)`; // Add extra rotations for effect
      }

      // Show result after animation
      setTimeout(() => {
        setSpinResult(result);
        setPlayerBalance(result.newBalance);
        setSpinning(false);
      }, result.animation.duration);

    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to spin wheel');
      setSpinning(false);
    }
  };

  const handlePlayAgain = () => {
    setSpinResult(null);
    if (wheelRef.current) {
      wheelRef.current.style.transition = 'none';
      wheelRef.current.style.transform = 'rotate(0deg)';
    }
  };

  const handleGoToWallet = () => {
    navigate('/wallet');
  };

  if (!campaignId || !sessionId) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <div>Invalid session. Please scan a QR code first.</div>
          <button onClick={() => navigate('/dashboard')} className="btn btn-primary">
            Back to Dashboard
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ marginBottom: '20px' }}>
        <button 
          onClick={() => navigate(-1)} 
          style={{ 
            background: 'none', 
            border: 'none', 
            color: '#007bff', 
            cursor: 'pointer',
            fontSize: '16px'
          }}
        >
          ← Back
        </button>
        <h2 style={{ margin: '10px 0' }}>Wheel of Luck</h2>
        {campaign && <p>{campaign.name}</p>}
      </div>

      {error && (
        <div style={{ 
          color: '#721c24', 
          backgroundColor: '#f8d7da', 
          border: '1px solid #f5c6cb',
          borderRadius: '4px',
          padding: '10px', 
          marginBottom: '20px' 
        }}>
          {error}
        </div>
      )}

      {/* Player Info */}
      <div style={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center',
        backgroundColor: '#f8f9fa',
        padding: '15px',
        borderRadius: '8px',
        marginBottom: '30px'
      }}>
        <div>
          <strong>Token Balance:</strong> {playerBalance} tokens
        </div>
        <div>
          <strong>Cost per Spin:</strong> {campaign?.tokenCostPerSpin || 5} tokens
        </div>
      </div>

      {/* Wheel */}
      <div style={{ textAlign: 'center', marginBottom: '30px' }}>
        <div style={{ position: 'relative', display: 'inline-block' }}>
          {/* Wheel Container */}
          <div
            ref={wheelRef}
            style={{
              width: '300px',
              height: '300px',
              borderRadius: '50%',
              border: '8px solid #333',
              position: 'relative',
              background: `conic-gradient(
                ${demoSegments.map(segment => 
                  `${segment.color} ${segment.startAngle}deg ${segment.endAngle}deg`
                ).join(', ')}
              )`,
              boxShadow: '0 0 20px rgba(0,0,0,0.3)'
            }}
          >
            {/* Segment Labels */}
            {demoSegments.map((segment, index) => {
              const angle = (segment.startAngle + segment.endAngle) / 2;
              const radian = (angle * Math.PI) / 180;
              const x = Math.cos(radian) * 100;
              const y = Math.sin(radian) * 100;
              
              return (
                <div
                  key={segment.id}
                  style={{
                    position: 'absolute',
                    left: '50%',
                    top: '50%',
                    transform: `translate(-50%, -50%) translate(${x}px, ${y}px) rotate(${angle + 90}deg)`,
                    fontSize: '12px',
                    fontWeight: 'bold',
                    color: '#fff',
                    textShadow: '1px 1px 2px rgba(0,0,0,0.7)',
                    whiteSpace: 'nowrap',
                    pointerEvents: 'none'
                  }}
                >
                  {segment.label}
                </div>
              );
            })}
          </div>

          {/* Pointer */}
          <div style={{
            position: 'absolute',
            top: '-10px',
            left: '50%',
            transform: 'translateX(-50%)',
            width: '0',
            height: '0',
            borderLeft: '15px solid transparent',
            borderRight: '15px solid transparent',
            borderTop: '30px solid #333',
            zIndex: 10
          }} />
        </div>
      </div>

      {/* Spin Button */}
      {!spinResult && (
        <div style={{ textAlign: 'center', marginBottom: '30px' }}>
          <button
            onClick={handleSpin}
            disabled={spinning || playerBalance < (campaign?.tokenCostPerSpin || 5)}
            className="btn btn-primary"
            style={{ 
              fontSize: '18px', 
              padding: '15px 30px',
              opacity: spinning ? 0.6 : 1
            }}
          >
            {spinning ? '🎰 Spinning...' : '🎰 Spin the Wheel!'}
          </button>
          
          {playerBalance < (campaign?.tokenCostPerSpin || 5) && (
            <div style={{ marginTop: '10px', color: '#dc3545' }}>
              Not enough tokens to spin
            </div>
          )}
        </div>
      )}

      {/* Spin Result */}
      {spinResult && (
        <div style={{ 
          textAlign: 'center',
          backgroundColor: spinResult.result === 'prize' ? '#d4edda' : '#fff3cd',
          border: `1px solid ${spinResult.result === 'prize' ? '#c3e6cb' : '#ffeaa7'}`,
          borderRadius: '8px',
          padding: '30px',
          marginBottom: '20px'
        }}>
          <div style={{ fontSize: '48px', marginBottom: '20px' }}>
            {spinResult.result === 'prize' ? '🎉' : '😔'}
          </div>
          
          <h3 style={{ 
            margin: '0 0 20px 0',
            color: spinResult.result === 'prize' ? '#155724' : '#856404'
          }}>
            {spinResult.result === 'prize' ? 'Congratulations!' : 'Better luck next time!'}
          </h3>

          {spinResult.prize && (
            <div style={{ marginBottom: '20px' }}>
              <h4 style={{ margin: '0 0 10px 0' }}>You won: {spinResult.prize.name}</h4>
              <p style={{ margin: '0 0 10px 0' }}>{spinResult.prize.description}</p>
              <div style={{ 
                backgroundColor: '#fff',
                border: '2px dashed #007bff',
                borderRadius: '8px',
                padding: '15px',
                margin: '15px 0',
                fontFamily: 'monospace',
                fontSize: '18px',
                fontWeight: 'bold'
              }}>
                Redemption Code: {spinResult.prize.redemptionCode}
              </div>
              <p style={{ fontSize: '14px', color: '#666' }}>
                Expires: {new Date(spinResult.prize.expiresAt).toLocaleDateString()}
              </p>
            </div>
          )}

          <div style={{ marginBottom: '20px' }}>
            <p>Tokens spent: {spinResult.tokensSpent}</p>
            <p>New balance: {spinResult.newBalance} tokens</p>
          </div>

          <div style={{ display: 'flex', gap: '10px', justifyContent: 'center', flexWrap: 'wrap' }}>
            {spinResult.prize && (
              <button onClick={handleGoToWallet} className="btn btn-primary">
                View in Wallet
              </button>
            )}
            
            {spinResult.newBalance >= (campaign?.tokenCostPerSpin || 5) && (
              <button onClick={handlePlayAgain} className="btn btn-secondary">
                Spin Again
              </button>
            )}
            
            <button onClick={() => navigate('/dashboard')} className="btn btn-secondary">
              Back to Dashboard
            </button>
          </div>
        </div>
      )}

      {/* Game Rules */}
      <div style={{ 
        backgroundColor: '#f8f9fa', 
        borderRadius: '8px',
        padding: '20px',
        marginTop: '20px'
      }}>
        <h4 style={{ margin: '0 0 15px 0' }}>How to play:</h4>
        <ul style={{ margin: '0', paddingLeft: '20px' }}>
          <li>Each spin costs {campaign?.tokenCostPerSpin || 5} tokens</li>
          <li>Spin the wheel to win prizes or try again</li>
          <li>Won prizes will appear in your wallet with redemption codes</li>
          <li>Present redemption codes at the business to claim your prizes</li>
          <li>Prizes have expiration dates - use them before they expire!</li>
        </ul>
      </div>
    </div>
  );
};

export default WheelSpin;