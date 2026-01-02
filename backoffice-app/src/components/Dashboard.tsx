import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { apiClient } from '../utils/api';

interface DashboardMetrics {
  activeCampaigns: number;
  totalSpinsToday: number;
  prizesRedeemed: number;
  totalPlayers: number;
  tokenRevenue: number;
  redemptionRate: number;
}

interface BusinessInfo {
  name: string;
  subscriptionTier: number;
  subscriptionExpiry?: string;
}

const Dashboard: React.FC = () => {
  const { user } = useAuth();
  const [metrics, setMetrics] = useState<DashboardMetrics>({
    activeCampaigns: 0,
    totalSpinsToday: 0,
    prizesRedeemed: 0,
    totalPlayers: 0,
    tokenRevenue: 0,
    redemptionRate: 0
  });
  const [businessInfo, setBusinessInfo] = useState<BusinessInfo>({
    name: '',
    subscriptionTier: 0
  });
  const [loading, setLoading] = useState(true);
  const [recentActivity, setRecentActivity] = useState<any[]>([]);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      // Load business info and metrics from API
      const [businessResponse, metricsResponse] = await Promise.all([
        apiClient.get('/business/info'),
        apiClient.get('/business/dashboard/metrics')
      ]);

      if (businessResponse.data) {
        setBusinessInfo({
          name: businessResponse.data.name,
          subscriptionTier: businessResponse.data.subscriptionTier
        });
      }

      if (metricsResponse.data) {
        setMetrics(metricsResponse.data);
      }

      // Mock recent activity for now
      setRecentActivity([
        { id: 1, type: 'spin', description: 'Player won "Free Coffee" prize', time: '2 minutes ago' },
        { id: 2, type: 'redemption', description: 'Prize redeemed at location', time: '15 minutes ago' },
        { id: 3, type: 'campaign', description: 'Holiday Campaign activated', time: '1 hour ago' }
      ]);

    } catch (error) {
      console.error('Failed to load dashboard data:', error);
      // Fallback to user data if API fails
      if (user) {
        setBusinessInfo({
          name: user.firstName && user.lastName ? `${user.firstName} ${user.lastName}` : user.email,
          subscriptionTier: 1 // Default to Premium for demo
        });
      }
    } finally {
      setLoading(false);
    }
  };

  const getSubscriptionBadge = (tier: number) => {
    const badges = {
      0: { name: 'Basic', class: 'basic' },
      1: { name: 'Premium', class: 'premium' },
      2: { name: 'Enterprise', class: 'enterprise' }
    };
    const badge = badges[tier as keyof typeof badges] || badges[0];
    return <span className={`subscription-badge ${badge.class}`}>{badge.name}</span>;
  };

  const handleLogout = () => {
    // Logout is now handled by the Navigation component
    // This function can be removed or used for additional cleanup
  };

  if (loading) {
    return (
      <div className="main-content">
        <div className="card">
          <p>Loading dashboard...</p>
        </div>
      </div>
    );
  }
  return (
    <div>
      <div className="sidebar">
        <ul>
          <li><Link to="/dashboard" className="active">Dashboard</Link></li>
          <li><Link to="/campaigns">Campaigns</Link></li>
          <li><Link to="/analytics">Analytics</Link></li>
          <li><Link to="/prizes">Prizes</Link></li>
          <li><Link to="/redemption">Redemption</Link></li>
          <li><Link to="/settings">Settings</Link></li>
        </ul>
      </div>
      
      <div className="main-content">
        <div className="dashboard-header">
          <div>
            <h2>Welcome back, {businessInfo.name}!</h2>
            <p className="welcome-message">
              {getSubscriptionBadge(businessInfo.subscriptionTier)} Plan • Here's your business overview
            </p>
          </div>
          <div>
            <Link to="/campaigns/new" className="btn btn-success">Create Campaign</Link>
          </div>
        </div>
        
        <div className="grid grid-3">
          <div className="card metric-card">
            <h3>Active Campaigns</h3>
            <div className="metric-value primary">{metrics.activeCampaigns}</div>
            <p className="metric-description">Currently running campaigns</p>
            <Link to="/campaigns" className="btn btn-primary">Manage Campaigns</Link>
          </div>
          
          <div className="card metric-card">
            <h3>Spins Today</h3>
            <div className="metric-value success">{metrics.totalSpinsToday}</div>
            <p className="metric-description">Player engagement today</p>
          </div>
          
          <div className="card metric-card">
            <h3>Prizes Redeemed</h3>
            <div className="metric-value warning">{metrics.prizesRedeemed}</div>
            <p className="metric-description">Successful redemptions</p>
          </div>
        </div>
        
        <div className="grid grid-3" style={{ marginTop: '20px' }}>
          <div className="card metric-card">
            <h3>Total Players</h3>
            <div className="metric-value info">{metrics.totalPlayers}</div>
            <p className="metric-description">Unique players engaged</p>
          </div>
          
          <div className="card metric-card">
            <h3>Token Revenue</h3>
            <div className="metric-value primary">${metrics.tokenRevenue}</div>
            <p className="metric-description">Revenue from token purchases</p>
          </div>
          
          <div className="card metric-card">
            <h3>Redemption Rate</h3>
            <div className="metric-value success">{(metrics.redemptionRate * 100).toFixed(1)}%</div>
            <p className="metric-description">Prize redemption success rate</p>
          </div>
        </div>
        
        <div className="grid grid-2" style={{ marginTop: '40px' }}>
          <div className="card">
            <h3>Quick Actions</h3>
            <div className="quick-actions">
              <Link to="/campaigns/new" className="btn btn-success">Create New Campaign</Link>
              <Link to="/prizes/new" className="btn btn-primary">Add Prize Inventory</Link>
              <Link to="/qr-codes" className="btn btn-primary">Generate QR Codes</Link>
              <Link to="/analytics" className="btn btn-primary">View Analytics</Link>
              {businessInfo.subscriptionTier === 0 && (
                <Link to="/subscription/upgrade" className="btn btn-warning">Upgrade Plan</Link>
              )}
            </div>
          </div>
          
          <div className="card">
            <h3>Recent Activity</h3>
            {recentActivity.length > 0 ? (
              <div>
                {recentActivity.map((activity) => (
                  <div key={activity.id} className="activity-item">
                    <p style={{ margin: '0 0 5px 0' }}>{activity.description}</p>
                    <p className="activity-time">{activity.time}</p>
                  </div>
                ))}
              </div>
            ) : (
              <div>
                <p>No recent activity to display</p>
                <p className="metric-description">
                  Activity will appear here once you start creating campaigns and players begin engaging.
                </p>
              </div>
            )}
          </div>
        </div>
        
        {businessInfo.subscriptionTier === 0 && (
          <div className="card" style={{ marginTop: '40px', background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', color: 'white' }}>
            <h3 style={{ color: 'white' }}>Unlock Premium Features</h3>
            <p>Upgrade to Premium or Enterprise to access advanced analytics, unlimited campaigns, and multi-location support.</p>
            <Link to="/subscription/upgrade" className="btn" style={{ backgroundColor: 'white', color: '#667eea' }}>
              View Plans
            </Link>
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;