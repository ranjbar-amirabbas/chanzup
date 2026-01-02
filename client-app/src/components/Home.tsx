import React from 'react';
import { Link } from 'react-router-dom';

const Home: React.FC = () => {
  return (
    <div className="container">
      <h1>Welcome to Chanzup</h1>
      <p>Discover local businesses and win amazing prizes!</p>
      
      <div style={{ marginTop: '40px' }}>
        <Link to="/register" className="btn btn-primary" style={{ marginRight: '20px' }}>
          Get Started
        </Link>
        <Link to="/login" className="btn btn-primary">
          Sign In
        </Link>
      </div>
      
      <div style={{ marginTop: '60px', textAlign: 'left', maxWidth: '800px', margin: '60px auto 0' }}>
        <h2>How it works:</h2>
        <ol>
          <li>Visit participating local businesses</li>
          <li>Scan QR codes to earn tokens</li>
          <li>Spin the wheel of luck to win prizes</li>
          <li>Redeem your prizes at the business</li>
        </ol>
      </div>
    </div>
  );
};

export default Home;