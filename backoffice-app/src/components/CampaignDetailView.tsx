import React, { useState } from 'react';

interface Campaign {
  id: string;
  name: string;
  description: string;
  gameType: 'WheelOfLuck' | 'TreasureHunt';
  tokenCostPerSpin: number;
  maxSpinsPerDay: number;
  isActive: boolean;
  startDate: string;
  endDate?: string;
  totalSpins: number;
  totalPrizes: number;
  qrCodeUrl?: string;
}

interface Props {
  campaign: Campaign;
  onClose: () => void;
  onUpdate: () => void;
}

const CampaignDetailView: React.FC<Props> = ({ campaign, onClose, onUpdate }) => {
  const [activeTab, setActiveTab] = useState('overview');
  const [qrCodeGenerated, setQrCodeGenerated] = useState(false);

  const generateQRCode = async () => {
    try {
      // TODO: Implement actual QR code generation API call
      setQrCodeGenerated(true);
      alert('QR Code generated successfully!');
    } catch (error) {
      console.error('Failed to generate QR code:', error);
      alert('Failed to generate QR code');
    }
  };

  const downloadQRCode = () => {
    // TODO: Implement QR code download
    alert('QR Code download will be implemented');
  };

  const renderOverview = () => (
    <div className="campaign-overview">
      <div className="grid grid-2">
        <div className="card">
          <h4>Campaign Details</h4>
          <div className="detail-item">
            <span className="label">Name:</span>
            <span className="value">{campaign.name}</span>
          </div>
          <div className="detail-item">
            <span className="label">Description:</span>
            <span className="value">{campaign.description}</span>
          </div>
          <div className="detail-item">
            <span className="label">Game Type:</span>
            <span className="value">{campaign.gameType}</span>
          </div>
          <div className="detail-item">
            <span className="label">Status:</span>
            <span className={`value status-badge ${campaign.isActive ? 'active' : 'inactive'}`}>
              {campaign.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
        </div>

        <div className="card">
          <h4>Game Settings</h4>
          <div className="detail-item">
            <span className="label">Token Cost:</span>
            <span className="value">{campaign.tokenCostPerSpin} tokens</span>
          </div>
          <div className="detail-item">
            <span className="label">Max Spins/Day:</span>
            <span className="value">{campaign.maxSpinsPerDay}</span>
          </div>
          <div className="detail-item">
            <span className="label">Start Date:</span>
            <span className="value">{new Date(campaign.startDate).toLocaleDateString()}</span>
          </div>
          <div className="detail-item">
            <span className="label">End Date:</span>
            <span className="value">
              {campaign.endDate ? new Date(campaign.endDate).toLocaleDateString() : 'Ongoing'}
            </span>
          </div>
        </div>
      </div>

      <div className="grid grid-3" style={{ marginTop: '20px' }}>
        <div className="card metric-card">
          <h4>Total Spins</h4>
          <div className="metric-value primary">{campaign.totalSpins}</div>
          <p className="metric-description">All-time spins</p>
        </div>

        <div className="card metric-card">
          <h4>Total Prizes</h4>
          <div className="metric-value success">{campaign.totalPrizes}</div>
          <p className="metric-description">Available prizes</p>
        </div>

        <div className="card metric-card">
          <h4>Engagement Rate</h4>
          <div className="metric-value info">73%</div>
          <p className="metric-description">Player return rate</p>
        </div>
      </div>
    </div>
  );

  const renderPrizes = () => (
    <div className="prizes-management">
      <div className="prizes-header">
        <h4>Prize Inventory</h4>
        <button className="btn btn-primary">Add New Prize</button>
      </div>

      <div className="prizes-table">
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ backgroundColor: '#f8f9fa' }}>
              <th style={{ padding: '12px', textAlign: 'left', borderBottom: '2px solid #dee2e6' }}>Prize Name</th>
              <th style={{ padding: '12px', textAlign: 'left', borderBottom: '2px solid #dee2e6' }}>Value</th>
              <th style={{ padding: '12px', textAlign: 'left', borderBottom: '2px solid #dee2e6' }}>Quantity</th>
              <th style={{ padding: '12px', textAlign: 'left', borderBottom: '2px solid #dee2e6' }}>Remaining</th>
              <th style={{ padding: '12px', textAlign: 'left', borderBottom: '2px solid #dee2e6' }}>Win Rate</th>
              <th style={{ padding: '12px', textAlign: 'left', borderBottom: '2px solid #dee2e6' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>Free Coffee</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>$5.00</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>100</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>87</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>30%</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>
                <button className="btn btn-primary" style={{ padding: '5px 10px', fontSize: '12px', marginRight: '5px' }}>Edit</button>
                <button className="btn btn-danger" style={{ padding: '5px 10px', fontSize: '12px' }}>Remove</button>
              </td>
            </tr>
            <tr>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>10% Discount</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>$0.00</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>200</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>156</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>50%</td>
              <td style={{ padding: '12px', borderBottom: '1px solid #dee2e6' }}>
                <button className="btn btn-primary" style={{ padding: '5px 10px', fontSize: '12px', marginRight: '5px' }}>Edit</button>
                <button className="btn btn-danger" style={{ padding: '5px 10px', fontSize: '12px' }}>Remove</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );

  const renderQRCode = () => (
    <div className="qr-code-section">
      <div className="card">
        <h4>QR Code Management</h4>
        <p>Generate and manage QR codes for this campaign</p>

        <div className="grid grid-2">
          <div>
            <h5>Campaign QR Code</h5>
            {qrCodeGenerated || campaign.qrCodeUrl ? (
              <div className="qr-code-display">
                <div className="qr-placeholder" style={{ 
                  width: '200px', 
                  height: '200px', 
                  backgroundColor: '#f8f9fa', 
                  border: '2px dashed #dee2e6',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  marginBottom: '15px'
                }}>
                  QR Code Preview
                </div>
                <div className="qr-actions">
                  <button onClick={downloadQRCode} className="btn btn-primary">
                    Download QR Code
                  </button>
                  <button onClick={generateQRCode} className="btn btn-secondary">
                    Regenerate
                  </button>
                </div>
              </div>
            ) : (
              <div>
                <p>No QR code generated yet</p>
                <button onClick={generateQRCode} className="btn btn-success">
                  Generate QR Code
                </button>
              </div>
            )}
          </div>

          <div>
            <h5>QR Code Information</h5>
            <div className="detail-item">
              <span className="label">Campaign ID:</span>
              <span className="value">{campaign.id}</span>
            </div>
            <div className="detail-item">
              <span className="label">QR URL:</span>
              <span className="value" style={{ fontSize: '12px', wordBreak: 'break-all' }}>
                {campaign.qrCodeUrl || 'Not generated'}
              </span>
            </div>
            <div className="detail-item">
              <span className="label">Scans Today:</span>
              <span className="value">24</span>
            </div>
            <div className="detail-item">
              <span className="label">Total Scans:</span>
              <span className="value">156</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );

  return (
    <div className="modal-overlay">
      <div className="modal-content campaign-detail">
        <div className="modal-header">
          <h2>{campaign.name}</h2>
          <button onClick={onClose} className="close-button">&times;</button>
        </div>

        <div className="campaign-tabs">
          <button 
            className={`tab-button ${activeTab === 'overview' ? 'active' : ''}`}
            onClick={() => setActiveTab('overview')}
          >
            Overview
          </button>
          <button 
            className={`tab-button ${activeTab === 'prizes' ? 'active' : ''}`}
            onClick={() => setActiveTab('prizes')}
          >
            Prizes
          </button>
          <button 
            className={`tab-button ${activeTab === 'qrcode' ? 'active' : ''}`}
            onClick={() => setActiveTab('qrcode')}
          >
            QR Code
          </button>
        </div>

        <div className="tab-content">
          {activeTab === 'overview' && renderOverview()}
          {activeTab === 'prizes' && renderPrizes()}
          {activeTab === 'qrcode' && renderQRCode()}
        </div>
      </div>
    </div>
  );
};

export default CampaignDetailView;