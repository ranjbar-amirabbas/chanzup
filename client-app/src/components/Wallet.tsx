import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { WalletData, PlayerPrize, TokenTransaction, walletService } from '../services/walletService';

const Wallet: React.FC = () => {
  const [walletData, setWalletData] = useState<WalletData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState<'prizes' | 'transactions'>('prizes');
  const [refreshing, setRefreshing] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    loadWalletData();
  }, []);

  const loadWalletData = async () => {
    try {
      setLoading(true);
      setError('');
      const data = await walletService.getWallet();
      setWalletData(data);
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to load wallet data');
    } finally {
      setLoading(false);
    }
  };

  const handleRefresh = async () => {
    setRefreshing(true);
    await loadWalletData();
    setRefreshing(false);
  };

  const handleRedeemPrize = async (prizeId: string) => {
    try {
      const result = await walletService.redeemPrize(prizeId);
      if (result.success) {
        // Refresh wallet data to show updated prize status
        await loadWalletData();
      } else {
        setError(result.message);
      }
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to redeem prize');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const isExpired = (expiresAt: string) => {
    return new Date(expiresAt) < new Date();
  };

  const getExpirationStatus = (expiresAt: string) => {
    const expirationDate = new Date(expiresAt);
    const now = new Date();
    const daysUntilExpiration = Math.ceil((expirationDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    if (daysUntilExpiration < 0) {
      return { status: 'expired', text: 'Expired', color: '#dc3545' };
    } else if (daysUntilExpiration <= 3) {
      return { status: 'expiring', text: `Expires in ${daysUntilExpiration} day${daysUntilExpiration === 1 ? '' : 's'}`, color: '#fd7e14' };
    } else {
      return { status: 'valid', text: `Expires ${formatDate(expiresAt)}`, color: '#6c757d' };
    }
  };

  if (loading) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          Loading wallet...
        </div>
      </div>
    );
  }

  if (error || !walletData) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <div style={{ color: 'red', marginBottom: '20px' }}>
            {error || 'Failed to load wallet'}
          </div>
          <button onClick={loadWalletData} className="btn btn-primary">
            Try Again
          </button>
        </div>
      </div>
    );
  }

  const activePrizes = walletData.prizes.filter(prize => !prize.isRedeemed && !isExpired(prize.expiresAt));
  const redeemedPrizes = walletData.prizes.filter(prize => prize.isRedeemed);
  const expiredPrizes = walletData.prizes.filter(prize => !prize.isRedeemed && isExpired(prize.expiresAt));

  return (
    <div className="container">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <h2>My Wallet</h2>
        <button 
          onClick={handleRefresh}
          disabled={refreshing}
          className="btn btn-secondary"
        >
          {refreshing ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {/* Token Balance */}
      <div style={{ 
        backgroundColor: '#007bff', 
        color: 'white',
        padding: '30px', 
        borderRadius: '12px', 
        marginBottom: '30px',
        textAlign: 'center'
      }}>
        <h3 style={{ margin: '0 0 10px 0', fontSize: '18px' }}>Token Balance</h3>
        <div style={{ fontSize: '48px', fontWeight: 'bold', margin: '10px 0' }}>
          🪙 {walletData.tokenBalance}
        </div>
        <p style={{ margin: '0', opacity: 0.9 }}>
          Earn more tokens by visiting businesses and scanning QR codes
        </p>
      </div>

      {/* Navigation Tabs */}
      <div style={{ 
        display: 'flex', 
        borderBottom: '2px solid #dee2e6',
        marginBottom: '20px'
      }}>
        <button
          onClick={() => setActiveTab('prizes')}
          style={{
            padding: '15px 20px',
            border: 'none',
            background: 'none',
            borderBottom: activeTab === 'prizes' ? '2px solid #007bff' : 'none',
            color: activeTab === 'prizes' ? '#007bff' : '#6c757d',
            fontWeight: activeTab === 'prizes' ? 'bold' : 'normal',
            cursor: 'pointer'
          }}
        >
          My Prizes ({walletData.prizes.length})
        </button>
        <button
          onClick={() => setActiveTab('transactions')}
          style={{
            padding: '15px 20px',
            border: 'none',
            background: 'none',
            borderBottom: activeTab === 'transactions' ? '2px solid #007bff' : 'none',
            color: activeTab === 'transactions' ? '#007bff' : '#6c757d',
            fontWeight: activeTab === 'transactions' ? 'bold' : 'normal',
            cursor: 'pointer'
          }}
        >
          Transactions ({walletData.recentTransactions.length})
        </button>
      </div>

      {/* Prizes Tab */}
      {activeTab === 'prizes' && (
        <div>
          {/* Active Prizes */}
          {activePrizes.length > 0 && (
            <div style={{ marginBottom: '30px' }}>
              <h3 style={{ color: '#28a745', marginBottom: '15px' }}>
                🏆 Active Prizes ({activePrizes.length})
              </h3>
              <div style={{ display: 'grid', gap: '15px' }}>
                {activePrizes.map((prize) => {
                  const expiration = getExpirationStatus(prize.expiresAt);
                  return (
                    <div
                      key={prize.id}
                      style={{
                        border: '2px solid #28a745',
                        borderRadius: '8px',
                        padding: '20px',
                        backgroundColor: '#f8fff9'
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <div style={{ flex: 1 }}>
                          <h4 style={{ margin: '0 0 5px 0', color: '#28a745' }}>
                            {prize.name}
                          </h4>
                          <p style={{ margin: '0 0 10px 0', color: '#666' }}>
                            {prize.description}
                          </p>
                          <p style={{ margin: '0 0 15px 0', fontWeight: 'bold' }}>
                            📍 {prize.businessName}
                          </p>
                          
                          <div style={{ 
                            backgroundColor: '#fff',
                            border: '2px dashed #007bff',
                            borderRadius: '8px',
                            padding: '15px',
                            margin: '15px 0',
                            textAlign: 'center'
                          }}>
                            <div style={{ fontSize: '12px', color: '#666', marginBottom: '5px' }}>
                              REDEMPTION CODE
                            </div>
                            <div style={{ 
                              fontFamily: 'monospace',
                              fontSize: '18px',
                              fontWeight: 'bold',
                              color: '#007bff'
                            }}>
                              {prize.redemptionCode}
                            </div>
                          </div>

                          <div style={{ 
                            fontSize: '14px', 
                            color: expiration.color,
                            fontWeight: expiration.status === 'expiring' ? 'bold' : 'normal'
                          }}>
                            {expiration.text}
                          </div>
                        </div>
                      </div>

                      <div style={{ 
                        marginTop: '20px', 
                        paddingTop: '15px', 
                        borderTop: '1px solid #dee2e6',
                        fontSize: '14px',
                        color: '#666'
                      }}>
                        💡 Present this code at {prize.businessName} to redeem your prize
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Redeemed Prizes */}
          {redeemedPrizes.length > 0 && (
            <div style={{ marginBottom: '30px' }}>
              <h3 style={{ color: '#6c757d', marginBottom: '15px' }}>
                ✅ Redeemed Prizes ({redeemedPrizes.length})
              </h3>
              <div style={{ display: 'grid', gap: '15px' }}>
                {redeemedPrizes.map((prize) => (
                  <div
                    key={prize.id}
                    style={{
                      border: '1px solid #dee2e6',
                      borderRadius: '8px',
                      padding: '15px',
                      backgroundColor: '#f8f9fa',
                      opacity: 0.8
                    }}
                  >
                    <h4 style={{ margin: '0 0 5px 0', color: '#6c757d' }}>
                      {prize.name}
                    </h4>
                    <p style={{ margin: '0 0 5px 0', fontSize: '14px', color: '#666' }}>
                      {prize.businessName}
                    </p>
                    <p style={{ margin: '0', fontSize: '14px', color: '#28a745' }}>
                      ✅ Redeemed
                    </p>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Expired Prizes */}
          {expiredPrizes.length > 0 && (
            <div style={{ marginBottom: '30px' }}>
              <h3 style={{ color: '#dc3545', marginBottom: '15px' }}>
                ⏰ Expired Prizes ({expiredPrizes.length})
              </h3>
              <div style={{ display: 'grid', gap: '15px' }}>
                {expiredPrizes.map((prize) => (
                  <div
                    key={prize.id}
                    style={{
                      border: '1px solid #dc3545',
                      borderRadius: '8px',
                      padding: '15px',
                      backgroundColor: '#fff5f5',
                      opacity: 0.8
                    }}
                  >
                    <h4 style={{ margin: '0 0 5px 0', color: '#dc3545' }}>
                      {prize.name}
                    </h4>
                    <p style={{ margin: '0 0 5px 0', fontSize: '14px', color: '#666' }}>
                      {prize.businessName}
                    </p>
                    <p style={{ margin: '0', fontSize: '14px', color: '#dc3545' }}>
                      ⏰ Expired on {formatDate(prize.expiresAt)}
                    </p>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* No Prizes */}
          {walletData.prizes.length === 0 && (
            <div style={{ 
              textAlign: 'center', 
              padding: '60px 20px',
              backgroundColor: '#f8f9fa',
              borderRadius: '8px'
            }}>
              <div style={{ fontSize: '48px', marginBottom: '20px' }}>🎁</div>
              <h3 style={{ margin: '0 0 15px 0' }}>No prizes yet</h3>
              <p style={{ margin: '0 0 20px 0', color: '#666' }}>
                Visit businesses, scan QR codes, and spin the wheel to win prizes!
              </p>
              <button 
                onClick={() => navigate('/nearby')}
                className="btn btn-primary"
              >
                Find Businesses
              </button>
            </div>
          )}
        </div>
      )}

      {/* Transactions Tab */}
      {activeTab === 'transactions' && (
        <div>
          <h3 style={{ marginBottom: '20px' }}>Recent Transactions</h3>
          
          {walletData.recentTransactions.length === 0 ? (
            <div style={{ 
              textAlign: 'center', 
              padding: '60px 20px',
              backgroundColor: '#f8f9fa',
              borderRadius: '8px'
            }}>
              <div style={{ fontSize: '48px', marginBottom: '20px' }}>📊</div>
              <h3 style={{ margin: '0 0 15px 0' }}>No transactions yet</h3>
              <p style={{ margin: '0', color: '#666' }}>
                Your token earning and spending history will appear here
              </p>
            </div>
          ) : (
            <div style={{ display: 'grid', gap: '10px' }}>
              {walletData.recentTransactions.map((transaction) => (
                <div
                  key={transaction.id}
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    padding: '15px',
                    border: '1px solid #dee2e6',
                    borderRadius: '8px',
                    backgroundColor: 'white'
                  }}
                >
                  <div style={{ flex: 1 }}>
                    <div style={{ fontWeight: 'bold', marginBottom: '5px' }}>
                      {transaction.description}
                    </div>
                    <div style={{ fontSize: '14px', color: '#666' }}>
                      {formatDate(transaction.timestamp)}
                    </div>
                  </div>
                  <div style={{ 
                    fontSize: '18px', 
                    fontWeight: 'bold',
                    color: transaction.type === 'earned' || transaction.type === 'purchased' ? '#28a745' : '#dc3545'
                  }}>
                    {transaction.type === 'earned' || transaction.type === 'purchased' ? '+' : '-'}
                    {transaction.amount} 🪙
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Quick Actions */}
      <div style={{ 
        marginTop: '40px',
        padding: '20px',
        backgroundColor: '#f8f9fa',
        borderRadius: '8px'
      }}>
        <h4 style={{ margin: '0 0 15px 0' }}>Quick Actions</h4>
        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
          <button 
            onClick={() => navigate('/nearby')}
            className="btn btn-primary"
          >
            Find Businesses
          </button>
          <button 
            onClick={() => navigate('/qr-scanner')}
            className="btn btn-secondary"
          >
            Scan QR Code
          </button>
          <button 
            onClick={() => navigate('/dashboard')}
            className="btn btn-secondary"
          >
            Dashboard
          </button>
        </div>
      </div>
    </div>
  );
};

export default Wallet;