import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import NearbyBusinesses from '../components/NearbyBusinesses';
import BusinessDetail from '../components/BusinessDetail';
import { businessService } from '../services/businessService';
import { locationService } from '../services/locationService';

// Mock services
jest.mock('../services/businessService', () => ({
  businessService: {
    getNearbyBusinesses: jest.fn(),
    getBusinessDetails: jest.fn()
  }
}));

jest.mock('../services/locationService', () => ({
  locationService: {
    getCurrentLocation: jest.fn()
  }
}));

// Mock react-router-dom
const mockNavigate = jest.fn();
const mockParams = { businessId: 'business123' };

jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
  useParams: () => mockParams
}));

describe('Business Discovery Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Nearby Businesses Flow', () => {
    test('loads and displays nearby businesses', async () => {
      const mockLocation = { latitude: 49.2827, longitude: -123.1207 };
      const mockBusinesses = [
        {
          businessId: 'business1',
          name: 'Coffee Shop',
          address: '123 Main St, Vancouver, BC',
          distance: 250,
          activeCampaigns: 2,
          location: { latitude: 49.2827, longitude: -123.1207 },
          campaigns: [
            {
              id: 'campaign1',
              name: 'Holiday Special',
              description: 'Win holiday prizes!',
              tokenCostPerSpin: 5,
              topPrizes: ['Free Coffee', '10% Discount']
            }
          ]
        },
        {
          businessId: 'business2',
          name: 'Pizza Place',
          address: '456 Oak Ave, Vancouver, BC',
          distance: 500,
          activeCampaigns: 1,
          location: { latitude: 49.2830, longitude: -123.1210 },
          campaigns: [
            {
              id: 'campaign2',
              name: 'Pizza Wheel',
              description: 'Spin for pizza deals!',
              tokenCostPerSpin: 3,
              topPrizes: ['Free Slice', 'Free Drink']
            }
          ]
        }
      ];

      (locationService.getCurrentLocation as jest.Mock).mockResolvedValue(mockLocation);
      (businessService.getNearbyBusinesses as jest.Mock).mockResolvedValue(mockBusinesses);

      render(
        <BrowserRouter>
          <NearbyBusinesses />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Nearby Businesses')).toBeInTheDocument();
        expect(screen.getByText('Found 2 businesses within 5 km')).toBeInTheDocument();
        expect(screen.getByText('Coffee Shop')).toBeInTheDocument();
        expect(screen.getByText('Pizza Place')).toBeInTheDocument();
        expect(screen.getByText('📍 250m away')).toBeInTheDocument();
        expect(screen.getByText('🎯 2 active campaigns')).toBeInTheDocument();
      });

      expect(businessService.getNearbyBusinesses).toHaveBeenCalledWith({
        latitude: mockLocation.latitude,
        longitude: mockLocation.longitude,
        radius: 5000
      });
    });

    test('handles location permission denied', async () => {
      (locationService.getCurrentLocation as jest.Mock).mockRejectedValue(
        new Error('Location access denied by user')
      );

      render(
        <BrowserRouter>
          <NearbyBusinesses />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Location access denied by user')).toBeInTheDocument();
        expect(screen.getByText('Try Again')).toBeInTheDocument();
      });
    });

    test('filters businesses by active campaigns', async () => {
      const mockLocation = { latitude: 49.2827, longitude: -123.1207 };
      const mockBusinesses = [
        {
          businessId: 'business1',
          name: 'Active Business',
          address: '123 Main St',
          distance: 250,
          activeCampaigns: 1,
          location: { latitude: 49.2827, longitude: -123.1207 },
          campaigns: [{ id: 'campaign1', name: 'Test', description: 'Test', tokenCostPerSpin: 5, topPrizes: [] }]
        },
        {
          businessId: 'business2',
          name: 'Inactive Business',
          address: '456 Oak Ave',
          distance: 500,
          activeCampaigns: 0,
          location: { latitude: 49.2830, longitude: -123.1210 },
          campaigns: []
        }
      ];

      (locationService.getCurrentLocation as jest.Mock).mockResolvedValue(mockLocation);
      (businessService.getNearbyBusinesses as jest.Mock).mockResolvedValue(mockBusinesses);

      render(
        <BrowserRouter>
          <NearbyBusinesses />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Active Business')).toBeInTheDocument();
        expect(screen.getByText('Inactive Business')).toBeInTheDocument();
      });

      // Filter by active campaigns
      const filterSelect = screen.getByLabelText('Filter:');
      fireEvent.change(filterSelect, { target: { value: 'active' } });

      await waitFor(() => {
        expect(screen.getByText('Active Business')).toBeInTheDocument();
        expect(screen.queryByText('Inactive Business')).not.toBeInTheDocument();
        expect(screen.getByText('Found 1 businesses within 5 km')).toBeInTheDocument();
      });
    });

    test('navigates to business detail when business is clicked', async () => {
      const mockLocation = { latitude: 49.2827, longitude: -123.1207 };
      const mockBusinesses = [
        {
          businessId: 'business1',
          name: 'Coffee Shop',
          address: '123 Main St',
          distance: 250,
          activeCampaigns: 1,
          location: { latitude: 49.2827, longitude: -123.1207 },
          campaigns: []
        }
      ];

      (locationService.getCurrentLocation as jest.Mock).mockResolvedValue(mockLocation);
      (businessService.getNearbyBusinesses as jest.Mock).mockResolvedValue(mockBusinesses);

      render(
        <BrowserRouter>
          <NearbyBusinesses />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Coffee Shop')).toBeInTheDocument();
      });

      // Click on business
      fireEvent.click(screen.getByText('Coffee Shop'));

      expect(mockNavigate).toHaveBeenCalledWith('/business/business1');
    });
  });

  describe('Business Detail Flow', () => {
    test('loads and displays business details', async () => {
      const mockBusiness = {
        businessId: 'business123',
        name: 'Coffee Shop',
        address: '123 Main St, Vancouver, BC',
        distance: 250,
        activeCampaigns: 2,
        location: { latitude: 49.2827, longitude: -123.1207 },
        campaigns: [
          {
            id: 'campaign1',
            name: 'Holiday Special',
            description: 'Win amazing holiday prizes!',
            tokenCostPerSpin: 5,
            topPrizes: ['Free Coffee', '10% Discount', 'Free Pastry']
          },
          {
            id: 'campaign2',
            name: 'Daily Rewards',
            description: 'Daily spin rewards',
            tokenCostPerSpin: 3,
            topPrizes: ['Free Drink']
          }
        ]
      };

      (businessService.getBusinessDetails as jest.Mock).mockResolvedValue(mockBusiness);

      render(
        <BrowserRouter>
          <BusinessDetail />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Coffee Shop')).toBeInTheDocument();
        expect(screen.getByText('123 Main St, Vancouver, BC')).toBeInTheDocument();
        expect(screen.getByText('250m away')).toBeInTheDocument();
        expect(screen.getByText('2')).toBeInTheDocument(); // Active campaigns count
        expect(screen.getByText('Holiday Special')).toBeInTheDocument();
        expect(screen.getByText('Daily Rewards')).toBeInTheDocument();
        expect(screen.getByText('🪙 5 tokens per spin')).toBeInTheDocument();
        expect(screen.getByText('Free Coffee, 10% Discount, Free Pastry')).toBeInTheDocument();
      });

      expect(businessService.getBusinessDetails).toHaveBeenCalledWith('business123');
    });

    test('handles business not found', async () => {
      (businessService.getBusinessDetails as jest.Mock).mockRejectedValue(
        new Error('Business not found')
      );

      render(
        <BrowserRouter>
          <BusinessDetail />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Business not found')).toBeInTheDocument();
        expect(screen.getByText('Back to Businesses')).toBeInTheDocument();
      });
    });

    test('navigates to QR scanner when scan button is clicked', async () => {
      const mockBusiness = {
        businessId: 'business123',
        name: 'Coffee Shop',
        address: '123 Main St',
        distance: 250,
        activeCampaigns: 1,
        location: { latitude: 49.2827, longitude: -123.1207 },
        campaigns: []
      };

      (businessService.getBusinessDetails as jest.Mock).mockResolvedValue(mockBusiness);

      render(
        <BrowserRouter>
          <BusinessDetail />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('📱 Scan QR Code')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('📱 Scan QR Code'));

      expect(mockNavigate).toHaveBeenCalledWith('/qr-scanner', {
        state: { businessId: 'business123' }
      });
    });
  });
});