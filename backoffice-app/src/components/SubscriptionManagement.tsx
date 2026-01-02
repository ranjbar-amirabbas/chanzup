import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

interface SubscriptionPlan {
  id: number;
  name: string;
  price: number;
  features: string[];
  recommended?: boolean;
}

const SubscriptionManagement: React.FC = () => {
  const [currentPlan, setCurrentPlan] = useState(0);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const plans: SubscriptionPlan[] = [
    {
      id: 0,
      name: 'Basic',
      price: 0,
      features: [
        'Up to 2 active campaigns',
        'Basic analytics',
        'Standard support',
        'QR code generation',
        'Prize management'
      ]
    },
    {
      id: 1,
      name: 'Premium',
      price: 29,
      recommended: true,
      features: [
        'Unlimited campaigns',
        'Advanced analytics',
        'Priority support',
        'Multi-location support',
        'Custom branding',
        'Export reports',
        'Real-time notifications'
      ]
    },
    {
      id: 2,
      name: 'Enterprise',
      price: 99,
      features: [
        'Everything in Premium',
        'API access',
        'White-label solution',
        'Dedicated support',
        'Custom integrations',
        'Advanced fraud protection',
        'Custom reporting'
      ]
    }
  ];

  useEffect(() => {
    const subscriptionTier = parseInt(localStorage.getItem('subscriptionTier') || '0');
    setCurrentPlan(subscriptionTier);
  }, []);

  const handleUpgrade = async (planId: number) => {
    setLoading(true);
    try {
      // TODO: Implement actual subscription upgrade API call
      // For now, just update localStorage
      localStorage.setItem('subscriptionTier', planId.toString());
      setCurrentPlan(planId);
      
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      alert(`Successfully upgraded to ${plans[planId].name} plan!`);
      navigate('/dashboard');
    } catch (error) {
      console.error('Upgrade failed:', error);
      alert('Upgrade failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container" style={{ maxWidth: '1000px' }}>
      <div className="card">
        <h2>Subscription Management</h2>
        <p>Choose the plan that best fits your business needs</p>
        
        <div className="subscription-plans" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))' }}>
          {plans.map((plan) => (
            <div 
              key={plan.id} 
              className={`plan-card ${currentPlan === plan.id ? 'selected' : ''} ${plan.recommended ? 'recommended' : ''}`}
            >
              {plan.recommended && (
                <div className="recommended-badge">Most Popular</div>
              )}
              
              <h3>{plan.name}</h3>
              <div className="price">
                {plan.price === 0 ? 'Free' : `$${plan.price}/month`}
              </div>
              
              <ul>
                {plan.features.map((feature, index) => (
                  <li key={index}>{feature}</li>
                ))}
              </ul>
              
              <div style={{ marginTop: 'auto', paddingTop: '20px' }}>
                {currentPlan === plan.id ? (
                  <button className="btn btn-secondary" disabled>
                    Current Plan
                  </button>
                ) : currentPlan > plan.id ? (
                  <button 
                    className="btn btn-secondary" 
                    onClick={() => handleUpgrade(plan.id)}
                    disabled={loading}
                  >
                    Downgrade
                  </button>
                ) : (
                  <button 
                    className="btn btn-success" 
                    onClick={() => handleUpgrade(plan.id)}
                    disabled={loading}
                  >
                    {loading ? 'Processing...' : 'Upgrade'}
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
        
        <div className="subscription-info" style={{ marginTop: '40px', padding: '20px', backgroundColor: '#f8f9fa', borderRadius: '8px' }}>
          <h4>Subscription Information</h4>
          <p><strong>Current Plan:</strong> {plans[currentPlan].name}</p>
          <p><strong>Monthly Cost:</strong> {plans[currentPlan].price === 0 ? 'Free' : `$${plans[currentPlan].price}`}</p>
          <p><strong>Next Billing Date:</strong> {plans[currentPlan].price === 0 ? 'N/A' : 'January 15, 2025'}</p>
          
          {currentPlan > 0 && (
            <div style={{ marginTop: '20px' }}>
              <button className="btn btn-secondary">
                Cancel Subscription
              </button>
              <small style={{ display: 'block', marginTop: '10px', color: '#666' }}>
                You can cancel anytime. Your plan will remain active until the end of the billing period.
              </small>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default SubscriptionManagement;