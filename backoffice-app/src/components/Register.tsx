import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';

interface BusinessRegistrationData {
  businessName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phone: string;
  address: string;
  subscriptionTier: number;
  businessType: string;
  website?: string;
  description?: string;
}

const Register: React.FC = () => {
  const [currentStep, setCurrentStep] = useState(1);
  const [formData, setFormData] = useState<BusinessRegistrationData>({
    businessName: '',
    email: '',
    password: '',
    confirmPassword: '',
    phone: '',
    address: '',
    subscriptionTier: 0,
    businessType: '',
    website: '',
    description: ''
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const value = e.target.type === 'number' ? parseInt(e.target.value) : e.target.value;
    setFormData({
      ...formData,
      [e.target.name]: value
    });
  };

  const validateStep = (step: number): boolean => {
    switch (step) {
      case 1:
        return !!(formData.businessName && formData.email && formData.password && formData.confirmPassword);
      case 2:
        return !!(formData.phone && formData.address && formData.businessType);
      case 3:
        return true; // Optional step
      default:
        return false;
    }
  };

  const nextStep = () => {
    if (validateStep(currentStep)) {
      if (currentStep === 1 && formData.password !== formData.confirmPassword) {
        setError('Passwords do not match');
        return;
      }
      setError('');
      setCurrentStep(currentStep + 1);
    } else {
      setError('Please fill in all required fields');
    }
  };

  const prevStep = () => {
    setCurrentStep(currentStep - 1);
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      setLoading(false);
      return;
    }

    try {
      const registrationData = {
        businessName: formData.businessName,
        email: formData.email,
        password: formData.password,
        phone: formData.phone,
        address: formData.address,
        subscriptionTier: formData.subscriptionTier,
        businessType: formData.businessType,
        website: formData.website,
        description: formData.description
      };

      const response = await axios.post('https://localhost:7000/api/auth/register/business', registrationData);
      
      // Store token and business info
      localStorage.setItem('token', response.data.accessToken);
      localStorage.setItem('businessId', response.data.businessId);
      localStorage.setItem('businessName', formData.businessName);
      localStorage.setItem('subscriptionTier', formData.subscriptionTier.toString());
      
      // Redirect to dashboard
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.error || 'Registration failed');
    } finally {
      setLoading(false);
    }
  };

  const renderStep1 = () => (
    <div>
      <h3>Account Information</h3>
      <div className="form-group">
        <label htmlFor="businessName">Business Name *</label>
        <input
          type="text"
          id="businessName"
          name="businessName"
          value={formData.businessName}
          onChange={handleChange}
          placeholder="Enter your business name"
          required
        />
      </div>
      
      <div className="form-group">
        <label htmlFor="email">Email Address *</label>
        <input
          type="email"
          id="email"
          name="email"
          value={formData.email}
          onChange={handleChange}
          placeholder="business@example.com"
          required
        />
      </div>
      
      <div className="form-group">
        <label htmlFor="password">Password *</label>
        <input
          type="password"
          id="password"
          name="password"
          value={formData.password}
          onChange={handleChange}
          placeholder="Create a secure password"
          required
        />
      </div>
      
      <div className="form-group">
        <label htmlFor="confirmPassword">Confirm Password *</label>
        <input
          type="password"
          id="confirmPassword"
          name="confirmPassword"
          value={formData.confirmPassword}
          onChange={handleChange}
          placeholder="Confirm your password"
          required
        />
      </div>
    </div>
  );

  const renderStep2 = () => (
    <div>
      <h3>Business Details</h3>
      <div className="form-group">
        <label htmlFor="phone">Phone Number *</label>
        <input
          type="tel"
          id="phone"
          name="phone"
          value={formData.phone}
          onChange={handleChange}
          placeholder="+1 (604) 555-0123"
          required
        />
      </div>
      
      <div className="form-group">
        <label htmlFor="address">Business Address *</label>
        <input
          type="text"
          id="address"
          name="address"
          value={formData.address}
          onChange={handleChange}
          placeholder="123 Main St, Vancouver, BC"
          required
        />
      </div>
      
      <div className="form-group">
        <label htmlFor="businessType">Business Type *</label>
        <select
          id="businessType"
          name="businessType"
          value={formData.businessType}
          onChange={handleChange}
          required
        >
          <option value="">Select business type</option>
          <option value="restaurant">Restaurant</option>
          <option value="retail">Retail Store</option>
          <option value="cafe">Cafe/Coffee Shop</option>
          <option value="salon">Salon/Spa</option>
          <option value="fitness">Fitness/Gym</option>
          <option value="entertainment">Entertainment</option>
          <option value="services">Professional Services</option>
          <option value="other">Other</option>
        </select>
      </div>
      
      <div className="form-group">
        <label htmlFor="website">Website (Optional)</label>
        <input
          type="url"
          id="website"
          name="website"
          value={formData.website}
          onChange={handleChange}
          placeholder="https://www.yourbusiness.com"
        />
      </div>
    </div>
  );

  const renderStep3 = () => (
    <div>
      <h3>Choose Your Plan</h3>
      <div className="subscription-plans">
        <div className={`plan-card ${formData.subscriptionTier === 0 ? 'selected' : ''}`} 
             onClick={() => setFormData({...formData, subscriptionTier: 0})}>
          <h4>Basic Plan</h4>
          <div className="price">Free</div>
          <ul>
            <li>Up to 2 active campaigns</li>
            <li>Basic analytics</li>
            <li>Standard support</li>
            <li>QR code generation</li>
          </ul>
        </div>
        
        <div className={`plan-card ${formData.subscriptionTier === 1 ? 'selected' : ''}`} 
             onClick={() => setFormData({...formData, subscriptionTier: 1})}>
          <h4>Premium Plan</h4>
          <div className="price">$29/month</div>
          <ul>
            <li>Unlimited campaigns</li>
            <li>Advanced analytics</li>
            <li>Priority support</li>
            <li>Multi-location support</li>
            <li>Custom branding</li>
          </ul>
        </div>
        
        <div className={`plan-card ${formData.subscriptionTier === 2 ? 'selected' : ''}`} 
             onClick={() => setFormData({...formData, subscriptionTier: 2})}>
          <h4>Enterprise Plan</h4>
          <div className="price">$99/month</div>
          <ul>
            <li>Everything in Premium</li>
            <li>API access</li>
            <li>White-label solution</li>
            <li>Dedicated support</li>
            <li>Custom integrations</li>
          </ul>
        </div>
      </div>
      
      <div className="form-group" style={{ marginTop: '30px' }}>
        <label htmlFor="description">Business Description (Optional)</label>
        <textarea
          id="description"
          name="description"
          value={formData.description}
          onChange={handleChange}
          placeholder="Tell us about your business..."
          rows={4}
        />
      </div>
    </div>
  );

  return (
    <div className="container" style={{ maxWidth: '600px' }}>
      <div className="card">
        <div className="registration-header">
          <h2>Register Your Business</h2>
          <div className="step-indicator">
            <div className={`step ${currentStep >= 1 ? 'active' : ''}`}>1</div>
            <div className={`step ${currentStep >= 2 ? 'active' : ''}`}>2</div>
            <div className={`step ${currentStep >= 3 ? 'active' : ''}`}>3</div>
          </div>
        </div>
        
        {error && (
          <div className="error-message">
            {error}
          </div>
        )}
        
        <form onSubmit={handleSubmit}>
          {currentStep === 1 && renderStep1()}
          {currentStep === 2 && renderStep2()}
          {currentStep === 3 && renderStep3()}
          
          <div className="form-actions">
            {currentStep > 1 && (
              <button type="button" className="btn btn-secondary" onClick={prevStep}>
                Previous
              </button>
            )}
            
            {currentStep < 3 ? (
              <button type="button" className="btn btn-primary" onClick={nextStep}>
                Next
              </button>
            ) : (
              <button type="submit" className="btn btn-success" disabled={loading}>
                {loading ? 'Creating Account...' : 'Complete Registration'}
              </button>
            )}
          </div>
        </form>
        
        <div className="login-link">
          Already have an account? <a href="/login">Sign in here</a>
        </div>
      </div>
    </div>
  );
};

export default Register;