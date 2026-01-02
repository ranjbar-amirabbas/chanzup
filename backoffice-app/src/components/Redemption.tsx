import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';

interface RedemptionRecord {
  id: string;
  redemptionCode: string;
  prizeName: string;
  prizeValue: number;
  playerName: string;
  playerEmail: string;
  campaignName: string;
  status: 'pending' | 'verified' | 'completed' | 'expired';
  createdAt: string;
  redeemedAt?: string;
  staffMember?: string;
  expiresAt: string;
}

interface PrizeVerification {
  redemptionCode: string;
  valid: boolean;
  prize?: {
    name: string;
    description: string;
    value: number;
    playerName: string;
    businessName: string;
    expiresAt: string;
  };
  error?: string;
}

const Redemption: React.FC = () => {
  const [redemptionCode, setRedemptionCode] = useState('');
  const [verificationResult, setVerificationResult] = useState<PrizeVerification | null>(null);
  const [loading, setLoading] = useState(false);
  const [redemptionHistory, setRedemptionHistory] = useState<RedemptionRecord[]>([]);
  const [activeTab, setActiveTab] = useState('verify');
  const [filterStatus, setFilterStatus] = useState('all');

  useEffect(() => {
    loadRedemptionHistory();
  }, []);

  const loadRedemptionHistory = async () => {
    try {
      // TODO: Replace with actual API call
      // Mock data for demonstration
      setRedemptionHistory([
        {
          id: '1',
          redemptionCode: 'COFFEE-ABC123',
          prizeName: 'Free Coffee',
          prizeValue: 5.00,
          playerName: 'John Doe',
          playerEmail: 'john@example.com',
          campaignName: 'Holiday Wheel',
          status: 'completed',
          createdAt: '2024-12-15T10:30:00Z',
          redeemedAt: '2024-12-15T14:45:00Z',
          staffMember: 'Sarah Johnson',
          expiresAt: '2024-12-22T23:59:59Z'
        },
        {
          id: '2',
          redemptionCode: 'DISCOUNT-XYZ789',
          prizeName: '10% Discount',
          prizeValue: 0,
          playerName: 'Jane Smith',
          playerEmail: 'jane@example.com',
          campaignName: 'Holiday Wheel',
          status: 'pending',
          createdAt: '2024-12-15T12:15:00Z',
          expiresAt: '2024-12-22T23:59:59Z'
        },
        {
          id: '3',
          redemptionCode: 'PASTRY-DEF456',
          prizeName: 'Free Pastry',
          prizeValue: 3.50,
          playerName: 'Mike Wilson',
          playerEmail: 'mike@example.com',
          campaignName: 'Winter Special',
          status: 'verified',
          createdAt: '2024-12-15T09:20:00Z',
          expiresAt: '2024-12-20T23:59:59Z'
        }
      ]);
    } catch (error) {
      console.error('Failed to load redemption history:', error);
    }
  };

  const verifyRedemptionCode = async () => {
    if (!redemptionCode.trim()) {
      setVerificationResult({
        redemptionCode,
        valid: false,
        error: 'Please enter a redemption code'
      });
      return;
    }

    setLoading(true);
    try {
      // TODO: Replace with actual API call
      // Mock verification logic
      const mockPrize = {
        name: 'Free Coffee',
        description: 'One free regular coffee',
        value: 5.00,
        playerName: 'John D.',
        businessName: 'Coffee Shop',
        expiresAt: '2024-12-22T23:59:59Z'
      };

      setVerificationResult({
        redemptionCode,
        valid: true,
        prize: mockPrize
      });
    } catch (error) {
      setVerificationResult({
        redemptionCode,
        valid: false,
        error: 'Failed to verify redemption code'
      });
    } finally {
      setLoading(false);
    }
  };

  const completeRedemption = async () => {
    if (!verificationResult?.valid || !verificationResult.prize) return;

    setLoading(true);
    try {
      // TODO: Replace with actual API call
      const staffMember = 'Current Staff Member'; // Get from auth context
      
      // Update the redemption record
      setRedemptionHistory(prev => prev.map(record => 
        record.redemptionCode === redemptionCode 
          ? { ...record, status: 'completed', redeemedAt: new Date().toISOString(), staffMember }
          : record
      ));

      alert('Prize redemption completed successfully!');
      setRedemptionCode('');
      setVerificationResult(null);
    } catch (error) {
      alert('Failed to complete redemption');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status: string) => {
    const statusConfig = {
      pending: { class: 'warning', text: 'Pending' },
      verified: { class: 'info', text: 'Verified' },
      completed: { class: 'success', text: 'Completed' },
      expired: { class: 'danger', text: 'Expired' }
    };
    const config = statusConfig[status as keyof typeof statusConfig] || statusConfig.pending;
    return <span className={`status-badge ${config.class}`}>{config.text}</span>;
  };

  const filteredHistory = redemptionHistory.filter(record => 
    filterStatus === 'all' || record.status === filterStatus
  );

  const renderVerificationTab = () => (
    <div className="verification-section">
      <div className="card">
        <h3>Verify Redemption Code</h3>
        <p>Enter the redemption code provided by the customer to verify and complete the prize redemption.</p>
        
        <div className="verification-form">
          <div className="form-group">
            <label htmlFor="redemptionCode">Redemption Code</label>
            <div className="code-input-group">
              <input
                type="text"
                id="redemptionCode"
                value={redemptionCode}
                onChange={(e) => setRedemptionCode(e.target.value.toUpperCase())}
                placeholder="Enter redemption code (e.g., COFFEE-ABC123)"
                style={{ textTransform: 'uppercase' }}
              />
              <button 
                onClick={verifyRedemptionCode}
                className="btn btn-primary"
                disabled={loading}
              >
                {loading ? 'Verifying...' : 'Verify'}
              </button>
            </div>
          </div>
        </div>

        {verificationResult && (
          <div className={`verification-result ${verificationResult.valid ? 'valid' : 'invalid'}`}>
            {verificationResult.valid && verificationResult.prize ? (
              <div className="valid-prize">
                <div className="prize-header">
                  <h4>✅ Valid Prize</h4>
                  <span className="prize-value">${verificationResult.prize.value.toFixed(2)}</span>
                </div>
                
                <div className="prize-details">
                  <div className="detail-row">
                    <span className="label">Prize:</span>
                    <span className="value">{verificationResult.prize.name}</span>
                  </div>
                  <div className="detail-row">
                    <span className="label">Description:</span>
                    <span className="value">{verificationResult.prize.description}</span>
                  </div>
                  <div className="detail-row">
                    <span className="label">Customer:</span>
                    <span className="value">{verificationResult.prize.playerName}</span>
                  </div>
                  <div className="detail-row">
                    <span className="label">Expires:</span>
                    <span className="value">
                      {new Date(verificationResult.prize.expiresAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>

                <div className="redemption-actions">
                  <button 
                    onClick={completeRedemption}
                    className="btn btn-success"
                    disabled={loading}
                  >
                    {loading ? 'Processing...' : 'Complete Redemption'}
                  </button>
                  <button 
                    onClick={() => {
                      setRedemptionCode('');
                      setVerificationResult(null);
                    }}
                    className="btn btn-secondary"
                  >
                    Clear
                  </button>
                </div>
              </div>
            ) : (
              <div className="invalid-prize">
                <h4>❌ Invalid Code</h4>
                <p>{verificationResult.error || 'This redemption code is not valid or has already been used.'}</p>
              </div>
            )}
          </div>
        )}
      </div>

      <div className="card">
        <h3>Quick Tips</h3>
        <ul className="tips-list">
          <li>Redemption codes are case-insensitive and will be automatically converted to uppercase</li>
          <li>Each code can only be used once</li>
          <li>Check the expiration date before completing redemption</li>
          <li>If a code appears invalid, ask the customer to check their prize wallet</li>
          <li>Contact support if you encounter technical issues</li>
        </ul>
      </div>
    </div>
  );

  const renderHistoryTab = () => (
    <div className="history-section">
      <div className="history-header">
        <h3>Redemption History</h3>
        <div className="history-filters">
          <select 
            value={filterStatus} 
            onChange={(e) => setFilterStatus(e.target.value)}
            className="status-filter"
          >
            <option value="all">All Status</option>
            <option value="pending">Pending</option>
            <option value="verified">Verified</option>
            <option value="completed">Completed</option>
            <option value="expired">Expired</option>
          </select>
        </div>
      </div>

      <div className="redemption-table">
        {filteredHistory.length > 0 ? (
          <table>
            <thead>
              <tr>
                <th>Code</th>
                <th>Prize</th>
                <th>Customer</th>
                <th>Campaign</th>
                <th>Status</th>
                <th>Created</th>
                <th>Redeemed</th>
                <th>Staff</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredHistory.map((record) => (
                <tr key={record.id}>
                  <td className="code-cell">{record.redemptionCode}</td>
                  <td>
                    <div className="prize-cell">
                      <span className="prize-name">{record.prizeName}</span>
                      {record.prizeValue > 0 && (
                        <span className="prize-value">${record.prizeValue.toFixed(2)}</span>
                      )}
                    </div>
                  </td>
                  <td>
                    <div className="customer-cell">
                      <span className="customer-name">{record.playerName}</span>
                      <span className="customer-email">{record.playerEmail}</span>
                    </div>
                  </td>
                  <td>{record.campaignName}</td>
                  <td>{getStatusBadge(record.status)}</td>
                  <td>{new Date(record.createdAt).toLocaleDateString()}</td>
                  <td>
                    {record.redeemedAt 
                      ? new Date(record.redeemedAt).toLocaleDateString()
                      : '-'
                    }
                  </td>
                  <td>{record.staffMember || '-'}</td>
                  <td>
                    {record.status === 'pending' && (
                      <button 
                        onClick={() => {
                          setRedemptionCode(record.redemptionCode);
                          setActiveTab('verify');
                        }}
                        className="btn btn-primary"
                        style={{ padding: '5px 10px', fontSize: '12px' }}
                      >
                        Verify
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="empty-state">
            <p>No redemption records found for the selected filter.</p>
          </div>
        )}
      </div>
    </div>
  );

  return (
    <div>
      <div className="sidebar">
        <ul>
          <li><Link to="/dashboard">Dashboard</Link></li>
          <li><Link to="/campaigns">Campaigns</Link></li>
          <li><Link to="/analytics">Analytics</Link></li>
          <li><Link to="/prizes">Prizes</Link></li>
          <li><Link to="/redemption" className="active">Redemption</Link></li>
          <li><Link to="/settings">Settings</Link></li>
        </ul>
      </div>
      
      <div className="main-content">
        <div className="dashboard-header">
          <h2>Prize Redemption</h2>
          <p className="welcome-message">Verify and manage customer prize redemptions</p>
        </div>

        <div className="redemption-tabs">
          <button 
            className={`tab-button ${activeTab === 'verify' ? 'active' : ''}`}
            onClick={() => setActiveTab('verify')}
          >
            Verify Code
          </button>
          <button 
            className={`tab-button ${activeTab === 'history' ? 'active' : ''}`}
            onClick={() => setActiveTab('history')}
          >
            Redemption History
          </button>
        </div>

        <div className="tab-content">
          {activeTab === 'verify' && renderVerificationTab()}
          {activeTab === 'history' && renderHistoryTab()}
        </div>
      </div>
    </div>
  );
};

export default Redemption;