import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Business, Campaign, businessService } from '../services/businessService';

const BusinessDetail: React.FC = () => {
  const { businessId } = useParams<{ businessId: string }>();
  const [business, setBusiness] = useState<Business | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    if (businessId) {
      loadBusinessDetails();
    }
  }, [businessId]);

  const loadBusinessDetails = async () => {
    if (!businessId) return;

    try {
      setLoading(true);
      setError('');
      const businessData = await businessService.getBusinessDetails(businessId);
      setBusiness(businessData);
    } catch (err: any) {
      setError(err.message || 'Failed to load business details');
    } finally {
      setLoading(false);
    }
  };

  const handleScanQR = () => {
    navigate('/qr-scanner', { state: { businessId } });
  };

  const handleGetDirections = () => {
    if (business) {
      const url = `https://www.google.com/maps/dir/?api=1&destination=${business.location.latitude},${business.location.longitude}`;
      window.open(url, '_blank');
    }
  };

  if (loading) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          Loading business details...
        </div>
      </div>
    );
  }

  if (error || !business) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <div style={{ color: 'red', marginBottom: '20px' }}>
            {error || 'Business not found'}
          </div>
          <button onClick={() => navigate('/nearby')} className="btn btn-primary">
            Back to Businesses
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      {/* Header */}
      <div style={{ marginBottom: '20px' }}>
        <button 
          onClick={() => navigate('/nearby')} 
          style={{ 
            background: 'none', 
            border: 'none', 
            color: '#007bff', 
            cursor: 'pointer',
            fontSize: '16px',
            marginBottom: '10px'
          }}
        >
          ← Back to Businesses
        </button>
        <h1 style={{ margin: '0' }}>{business.name}</h1>
      </div>

      {/* Business Info */}
      <div style={{ 
        backgroundColor: '#f8f9fa', 
        padding: '20px', 
        borderRadius: '8px', 
        marginBottom: '30px' 
      }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px' }}>
          <div>
            <h3 style={{ margin: '0 0 10px 0' }}>📍 Location</h3>
            <p style={{ margin: '0', color: '#666' }}>{business.address}</p>
            <p style={{ margin: '5px 0 0 0', color: '#666' }}>
              {business.distance}m away
            </p>
          </div>
          <div>
            <h3 style={{ margin: '0 0 10px 0' }}>🎯 Campaigns</h3>
            <p style={{ margin: '0', fontSize: '24px', fontWeight: 'bold', color: '#007bff' }}>
              {business.activeCampaigns}
            </p>
            <p style={{ margin: '5px 0 0 0', color: '#666' }}>
              Active campaigns
            </p>
          </div>
        </div>

        <div style={{ marginTop: '20px', display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
          <button onClick={handleScanQR} className="btn btn-primary">
            📱 Scan QR Code
          </button>
          <button onClick={handleGetDirections} className="btn btn-secondary">
            🧭 Get Directions
          </button>
        </div>
      </div>

      {/* Campaigns */}
      <div>
        <h2 style={{ marginBottom: '20px' }}>Available Campaigns</h2>
        
        {business.campaigns.length === 0 ? (
          <div style={{ 
            textAlign: 'center', 
            padding: '40px', 
            backgroundColor: '#f8f9fa', 
            borderRadius: '8px' 
          }}>
            <div style={{ fontSize: '18px', marginBottom: '10px' }}>🎯</div>
            <div>No active campaigns at this location</div>
            <div style={{ fontSize: '14px', color: '#666', marginTop: '10px' }}>
              Check back later for new campaigns
            </div>
          </div>
        ) : (
          <div style={{ display: 'grid', gap: '20px' }}>
            {business.campaigns.map((campaign) => (
              <div
                key={campaign.id}
                style={{
                  border: '1px solid #ddd',
                  borderRadius: '8px',
                  padding: '20px',
                  backgroundColor: 'white'
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div style={{ flex: 1 }}>
                    <h3 style={{ margin: '0 0 10px 0', color: '#007bff' }}>
                      {campaign.name}
                    </h3>
                    <p style={{ margin: '0 0 15px 0', color: '#666' }}>
                      {campaign.description}
                    </p>
                    
                    <div style={{ display: 'flex', gap: '20px', marginBottom: '15px' }}>
                      <div style={{ 
                        backgroundColor: '#e3f2fd', 
                        padding: '10px 15px', 
                        borderRadius: '20px',
                        fontSize: '14px'
                      }}>
                        🪙 {campaign.tokenCostPerSpin} tokens per spin
                      </div>
                    </div>

                    {campaign.topPrizes.length > 0 && (
                      <div>
                        <h4 style={{ margin: '0 0 10px 0', fontSize: '16px' }}>
                          🏆 Top Prizes:
                        </h4>
                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px' }}>
                          {campaign.topPrizes.map((prize, index) => (
                            <span
                              key={index}
                              style={{
                                backgroundColor: '#fff3cd',
                                padding: '5px 10px',
                                borderRadius: '15px',
                                fontSize: '14px',
                                border: '1px solid #ffeaa7'
                              }}
                            >
                              {prize}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                <div style={{ marginTop: '20px', paddingTop: '20px', borderTop: '1px solid #eee' }}>
                  <button 
                    onClick={handleScanQR}
                    className="btn btn-primary"
                    style={{ marginRight: '10px' }}
                  >
                    Scan QR to Play
                  </button>
                  <span style={{ fontSize: '14px', color: '#666' }}>
                    Visit this location and scan the QR code to earn tokens and play
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default BusinessDetail;