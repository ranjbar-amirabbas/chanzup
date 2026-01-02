import React from 'react';
import { Link } from 'react-router-dom';

const Home: React.FC = () => {
  return (
    <div className="container">
      <div className="card" style={{ textAlign: 'center', maxWidth: '600px', margin: '0 auto' }}>
        <h1>Welcome to Chanzup Business Portal</h1>
        <p>Create engaging campaigns and connect with customers through gamified experiences.</p>
        
        <div style={{ marginTop: '40px' }}>
          <Link to="/register" className="btn btn-success" style={{ marginRight: '20px' }}>
            Register Your Business
          </Link>
          <Link to="/login" className="btn btn-primary">
            Business Login
          </Link>
        </div>
      </div>
      
      <div className="grid grid-3" style={{ marginTop: '60px' }}>
        <div className="card">
          <h3>🎯 Create Campaigns</h3>
          <p>Design engaging Wheel of Luck campaigns with custom prizes and rewards.</p>
        </div>
        
        <div className="card">
          <h3>📊 Track Performance</h3>
          <p>Monitor campaign performance with real-time analytics and insights.</p>
        </div>
        
        <div className="card">
          <h3>🏆 Manage Prizes</h3>
          <p>Set up prize inventories and track redemptions across all campaigns.</p>
        </div>
      </div>
    </div>
  );
};

export default Home;