import React, { useState } from 'react';
import axios from 'axios';

interface Prize {
  name: string;
  description: string;
  value: number;
  quantity: number;
  winProbability: number;
}

interface CampaignData {
  name: string;
  description: string;
  gameType: 'WheelOfLuck' | 'TreasureHunt';
  tokenCostPerSpin: number;
  maxSpinsPerDay: number;
  startDate: string;
  endDate: string;
  prizes: Prize[];
}

interface Props {
  onClose: () => void;
  onCampaignCreated: () => void;
}

const CampaignCreationWizard: React.FC<Props> = ({ onClose, onCampaignCreated }) => {
  const [currentStep, setCurrentStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [campaignData, setCampaignData] = useState<CampaignData>({
    name: '',
    description: '',
    gameType: 'WheelOfLuck',
    tokenCostPerSpin: 5,
    maxSpinsPerDay: 3,
    startDate: '',
    endDate: '',
    prizes: []
  });

  const [newPrize, setNewPrize] = useState<Prize>({
    name: '',
    description: '',
    value: 0,
    quantity: 1,
    winProbability: 0.1
  });

  const handleCampaignChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const value = e.target.type === 'number' ? parseFloat(e.target.value) : e.target.value;
    setCampaignData({
      ...campaignData,
      [e.target.name]: value
    });
  };

  const handlePrizeChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const value = e.target.type === 'number' ? parseFloat(e.target.value) : e.target.value;
    setNewPrize({
      ...newPrize,
      [e.target.name]: value
    });
  };

  const addPrize = () => {
    if (!newPrize.name || newPrize.quantity <= 0 || newPrize.winProbability <= 0) {
      setError('Please fill in all prize fields with valid values');
      return;
    }

    setCampaignData({
      ...campaignData,
      prizes: [...campaignData.prizes, { ...newPrize }]
    });

    setNewPrize({
      name: '',
      description: '',
      value: 0,
      quantity: 1,
      winProbability: 0.1
    });
    setError('');
  };

  const removePrize = (index: number) => {
    setCampaignData({
      ...campaignData,
      prizes: campaignData.prizes.filter((_, i) => i !== index)
    });
  };

  const validateStep = (step: number): boolean => {
    switch (step) {
      case 1:
        return !!(campaignData.name && campaignData.description && campaignData.gameType);
      case 2:
        return !!(campaignData.tokenCostPerSpin > 0 && campaignData.maxSpinsPerDay > 0 && 
                 campaignData.startDate);
      case 3:
        return campaignData.prizes.length > 0;
      default:
        return false;
    }
  };

  const nextStep = () => {
    if (validateStep(currentStep)) {
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

  const handleSubmit = async () => {
    setLoading(true);
    setError('');

    try {
      // TODO: Replace with actual API call
      const response = await axios.post('https://localhost:7000/api/campaigns', campaignData);
      
      onCampaignCreated();
      onClose();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to create campaign');
    } finally {
      setLoading(false);
    }
  };

  const renderStep1 = () => (
    <div>
      <h3>Campaign Information</h3>
      <div className="form-group">
        <label htmlFor="name">Campaign Name *</label>
        <input
          type="text"
          id="name"
          name="name"
          value={campaignData.name}
          onChange={handleCampaignChange}
          placeholder="Enter campaign name"
          required
        />
      </div>

      <div className="form-group">
        <label htmlFor="description">Description *</label>
        <textarea
          id="description"
          name="description"
          value={campaignData.description}
          onChange={handleCampaignChange}
          placeholder="Describe your campaign"
          rows={3}
          required
        />
      </div>

      <div className="form-group">
        <label htmlFor="gameType">Game Type *</label>
        <select
          id="gameType"
          name="gameType"
          value={campaignData.gameType}
          onChange={handleCampaignChange}
          required
        >
          <option value="WheelOfLuck">Wheel of Luck</option>
          <option value="TreasureHunt">Treasure Hunt</option>
        </select>
      </div>
    </div>
  );

  const renderStep2 = () => (
    <div>
      <h3>Game Settings</h3>
      <div className="form-group">
        <label htmlFor="tokenCostPerSpin">Token Cost Per Spin *</label>
        <input
          type="number"
          id="tokenCostPerSpin"
          name="tokenCostPerSpin"
          value={campaignData.tokenCostPerSpin}
          onChange={handleCampaignChange}
          min="1"
          required
        />
      </div>

      <div className="form-group">
        <label htmlFor="maxSpinsPerDay">Max Spins Per Day *</label>
        <input
          type="number"
          id="maxSpinsPerDay"
          name="maxSpinsPerDay"
          value={campaignData.maxSpinsPerDay}
          onChange={handleCampaignChange}
          min="1"
          required
        />
      </div>

      <div className="form-group">
        <label htmlFor="startDate">Start Date *</label>
        <input
          type="date"
          id="startDate"
          name="startDate"
          value={campaignData.startDate}
          onChange={handleCampaignChange}
          required
        />
      </div>

      <div className="form-group">
        <label htmlFor="endDate">End Date (Optional)</label>
        <input
          type="date"
          id="endDate"
          name="endDate"
          value={campaignData.endDate}
          onChange={handleCampaignChange}
        />
      </div>
    </div>
  );

  const renderStep3 = () => (
    <div>
      <h3>Prize Inventory</h3>
      
      <div className="prize-form card" style={{ marginBottom: '20px', backgroundColor: '#f8f9fa' }}>
        <h4>Add Prize</h4>
        <div className="grid grid-2">
          <div className="form-group">
            <label htmlFor="prizeName">Prize Name *</label>
            <input
              type="text"
              id="prizeName"
              name="name"
              value={newPrize.name}
              onChange={handlePrizeChange}
              placeholder="e.g., Free Coffee"
            />
          </div>

          <div className="form-group">
            <label htmlFor="prizeValue">Value ($)</label>
            <input
              type="number"
              id="prizeValue"
              name="value"
              value={newPrize.value}
              onChange={handlePrizeChange}
              min="0"
              step="0.01"
            />
          </div>

          <div className="form-group">
            <label htmlFor="prizeQuantity">Quantity *</label>
            <input
              type="number"
              id="prizeQuantity"
              name="quantity"
              value={newPrize.quantity}
              onChange={handlePrizeChange}
              min="1"
            />
          </div>

          <div className="form-group">
            <label htmlFor="winProbability">Win Probability (0.0-1.0) *</label>
            <input
              type="number"
              id="winProbability"
              name="winProbability"
              value={newPrize.winProbability}
              onChange={handlePrizeChange}
              min="0.01"
              max="1.0"
              step="0.01"
            />
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="prizeDescription">Description</label>
          <textarea
            id="prizeDescription"
            name="description"
            value={newPrize.description}
            onChange={handlePrizeChange}
            placeholder="Prize description"
            rows={2}
          />
        </div>

        <button type="button" onClick={addPrize} className="btn btn-primary">
          Add Prize
        </button>
      </div>

      <div className="prizes-list">
        <h4>Campaign Prizes ({campaignData.prizes.length})</h4>
        {campaignData.prizes.length > 0 ? (
          <div className="prizes-grid">
            {campaignData.prizes.map((prize, index) => (
              <div key={index} className="prize-item card">
                <div className="prize-header">
                  <h5>{prize.name}</h5>
                  <button 
                    onClick={() => removePrize(index)}
                    className="btn btn-danger"
                    style={{ padding: '5px 10px', fontSize: '12px' }}
                  >
                    Remove
                  </button>
                </div>
                <p>{prize.description}</p>
                <div className="prize-details">
                  <span>Value: ${prize.value}</span>
                  <span>Qty: {prize.quantity}</span>
                  <span>Prob: {(prize.winProbability * 100).toFixed(1)}%</span>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p>No prizes added yet. Add at least one prize to continue.</p>
        )}
      </div>
    </div>
  );

  return (
    <div className="modal-overlay">
      <div className="modal-content campaign-wizard">
        <div className="modal-header">
          <h2>Create New Campaign</h2>
          <button onClick={onClose} className="close-button">&times;</button>
        </div>

        <div className="step-indicator">
          <div className={`step ${currentStep >= 1 ? 'active' : ''}`}>1</div>
          <div className={`step ${currentStep >= 2 ? 'active' : ''}`}>2</div>
          <div className={`step ${currentStep >= 3 ? 'active' : ''}`}>3</div>
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        <div className="wizard-content">
          {currentStep === 1 && renderStep1()}
          {currentStep === 2 && renderStep2()}
          {currentStep === 3 && renderStep3()}
        </div>

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
            <button 
              type="button" 
              className="btn btn-success" 
              onClick={handleSubmit}
              disabled={loading || campaignData.prizes.length === 0}
            >
              {loading ? 'Creating Campaign...' : 'Create Campaign'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default CampaignCreationWizard;