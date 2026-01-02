import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import QRScanner from '../components/QRScanner';
import WheelSpin from '../components/WheelSpin';
import { gameService } from '../services/gameService';
import { locationService } from '../services/locationService';

// Mock services
jest.mock('../services/gameService', () => ({
  gameService: {
    scanQRCode: jest.fn(),
    spinWheel: jest.fn(),
    getPlayerBalance: jest.fn(),
    generateDemoQRCode: jest.fn()
  }
}));

jest.mock('../services/locationService', () => ({
  locationService: {
    getCurrentLocation: jest.fn()
  }
}));

// Mock react-router-dom
const mockNavigate = jest.fn();
const mockLocation = {
  state: {
    campaignId: 'campaign123',
    sessionId: 'session123',
    campaign: {
      id: 'campaign123',
      name: 'Test Campaign',
      tokenCostPerSpin: 5
    }
  }
};

jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
  useLocation: () => mockLocation
}));

// Mock geolocation
const mockGeolocation = {
  getCurrentPosition: jest.fn(),
  watchPosition: jest.fn()
};

Object.defineProperty(global.navigator, 'geolocation', {
  value: mockGeolocation,
  writable: true
});

describe('Game Flow Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('QR Scanning Flow', () => {
    test('successful QR scan shows result and spin option', async () => {
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

      render(
        <BrowserRouter>
          <QRScanner />
        </BrowserRouter>
      );

      // Enter QR code manually
      const qrInput = screen.getByPlaceholderText('Enter QR code');
      fireEvent.change(qrInput, { target: { value: 'test-qr-code' } });
      fireEvent.click(screen.getByText('Scan'));

      await waitFor(() => {
        expect(screen.getByText('QR Code Scanned Successfully!')).toBeInTheDocument();
        expect(screen.getByText('Tokens Earned: +10')).toBeInTheDocument();
        expect(screen.getByText('New Balance: 50 tokens')).toBeInTheDocument();
        expect(screen.getByText('🎰 Spin the Wheel')).toBeInTheDocument();
      });

      expect(gameService.scanQRCode).toHaveBeenCalledWith({
        qrCode: 'test-qr-code',
        location: mockLocation,
        timestamp: expect.any(String)
      });
    });

    test('QR scan error displays error message', async () => {
      const errorMessage = 'Invalid QR code';
      (locationService.getCurrentLocation as jest.Mock).mockResolvedValue({
        latitude: 49.2827,
        longitude: -123.1207
      });
      (gameService.scanQRCode as jest.Mock).mockRejectedValue({
        response: { data: { error: { message: errorMessage } } }
      });

      render(
        <BrowserRouter>
          <QRScanner />
        </BrowserRouter>
      );

      // Enter invalid QR code
      const qrInput = screen.getByPlaceholderText('Enter QR code');
      fireEvent.change(qrInput, { target: { value: 'invalid-code' } });
      fireEvent.click(screen.getByText('Scan'));

      await waitFor(() => {
        expect(screen.getByText(errorMessage)).toBeInTheDocument();
      });
    });
  });

  describe('Wheel Spinning Flow', () => {
    test('successful wheel spin shows result', async () => {
      const mockSpinResult = {
        spinId: 'spin123',
        result: 'prize' as const,
        prize: {
          id: 'prize123',
          name: 'Free Coffee',
          description: 'One free regular coffee',
          redemptionCode: 'COFFEE123',
          expiresAt: '2024-12-31T23:59:59Z'
        },
        tokensSpent: 5,
        newBalance: 45,
        animation: {
          duration: 3000,
          finalAngle: 180,
          segments: []
        }
      };

      (gameService.getPlayerBalance as jest.Mock).mockResolvedValue({ balance: 50 });
      (gameService.spinWheel as jest.Mock).mockResolvedValue(mockSpinResult);

      render(
        <BrowserRouter>
          <WheelSpin />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Wheel of Luck')).toBeInTheDocument();
        expect(screen.getByText('Token Balance: 50 tokens')).toBeInTheDocument();
      });

      // Click spin button
      fireEvent.click(screen.getByText('🎰 Spin the Wheel!'));

      // Wait for spin animation and result
      await waitFor(() => {
        expect(screen.getByText('Congratulations!')).toBeInTheDocument();
        expect(screen.getByText('You won: Free Coffee')).toBeInTheDocument();
        expect(screen.getByText('Redemption Code: COFFEE123')).toBeInTheDocument();
      }, { timeout: 5000 });

      expect(gameService.spinWheel).toHaveBeenCalledWith({
        campaignId: 'campaign123',
        sessionId: 'session123'
      });
    });

    test('wheel spin with no prize shows appropriate message', async () => {
      const mockSpinResult = {
        spinId: 'spin123',
        result: 'no_prize' as const,
        tokensSpent: 5,
        newBalance: 45,
        animation: {
          duration: 3000,
          finalAngle: 90,
          segments: []
        }
      };

      (gameService.getPlayerBalance as jest.Mock).mockResolvedValue({ balance: 50 });
      (gameService.spinWheel as jest.Mock).mockResolvedValue(mockSpinResult);

      render(
        <BrowserRouter>
          <WheelSpin />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('🎰 Spin the Wheel!')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('🎰 Spin the Wheel!'));

      await waitFor(() => {
        expect(screen.getByText('Better luck next time!')).toBeInTheDocument();
        expect(screen.getByText('New balance: 45 tokens')).toBeInTheDocument();
      }, { timeout: 5000 });
    });

    test('insufficient tokens disables spin button', async () => {
      (gameService.getPlayerBalance as jest.Mock).mockResolvedValue({ balance: 2 });

      render(
        <BrowserRouter>
          <WheelSpin />
        </BrowserRouter>
      );

      await waitFor(() => {
        expect(screen.getByText('Token Balance: 2 tokens')).toBeInTheDocument();
        expect(screen.getByText('Not enough tokens to spin')).toBeInTheDocument();
        
        const spinButton = screen.getByText('🎰 Spin the Wheel!');
        expect(spinButton).toBeDisabled();
      });
    });
  });
});