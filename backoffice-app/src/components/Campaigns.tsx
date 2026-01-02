import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import axios from 'axios';
import CampaignCreationWizard from './CampaignCreationWizard';
import CampaignDetailView from './CampaignDetailView';

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

interface Prize {
  id?: string;
  name: string;
  description: string;
  value: number;
  quantity: number;
  winProbability: number;
}

const Campaigns: React.FC = () => {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [loading, setLoading] = useState(false);
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    loadCampaigns();
  }, []);

  const loadCampaigns = async () => {
    try {
      // TODO: Replace with actual API call
      // Mock data for now
      setCampaigns([
        {
          id: '1',
          name: 'Holiday Wheel of Fortune',
          description: 'Spin to win holiday prizes!',
          gameType: 'WheelOfLuck',
          tokenCostPerSpin: 5,
          maxSpinsPerDay: 3,
          isActive: true,
          startDate: '2024-12-01',
          endDate: '2024-12-31',
          totalSpins: 245,
          totalPrizes: 8,
          qrCodeUrl: 'https://api.chanzup.com/qr/campaign1'
        },
        {
          id: '2',
          name: 'New Year Treasure Hunt',
          description: 'Multi-location treasure hunt experience',
          gameType: 'TreasureHunt',
          tokenCostPerSpin: 3,
          maxSpinsPerDay: 5,
          isActive: false,
          startDate: '2025-01-01',
          endDate: '2025-01-15',
          totalSpins: 0,
          totalPrizes: 12,
          qrCodeUrl: 'https://api.chanzup.com/qr/campaign2'
        }
      ]);
    } catch (error) {
      console.error('Failed to load campaigns:', error);
    }
  };

  const toggleCampaignStatus = async (campaignId: string, isActive: boolean) => {
    try {
      // TODO: Implement API call
      setCampaigns(campaigns.map(c => 
        c.id === campaignId ? { ...c, isActive: !isActive } : c
      ));
    } catch (error) {
      console.error('Failed to toggle campaign status:', error);
    }
  };

  const deleteCampaign = async (campaignId: string) => {
    if (!window.confirm('Are you sure you want to delete this campaign?')) {
      return;
    }
    
    try {
      // TODO: Implement API call
      setCampaigns(campaigns.filter(c => c.id !== campaignId));
    } catch (error) {
      console.error('Failed to delete campaign:', error);
    }
  };
  return (
    <div>
      <div className="sidebar">
        <ul>
          <li><Link to="/dashboard">Dashboard</Link></li>
          <li><Link to="/campaigns" className="active">Campaigns</Link></li>
          <li><Link to="/analytics">Analytics</Link></li>
          <li><Link to="/prizes">Prizes</Link></li>
          <li><Link to="/redemption">Redemption</Link></li>
          <li><Link to="/settings">Settings</Link></li>
        </ul>
      </div>
      
      <div className="main-content">
        <div className="dashboard-header">
          <h2>Campaign Management</h2>
          <div>
            <button 
              onClick={() => setShowCreateForm(true)} 
              className="btn btn-success"
            >
              Create New Campaign
            </button>
          </div>
        </div>
        
        {showCreateForm && (
          <CampaignCreationWizard 
            onClose={() => setShowCreateForm(false)}
            onCampaignCreated={loadCampaigns}
          />
        )}
        
        {selectedCampaign && (
          <CampaignDetailView 
            campaign={selectedCampaign}
            onClose={() => setSelectedCampaign(null)}
            onUpdate={loadCampaigns}
          />
        )}
        
        <div className="campaigns-grid">
          {campaigns.length > 0 ? (
            campaigns.map((campaign) => (
              <div key={campaign.id} className="campaign-card card">
                <div className="campaign-header">
                  <h3>{campaign.name}</h3>
                  <div className="campaign-status">
                    <span className={`status-badge ${campaign.isActive ? 'active' : 'inactive'}`}>
                      {campaign.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>
                </div>
                
                <p className="campaign-description">{campaign.description}</p>
                
                <div className="campaign-stats">
                  <div className="stat">
                    <span className="stat-label">Game Type:</span>
                    <span className="stat-value">{campaign.gameType}</span>
                  </div>
                  <div className="stat">
                    <span className="stat-label">Token Cost:</span>
                    <span className="stat-value">{campaign.tokenCostPerSpin}</span>
                  </div>
                  <div className="stat">
                    <span className="stat-label">Total Spins:</span>
                    <span className="stat-value">{campaign.totalSpins}</span>
                  </div>
                  <div className="stat">
                    <span className="stat-label">Prizes:</span>
                    <span className="stat-value">{campaign.totalPrizes}</span>
                  </div>
                </div>
                
                <div className="campaign-dates">
                  <small>
                    {new Date(campaign.startDate).toLocaleDateString()} - 
                    {campaign.endDate ? new Date(campaign.endDate).toLocaleDateString() : 'Ongoing'}
                  </small>
                </div>
                
                <div className="campaign-actions">
                  <button 
                    onClick={() => setSelectedCampaign(campaign)}
                    className="btn btn-primary"
                  >
                    View Details
                  </button>
                  <button 
                    onClick={() => toggleCampaignStatus(campaign.id, campaign.isActive)}
                    className={`btn ${campaign.isActive ? 'btn-warning' : 'btn-success'}`}
                  >
                    {campaign.isActive ? 'Pause' : 'Activate'}
                  </button>
                  <button 
                    onClick={() => deleteCampaign(campaign.id)}
                    className="btn btn-danger"
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))
          ) : (
            <div className="card" style={{ gridColumn: '1 / -1' }}>
              <h3>No Campaigns Yet</h3>
              <p>Create your first campaign to start engaging with customers!</p>
              <div style={{ padding: '20px', backgroundColor: '#f8f9fa', borderRadius: '8px', marginTop: '20px' }}>
                <h4>Campaign Types Available:</h4>
                <ul style={{ textAlign: 'left', marginTop: '15px' }}>
                  <li><strong>Wheel of Luck:</strong> Players spin a wheel to win prizes based on configurable odds</li>
                  <li><strong>Treasure Hunt:</strong> Multi-location experiences where players collect rewards</li>
                </ul>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Campaigns;