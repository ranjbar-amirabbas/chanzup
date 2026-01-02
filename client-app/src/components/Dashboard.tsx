import React from 'react';
import { useNavigate } from 'react-router-dom';

const Dashboard: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="container">
      <h2>Player Dashboard</h2>
      <p>Welcome to your dashboard! Here you can:</p>
      
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '20px', marginTop: '40px' }}>
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px' }}>
          <h3>Token Balance</h3>
          <p style={{ fontSize: '24px', fontWeight: 'bold', color: '#007bff' }}>0 Tokens</p>
          <p>Earn tokens by visiting businesses and scanning QR codes</p>
        </div>
        
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px' }}>
          <h3>Nearby Businesses</h3>
          <p>Discover participating businesses near you</p>
          <button 
            className="btn btn-primary"
            onClick={() => navigate('/nearby')}
          >
            Find Businesses
          </button>
        </div>
        
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px' }}>
          <h3>My Prizes</h3>
          <p>View and redeem your won prizes</p>
          <button 
            className="btn btn-primary"
            onClick={() => navigate('/wallet')}
          >
            View Prizes
          </button>
        </div>
        
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px' }}>
          <h3>QR Scanner</h3>
          <p>Scan QR codes at businesses to earn tokens</p>
          <button 
            className="btn btn-primary"
            onClick={() => navigate('/qr-scanner')}
          >
            Open Scanner
          </button>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;