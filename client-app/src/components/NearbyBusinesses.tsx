import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Business, businessService } from '../services/businessService';
import { locationService, Location } from '../services/locationService';

const NearbyBusinesses: React.FC = () => {
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [userLocation, setUserLocation] = useState<Location | null>(null);
  const [viewMode, setViewMode] = useState<'list' | 'map'>('list');
  const [searchRadius, setSearchRadius] = useState(5000);
  const [categoryFilter, setCategoryFilter] = useState('all');
  const navigate = useNavigate();

  useEffect(() => {
    loadNearbyBusinesses();
  }, [searchRadius]);

  const loadNearbyBusinesses = async () => {
    try {
      setLoading(true);
      setError('');

      // Get user location
      const location = await locationService.getCurrentLocation();
      setUserLocation(location);

      // Fetch nearby businesses
      const nearbyBusinesses = await businessService.getNearbyBusinesses({
        latitude: location.latitude,
        longitude: location.longitude,
        radius: searchRadius
      });

      setBusinesses(nearbyBusinesses);
    } catch (err: any) {
      setError(err.message || 'Failed to load nearby businesses');
    } finally {
      setLoading(false);
    }
  };

  const handleBusinessClick = (businessId: string) => {
    navigate(`/business/${businessId}`);
  };

  const handleRefresh = () => {
    loadNearbyBusinesses();
  };

  const filteredBusinesses = businesses.filter(business => {
    if (categoryFilter === 'all') return true;
    if (categoryFilter === 'active') return business.activeCampaigns > 0;
    return true;
  });

  if (loading) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <div>Loading nearby businesses...</div>
          <div style={{ marginTop: '10px', fontSize: '14px', color: '#666' }}>
            Getting your location and finding participating businesses
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <div style={{ color: 'red', marginBottom: '20px' }}>{error}</div>
          <button onClick={handleRefresh} className="btn btn-primary">
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <h2>Nearby Businesses</h2>
        <button onClick={handleRefresh} className="btn btn-secondary">
          Refresh
        </button>
      </div>

      {/* Controls */}
      <div style={{ display: 'flex', gap: '20px', marginBottom: '20px', flexWrap: 'wrap' }}>
        <div>
          <label htmlFor="viewMode" style={{ marginRight: '10px' }}>View:</label>
          <select
            id="viewMode"
            value={viewMode}
            onChange={(e) => setViewMode(e.target.value as 'list' | 'map')}
            style={{ padding: '5px' }}
          >
            <option value="list">List View</option>
            <option value="map">Map View</option>
          </select>
        </div>

        <div>
          <label htmlFor="radius" style={{ marginRight: '10px' }}>Radius:</label>
          <select
            id="radius"
            value={searchRadius}
            onChange={(e) => setSearchRadius(Number(e.target.value))}
            style={{ padding: '5px' }}
          >
            <option value={1000}>1 km</option>
            <option value={2000}>2 km</option>
            <option value={5000}>5 km</option>
            <option value={10000}>10 km</option>
          </select>
        </div>

        <div>
          <label htmlFor="category" style={{ marginRight: '10px' }}>Filter:</label>
          <select
            id="category"
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
            style={{ padding: '5px' }}
          >
            <option value="all">All Businesses</option>
            <option value="active">With Active Campaigns</option>
          </select>
        </div>
      </div>

      {/* Results Summary */}
      <div style={{ marginBottom: '20px', color: '#666' }}>
        Found {filteredBusinesses.length} businesses within {searchRadius / 1000} km
      </div>

      {/* Business List */}
      {viewMode === 'list' && (
        <div style={{ display: 'grid', gap: '20px' }}>
          {filteredBusinesses.map((business) => (
            <div
              key={business.businessId}
              onClick={() => handleBusinessClick(business.businessId)}
              style={{
                border: '1px solid #ddd',
                borderRadius: '8px',
                padding: '20px',
                cursor: 'pointer',
                transition: 'box-shadow 0.2s',
                ':hover': { boxShadow: '0 2px 8px rgba(0,0,0,0.1)' }
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div style={{ flex: 1 }}>
                  <h3 style={{ margin: '0 0 10px 0', color: '#007bff' }}>
                    {business.name}
                  </h3>
                  <p style={{ margin: '0 0 10px 0', color: '#666' }}>
                    {business.address}
                  </p>
                  <div style={{ display: 'flex', gap: '20px', fontSize: '14px' }}>
                    <span>📍 {business.distance}m away</span>
                    <span>🎯 {business.activeCampaigns} active campaigns</span>
                  </div>
                </div>
              </div>

              {business.campaigns.length > 0 && (
                <div style={{ marginTop: '15px', paddingTop: '15px', borderTop: '1px solid #eee' }}>
                  <h4 style={{ margin: '0 0 10px 0', fontSize: '16px' }}>Active Campaigns:</h4>
                  {business.campaigns.slice(0, 2).map((campaign) => (
                    <div key={campaign.id} style={{ marginBottom: '10px' }}>
                      <div style={{ fontWeight: 'bold' }}>{campaign.name}</div>
                      <div style={{ fontSize: '14px', color: '#666' }}>
                        {campaign.description}
                      </div>
                      <div style={{ fontSize: '14px', marginTop: '5px' }}>
                        🪙 {campaign.tokenCostPerSpin} tokens per spin
                      </div>
                      {campaign.topPrizes.length > 0 && (
                        <div style={{ fontSize: '14px', color: '#007bff' }}>
                          Prizes: {campaign.topPrizes.join(', ')}
                        </div>
                      )}
                    </div>
                  ))}
                  {business.campaigns.length > 2 && (
                    <div style={{ fontSize: '14px', color: '#007bff' }}>
                      +{business.campaigns.length - 2} more campaigns
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Map View Placeholder */}
      {viewMode === 'map' && (
        <div style={{
          height: '400px',
          border: '1px solid #ddd',
          borderRadius: '8px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: '#f8f9fa'
        }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '18px', marginBottom: '10px' }}>🗺️</div>
            <div>Map view will be implemented with Google Maps integration</div>
            <div style={{ fontSize: '14px', color: '#666', marginTop: '10px' }}>
              Showing {filteredBusinesses.length} businesses
            </div>
          </div>
        </div>
      )}

      {filteredBusinesses.length === 0 && (
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <div style={{ fontSize: '18px', marginBottom: '10px' }}>🔍</div>
          <div>No businesses found in your area</div>
          <div style={{ fontSize: '14px', color: '#666', marginTop: '10px' }}>
            Try increasing the search radius or check back later
          </div>
        </div>
      )}
    </div>
  );
};

export default NearbyBusinesses;