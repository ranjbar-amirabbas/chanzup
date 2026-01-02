import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import App from '../App';
import { authService } from '../services/authService';
import { businessService } from '../services/businessService';
import { gameService } from '../services/gameService';
import { walletService } from '../services/walletService';
import { locationService } from '../services/locationService';

// Mock all services
jest.mock('../services/authService');
jest.mock('../services/businessService');
jest.mock('../services/gameService');
jest.mock('../services/walletService');
jest.mock('../services/locationService');

// Mock navigator permissions
Object.defineProperty(global.navigator, 'permissions', {
  value: {
    query: jest.fn().mockResolvedValue({ state: 'granted' })
  },
  writable: true
});

// Mock geolocation
Object.defineProperty(global.navigator, 'geolocation', {
  value: {
    getCurrentPosition: jest.fn()
  },
  writable: true
});

describe('Client App Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (authService.isAuthenticated as jest.Mock).mockReturnValue(false);
  });

  describe('User Registration and Authentication Journey', () => {
    test('complete user registration flow', async () => {
      const mockAuthResponse = {
        playerId: 'player123',
        accessToken: 'token123',
        refreshToken: 'refresh123',
        expiresIn: 3600
      };

      (authService.register as jest.Mock).mockResolvedValue(mockAuthResponse);

      render(<App />);

      // Navigate to registration
      fireEvent.click(screen.getByText('Get Started'));
      
      expect(screen.getByText('Create Your Account')).toBeInTheDocument();

      // Fill registration form
      fireEvent.change(screen.getByLabelText('Email'), {
        target: { value: 'test@example.com' }
      });
      fireEvent.change(screen.getByLabelText('Password'), {
        target: { value: 'password123' }
      });
      fireEvent.change(screen.getByLabelText('First Name'), {
        target: { value: 'John' }
      });
      fireEvent.change(screen.getByLabelText('Last Name'), {
        target: { value: 'Doe' }
      });

      // Submit registration
      fireEvent.click(screen.getByText('Create Account'));

      await waitFor(() => {
        expect(authService.register).toHaveBeenCalledWith({
          email: 'test@example.com',
          password: 'password123',
          firstName: 'John',
          lastName: 'Doe',
          phone: ''
        });
      });
    });

    test('user login flow', async () => {
      const mockAuthResponse = {
        playerId: 'player123',
        accessToken: 'token123',
        refreshToken: 'refresh123',
        expiresIn: 3600
      };

      (authService.login as jest.Mock).mockResolvedValue(mockAuthResponse);

      render(<App />);

      // Navigate to login
      fireEvent.click(screen.getByText('Sign In'));
      
      expect(screen.getByText('Sign In')).toBeInTheDocument();

      // Fill login form
      fireEvent.change(screen.getByLabelText('Email'), {
        target: { value: 'test@example.com' }
      });
      fireEvent.change(screen.getByLabelText('Password'), {
        target: { value: 'password123' }
      });

      // Submit login
      fireEvent.click(screen.getByText('Sign In'));

      await waitFor(() => {
        expect(authService.login).toHaveBeenCalledWith({
          email: 'test@example.com',
          password: 'password123'
        });
      });
    });
  });

  describe('Business Discovery Journey', () => {
    test('authenticated user can discover nearby businesses', async () => {
      (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
      
      const mockLocation = { latitude: 49.2827, longitude: -123.1207 };
      const mockBusinesses = [
        {
          businessId: 'business1',
          name: 'Coffee Shop',
          address: '123 Main St',
          distance: 250,
          activeCampaigns: 1,
          location: mockLocation,
          campaigns: [{
            id: 'campaign1',
            name: 'Holiday Special',
            description: 'Win prizes!',
            tokenCostPerSpin: 5,
            topPrizes: ['Free Coffee']
          }]
        }
      ];

      (locationService.getCurrentLocation as jest.Mock).mockResolvedValue(mockLocation);
      (businessService.getNearbyBusinesses as jest.Mock).mockResolvedValue(mockBusinesses);

      // Navigate directly to nearby businesses
      window.history.pushState({}, 'Nearby', '/nearby');
      render(<App />);

      await waitFor(() => {
        expect(screen.getByText('Nearby Businesses')).toBeInTheDocument();
      });

      await waitFor(() => {
        expect(screen.getByText('Coffee Shop')).toBeInTheDocument();
        expect(screen.getByText('Holiday Special')).toBeInTheDocument();
      });

      expect(businessService.getNearbyBusinesses).toHaveBeenCalledWith({
        latitude: mockLocation.latitude,
        longitude: mockLocation.longitude,
        radius: 5000
      });
    });
  });

  describe('Game Mechanics Journey', () => {
    test('QR scanning and token earning flow', async () => {
      (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
      
      const mockLocation = { latitude: 49.2827, longitude: -123.1207 };
      const mockScanResult = {
        sessionId: 'session123',
        tokensEarned: 10,
        newBalance: 50,
        canSpin: true,
        campaign: {
          id: 'campaign123',
          name: 'Test Campaign',
          tokenCostPerSpin: 5,
          remainingSpinsToday: 3
        }
      };

      (locationService.getCurrentLocation as jest.Mock).mockResolvedValue(mockLocation);
      (gameService.scanQRCode as jest.Mock).mockResolvedValue(mockScanResult);

      // Navigate to QR scanner
      window.history.pushState({}, 'QR Scanner', '/qr-scanner');
      render(<App />);

      await waitFor(() => {
        expect(screen.getByText('QR Code Scanner')).toBeInTheDocument();
      });

      // Enter QR code manually
      const qrInput = screen.getByPlaceholderText('Enter QR code');
      fireEvent.change(qrInput, { target: { value: 'test-qr-code' } });
      fireEvent.click(screen.getByText('Scan'));

      await waitFor(() => {
        expect(screen.getByText('QR Code Scanned Successfully!')).toBeInTheDocument();
        expect(screen.getByText(/Tokens Earned.*10/)).toBeInTheDocument();
      });

      expect(gameService.scanQRCode).toHaveBeenCalledWith({
        qrCode: 'test-qr-code',
        location: mockLocation,
        timestamp: expect.any(String)
      });
    });

    test('wheel spinning flow', async () => {
      (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
      
      const mockSpinResult = {
        spinId: 'spin123',
        result: 'prize' as const,
        prize: {
          id: 'prize123',
          name: 'Free Coffee',
          description: 'One free coffee',
          redemptionCode: 'COFFEE123',
          expiresAt: '2024-12-31T23:59:59Z'
        },
        tokensSpent: 5,
        newBalance: 45,
        animation: {
          duration: 1000, // Shorter for tests
          finalAngle: 180,
          segments: []
        }
      };

      (gameService.getPlayerBalance as jest.Mock).mockResolvedValue({ balance: 50 });
      (gameService.spinWheel as jest.Mock).mockResolvedValue(mockSpinResult);

      // Navigate to wheel spin with state
      window.history.pushState({
        campaignId: 'campaign123',
        sessionId: 'session123',
        campaign: { id: 'campaign123', name: 'Test Campaign', tokenCostPerSpin: 5 }
      }, 'Wheel Spin', '/wheel-spin');
      
      render(<App />);

      await waitFor(() => {
        expect(screen.getByText('Wheel of Luck')).toBeInTheDocument();
      });

      // Click spin button
      const spinButton = screen.getByText('🎰 Spin the Wheel!');
      fireEvent.click(spinButton);

      // Wait for spin result (shorter timeout for test)
      await waitFor(() => {
        expect(screen.getByText('Congratulations!')).toBeInTheDocument();
        expect(screen.getByText('You won: Free Coffee')).toBeInTheDocument();
      }, { timeout: 2000 });

      expect(gameService.spinWheel).toHaveBeenCalledWith({
        campaignId: 'campaign123',
        sessionId: 'session123'
      });
    });
  });

  describe('Wallet and Prize Management Journey', () => {
    test('wallet displays prizes and transactions', async () => {
      (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
      
      const mockWalletData = {
        playerId: 'player123',
        tokenBalance: 45,
        prizes: [
          {
            id: 'prize1',
            name: 'Free Coffee',
            description: 'One free coffee',
            businessName: 'Coffee Shop',
            redemptionCode: 'COFFEE123',
            expiresAt: '2024-12-31T23:59:59Z',
            isRedeemed: false
          }
        ],
        recentTransactions: [
          {
            id: 'txn1',
            type: 'earned' as const,
            amount: 10,
            description: 'QR scan at Coffee Shop',
            timestamp: '2024-01-15T10:30:00Z'
          }
        ]
      };

      (walletService.getWallet as jest.Mock).mockResolvedValue(mockWalletData);

      // Navigate to wallet
      window.history.pushState({}, 'Wallet', '/wallet');
      render(<App />);

      await waitFor(() => {
        expect(screen.getByText('My Wallet')).toBeInTheDocument();
        expect(screen.getByText('🪙 45')).toBeInTheDocument();
      });

      await waitFor(() => {
        expect(screen.getByText('Free Coffee')).toBeInTheDocument();
        expect(screen.getByText('COFFEE123')).toBeInTheDocument();
      });

      // Switch to transactions tab
      fireEvent.click(screen.getByText(/Transactions/));

      await waitFor(() => {
        expect(screen.getByText('QR scan at Coffee Shop')).toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    test('handles authentication errors gracefully', async () => {
      (authService.login as jest.Mock).mockRejectedValue({
        response: { data: { error: { message: 'Invalid credentials' } } }
      });

      render(<App />);

      fireEvent.click(screen.getByText('Sign In'));
      
      fireEvent.change(screen.getByLabelText('Email'), {
        target: { value: 'wrong@example.com' }
      });
      fireEvent.change(screen.getByLabelText('Password'), {
        target: { value: 'wrongpassword' }
      });
      fireEvent.click(screen.getByText('Sign In'));

      await waitFor(() => {
        expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
      });
    });

    test('handles location permission denied', async () => {
      (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
      (locationService.getCurrentLocation as jest.Mock).mockRejectedValue(
        new Error('Location access denied by user')
      );

      window.history.pushState({}, 'Nearby', '/nearby');
      render(<App />);

      await waitFor(() => {
        expect(screen.getByText('Location access denied by user')).toBeInTheDocument();
      });
    });
  });

  describe('Navigation Flow', () => {
    test('protected routes redirect to login when not authenticated', () => {
      (authService.isAuthenticated as jest.Mock).mockReturnValue(false);

      // Try to access dashboard
      window.history.pushState({}, 'Dashboard', '/dashboard');
      render(<App />);

      // Should show login page
      expect(screen.getByText('Sign In')).toBeInTheDocument();
      expect(screen.getByLabelText('Email')).toBeInTheDocument();
    });

    test('authenticated users can access dashboard', () => {
      (authService.isAuthenticated as jest.Mock).mockReturnValue(true);

      window.history.pushState({}, 'Dashboard', '/dashboard');
      render(<App />);

      expect(screen.getByText('Player Dashboard')).toBeInTheDocument();
      expect(screen.getByText('Token Balance')).toBeInTheDocument();
    });
  });
});