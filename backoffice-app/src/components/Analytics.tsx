import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, BarChart, Bar, PieChart, Pie, Cell } from 'recharts';

interface AnalyticsData {
  dailySpins: Array<{ date: string; spins: number; players: number; revenue: number }>;
  campaignPerformance: Array<{ name: string; spins: number; redemptions: number; revenue: number }>;
  playerDemographics: Array<{ ageGroup: string; count: number; percentage: number }>;
  prizeDistribution: Array<{ name: string; value: number; color: string }>;
  topPrizes: Array<{ name: string; timesWon: number; redemptionRate: number }>;
  realtimeMetrics: {
    activeUsers: number;
    spinsLastHour: number;
    redemptionsToday: number;
    conversionRate: number;
  };
}

const Analytics: React.FC = () => {
  const [analyticsData, setAnalyticsData] = useState<AnalyticsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedPeriod, setSelectedPeriod] = useState('7d');
  const [subscriptionTier, setSubscriptionTier] = useState(0);

  useEffect(() => {
    const tier = parseInt(localStorage.getItem('subscriptionTier') || '0');
    setSubscriptionTier(tier);
    loadAnalyticsData();
  }, [selectedPeriod]);

  const loadAnalyticsData = async () => {
    try {
      // TODO: Replace with actual API call
      // Mock data for demonstration
      setAnalyticsData({
        dailySpins: [
          { date: '2024-12-09', spins: 45, players: 23, revenue: 225 },
          { date: '2024-12-10', spins: 52, players: 28, revenue: 260 },
          { date: '2024-12-11', spins: 38, players: 19, revenue: 190 },
          { date: '2024-12-12', spins: 67, players: 34, revenue: 335 },
          { date: '2024-12-13', spins: 71, players: 38, revenue: 355 },
          { date: '2024-12-14', spins: 59, players: 31, revenue: 295 },
          { date: '2024-12-15', spins: 83, players: 42, revenue: 415 }
        ],
        campaignPerformance: [
          { name: 'Holiday Wheel', spins: 245, redemptions: 178, revenue: 1225 },
          { name: 'New Year Hunt', spins: 89, redemptions: 67, revenue: 267 },
          { name: 'Winter Special', spins: 156, redemptions: 134, revenue: 468 }
        ],
        playerDemographics: [
          { ageGroup: '18-25', count: 45, percentage: 32 },
          { ageGroup: '26-35', count: 52, percentage: 37 },
          { ageGroup: '36-45', count: 28, percentage: 20 },
          { ageGroup: '46+', count: 15, percentage: 11 }
        ],
        prizeDistribution: [
          { name: 'Free Coffee', value: 35, color: '#8884d8' },
          { name: '10% Discount', value: 40, color: '#82ca9d' },
          { name: 'Free Pastry', value: 15, color: '#ffc658' },
          { name: 'No Prize', value: 10, color: '#ff7c7c' }
        ],
        topPrizes: [
          { name: 'Free Coffee', timesWon: 89, redemptionRate: 0.85 },
          { name: '10% Discount', timesWon: 67, redemptionRate: 0.92 },
          { name: 'Free Pastry', timesWon: 34, redemptionRate: 0.78 },
          { name: 'Buy One Get One', timesWon: 23, redemptionRate: 0.91 }
        ],
        realtimeMetrics: {
          activeUsers: 12,
          spinsLastHour: 8,
          redemptionsToday: 23,
          conversionRate: 0.73
        }
      });
    } catch (error) {
      console.error('Failed to load analytics data:', error);
    } finally {
      setLoading(false);
    }
  };

  const exportReport = (format: 'csv' | 'pdf') => {
    // TODO: Implement actual export functionality
    alert(`Exporting report as ${format.toUpperCase()}...`);
  };

  const isPremiumFeature = (feature: string) => {
    return subscriptionTier === 0; // Basic plan users see premium features as locked
  };

  if (loading) {
    return (
      <div className="main-content">
        <div className="card">
          <p>Loading analytics...</p>
        </div>
      </div>
    );
  }

  if (!analyticsData) {
    return (
      <div className="main-content">
        <div className="card">
          <p>Failed to load analytics data</p>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="sidebar">
        <ul>
          <li><Link to="/dashboard">Dashboard</Link></li>
          <li><Link to="/campaigns">Campaigns</Link></li>
          <li><Link to="/analytics" className="active">Analytics</Link></li>
          <li><Link to="/prizes">Prizes</Link></li>
          <li><Link to="/redemption">Redemption</Link></li>
          <li><Link to="/settings">Settings</Link></li>
        </ul>
      </div>
      
      <div className="main-content">
        <div className="dashboard-header">
          <div>
            <h2>Analytics & Reporting</h2>
            <p className="welcome-message">Comprehensive insights into your campaign performance</p>
          </div>
          <div className="analytics-controls">
            <select 
              value={selectedPeriod} 
              onChange={(e) => setSelectedPeriod(e.target.value)}
              className="period-selector"
            >
              <option value="24h">Last 24 Hours</option>
              <option value="7d">Last 7 Days</option>
              <option value="30d">Last 30 Days</option>
              <option value="90d">Last 90 Days</option>
            </select>
            <button onClick={() => exportReport('csv')} className="btn btn-primary">
              Export CSV
            </button>
            <button onClick={() => exportReport('pdf')} className="btn btn-secondary">
              Export PDF
            </button>
          </div>
        </div>

        {/* Real-time Metrics */}
        <div className="card">
          <h3>Real-time Metrics</h3>
          <div className="grid grid-4">
            <div className="realtime-metric">
              <div className="metric-value success">{analyticsData.realtimeMetrics.activeUsers}</div>
              <div className="metric-label">Active Users</div>
              <div className="metric-change">+2 from last hour</div>
            </div>
            <div className="realtime-metric">
              <div className="metric-value primary">{analyticsData.realtimeMetrics.spinsLastHour}</div>
              <div className="metric-label">Spins (Last Hour)</div>
              <div className="metric-change">+15% from avg</div>
            </div>
            <div className="realtime-metric">
              <div className="metric-value warning">{analyticsData.realtimeMetrics.redemptionsToday}</div>
              <div className="metric-label">Redemptions Today</div>
              <div className="metric-change">+8% from yesterday</div>
            </div>
            <div className="realtime-metric">
              <div className="metric-value info">{(analyticsData.realtimeMetrics.conversionRate * 100).toFixed(1)}%</div>
              <div className="metric-label">Conversion Rate</div>
              <div className="metric-change">+3.2% from last week</div>
            </div>
          </div>
        </div>

        {/* Daily Performance Chart */}
        <div className="card">
          <h3>Daily Performance Trends</h3>
          <div style={{ width: '100%', height: 300 }}>
            <ResponsiveContainer>
              <LineChart data={analyticsData.dailySpins}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="spins" stroke="#8884d8" name="Spins" />
                <Line type="monotone" dataKey="players" stroke="#82ca9d" name="Players" />
                <Line type="monotone" dataKey="revenue" stroke="#ffc658" name="Revenue ($)" />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="grid grid-2">
          {/* Campaign Performance */}
          <div className="card">
            <h3>Campaign Performance</h3>
            <div style={{ width: '100%', height: 250 }}>
              <ResponsiveContainer>
                <BarChart data={analyticsData.campaignPerformance}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" />
                  <YAxis />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="spins" fill="#8884d8" name="Spins" />
                  <Bar dataKey="redemptions" fill="#82ca9d" name="Redemptions" />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Prize Distribution */}
          <div className="card">
            <h3>Prize Distribution</h3>
            <div style={{ width: '100%', height: 250 }}>
              <ResponsiveContainer>
                <PieChart>
                  <Pie
                    data={analyticsData.prizeDistribution}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ name, percentage }) => `${name} ${percentage}%`}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="value"
                  >
                    {analyticsData.prizeDistribution.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </div>
        </div>

        {/* Premium Analytics Features */}
        {subscriptionTier > 0 ? (
          <div className="premium-analytics">
            <div className="grid grid-2">
              {/* Player Demographics */}
              <div className="card">
                <h3>Player Demographics</h3>
                <div className="demographics-list">
                  {analyticsData.playerDemographics.map((demo, index) => (
                    <div key={index} className="demographic-item">
                      <div className="demo-info">
                        <span className="demo-label">{demo.ageGroup}</span>
                        <span className="demo-count">{demo.count} players</span>
                      </div>
                      <div className="demo-bar">
                        <div 
                          className="demo-fill" 
                          style={{ width: `${demo.percentage}%` }}
                        ></div>
                      </div>
                      <span className="demo-percentage">{demo.percentage}%</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Top Performing Prizes */}
              <div className="card">
                <h3>Top Performing Prizes</h3>
                <div className="prizes-performance">
                  {analyticsData.topPrizes.map((prize, index) => (
                    <div key={index} className="prize-performance-item">
                      <div className="prize-info">
                        <h5>{prize.name}</h5>
                        <div className="prize-stats">
                          <span>Won {prize.timesWon} times</span>
                          <span>Redemption: {(prize.redemptionRate * 100).toFixed(1)}%</span>
                        </div>
                      </div>
                      <div className="performance-indicator">
                        <div 
                          className="performance-bar"
                          style={{ 
                            width: `${prize.redemptionRate * 100}%`,
                            backgroundColor: prize.redemptionRate > 0.8 ? '#28a745' : '#ffc107'
                          }}
                        ></div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* Advanced Insights */}
            <div className="card">
              <h3>Advanced Insights</h3>
              <div className="insights-grid">
                <div className="insight-card">
                  <h4>Peak Hours</h4>
                  <p>Most activity between 2-4 PM and 7-9 PM</p>
                  <small>Based on last 30 days of data</small>
                </div>
                <div className="insight-card">
                  <h4>Player Retention</h4>
                  <p>68% of players return within 7 days</p>
                  <small>Above industry average of 45%</small>
                </div>
                <div className="insight-card">
                  <h4>Revenue Optimization</h4>
                  <p>Increase token cost to 6 for 12% revenue boost</p>
                  <small>AI-powered recommendation</small>
                </div>
                <div className="insight-card">
                  <h4>Prize Strategy</h4>
                  <p>Free Coffee drives highest engagement</p>
                  <small>Consider increasing allocation</small>
                </div>
              </div>
            </div>
          </div>
        ) : (
          <div className="premium-upsell card">
            <div className="upsell-content">
              <h3>🚀 Unlock Premium Analytics</h3>
              <p>Get deeper insights with advanced analytics features:</p>
              <ul>
                <li>Player demographics and behavior analysis</li>
                <li>Prize performance optimization</li>
                <li>AI-powered recommendations</li>
                <li>Advanced reporting and exports</li>
                <li>Real-time alerts and notifications</li>
              </ul>
              <Link to="/subscription/upgrade" className="btn btn-success">
                Upgrade to Premium
              </Link>
            </div>
            <div className="upsell-preview">
              <div className="blurred-chart">
                <div className="blur-overlay">
                  <div className="lock-icon">🔒</div>
                  <p>Premium Feature</p>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Analytics;