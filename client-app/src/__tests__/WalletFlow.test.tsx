import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import Wallet from '../components/Wallet';
import { walletService } from '../services/walletService';

// Mock wallet service
jest.mock('../services/walletService', () => ({
  walletService: {
    getWallet: jest.fn(),
    redeemPrize: jest.fn()
  }
}));

// Mock react-router-dom
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

describe('Wallet Flow Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('loads and displays wallet data', async () => {
    const mockWalletData = {
      playerId: 'player123',
      tokenBalance: 45,
      prizes: [
        {
          id: 'prize1',
          name: 'Free Coffee',
          description: 'One free regular coffee',
          businessName: 'Coffee Shop',
          redemptionCode: 'COFFEE123',
          expiresAt: '2024-12-31T23:59:59Z',
          isRedeemed: false,
          value: 5
        },
        {
          id: 'prize2',
          name: '10% Discount',
          description: '10% off your next purchase',
          businessName: 'Pizza Place',
          redemptionCode: 'DISCOUNT456',
          expiresAt: '2024-01-01T00:00:00Z', // Expired
          isRedeemed: false,
          value: 0
        },
        {
          id: 'prize3',
          name: 'Free Slice',
          description: 'One free pizza slice',
          businessName: 'Pizza Place',
          redemptionCode: 'SLICE789',
          expiresAt: '2024-12-31T23:59:59Z',
          isRedeemed: true,
          value: 8
        }
      ],
      recentTransactions: [
        {
          id: 'txn1',
          type: 'earned' as const,
          amount: 10,
          description: 'QR scan at Coffee Shop',
          timestamp: '2024-01-15T10:30:00Z'
        },
        {
          id: 'txn2',
          type: 'spent' as const,
          amount: 5,
          description: 'Wheel spin at Coffee Shop',
          timestamp: '2024-01-15T10:35:00Z'
        }
      ]
    };

    (walletService.getWallet as jest.Mock).mockResolvedValue(mockWalletData);

    render(
      <BrowserRouter>
        <Wallet />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('My Wallet')).toBeInTheDocument();
      expect(screen.getByText('🪙 45')).toBeInTheDocument();
      expect(screen.getByText('My Prizes (3)')).toBeInTheDocument();
      expect(screen.getByText('Transactions (2)')).toBeInTheDocument();
    });

    // Check active prizes
    expect(screen.getByText('🏆 Active Prizes (1)')).toBeInTheDocument();
    expect(screen.getByText('Free Coffee')).toBeInTheDocument();
    expect(screen.getByText('COFFEE123')).toBeInTheDocument();

    // Check redeemed prizes
    expect(screen.getByText('✅ Redeemed Prizes (1)')).toBeInTheDocument();
    expect(screen.getByText('Free Slice')).toBeInTheDocument();

    // Check expired prizes
    expect(screen.getByText('⏰ Expired Prizes (1)')).toBeInTheDocument();
    expect(screen.getByText('10% Discount')).toBeInTheDocument();
  });

  test('switches between prizes and transactions tabs', async () => {
    const mockWalletData = {
      playerId: 'player123',
      tokenBalance: 45,
      prizes: [],
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

    render(
      <BrowserRouter>
        <Wallet />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('My Wallet')).toBeInTheDocument();
    });

    // Switch to transactions tab
    fireEvent.click(screen.getByText('Transactions (1)'));

    await waitFor(() => {
      expect(screen.getByText('Recent Transactions')).toBeInTheDocument();
      expect(screen.getByText('QR scan at Coffee Shop')).toBeInTheDocument();
      expect(screen.getByText('+10 🪙')).toBeInTheDocument();
    });

    // Switch back to prizes tab
    fireEvent.click(screen.getByText('My Prizes (0)'));

    await waitFor(() => {
      expect(screen.getByText('No prizes yet')).toBeInTheDocument();
      expect(screen.getByText('Visit businesses, scan QR codes, and spin the wheel to win prizes!')).toBeInTheDocument();
    });
  });

  test('handles empty wallet state', async () => {
    const mockWalletData = {
      playerId: 'player123',
      tokenBalance: 0,
      prizes: [],
      recentTransactions: []
    };

    (walletService.getWallet as jest.Mock).mockResolvedValue(mockWalletData);

    render(
      <BrowserRouter>
        <Wallet />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('🪙 0')).toBeInTheDocument();
      expect(screen.getByText('No prizes yet')).toBeInTheDocument();
    });

    // Switch to transactions tab
    fireEvent.click(screen.getByText('Transactions (0)'));

    await waitFor(() => {
      expect(screen.getByText('No transactions yet')).toBeInTheDocument();
      expect(screen.getByText('Your token earning and spending history will appear here')).toBeInTheDocument();
    });
  });

  test('handles wallet loading error', async () => {
    (walletService.getWallet as jest.Mock).mockRejectedValue(
      new Error('Failed to load wallet')
    );

    render(
      <BrowserRouter>
        <Wallet />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Failed to load wallet')).toBeInTheDocument();
      expect(screen.getByText('Try Again')).toBeInTheDocument();
    });
  });

  test('navigates to find businesses when no prizes', async () => {
    const mockWalletData = {
      playerId: 'player123',
      tokenBalance: 10,
      prizes: [],
      recentTransactions: []
    };

    (walletService.getWallet as jest.Mock).mockResolvedValue(mockWalletData);

    render(
      <BrowserRouter>
        <Wallet />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Find Businesses')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Find Businesses'));

    expect(mockNavigate).toHaveBeenCalledWith('/nearby');
  });

  test('quick actions navigation works', async () => {
    const mockWalletData = {
      playerId: 'player123',
      tokenBalance: 25,
      prizes: [],
      recentTransactions: []
    };

    (walletService.getWallet as jest.Mock).mockResolvedValue(mockWalletData);

    render(
      <BrowserRouter>
        <Wallet />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Quick Actions')).toBeInTheDocument();
    });

    // Test navigation buttons
    const quickActionButtons = screen.getAllByText('Find Businesses');
    fireEvent.click(quickActionButtons[1]); // The one in quick actions
    expect(mockNavigate).toHaveBeenCalledWith('/nearby');

    fireEvent.click(screen.getByText('Scan QR Code'));
    expect(mockNavigate).toHaveBeenCalledWith('/qr-scanner');

    fireEvent.click(screen.getByText('Dashboard'));
    expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
  });

  test('displays prize expiration warnings correctly', async () => {
    const now = new Date();
    const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);
    const nextWeek = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);

    const mockWalletData = {
      playerId: 'player123',
      tokenBalance: 30,
      prizes: [
        {
          id: 'prize1',
          name: 'Expiring Soon',
          description: 'Expires tomorrow',
          businessName: 'Coffee Shop',
          redemptionCode: 'EXPIRE123',
          expiresAt: tomorrow.toISOString(),
          isRedeemed: false
        },
        {
          id: 'prize2',
          name: 'Valid Prize',
          description: 'Expires next week',
          businessName: 'Pizza Place',
          redemptionCode: 'VALID456',
          expiresAt: nextWeek.toISOString(),
          isRedeemed: false
        }
      ],
      recentTransactions: []
    };

    (walletService.getWallet as jest.Mock).mockResolvedValue(mockWalletData);

    render(
      <BrowserRouter>
        <Wallet />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Expiring Soon')).toBeInTheDocument();
      expect(screen.getByText('Valid Prize')).toBeInTheDocument();
      expect(screen.getByText('Expires in 1 day')).toBeInTheDocument();
    });
  });
});