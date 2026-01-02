import axios from 'axios';

const API_BASE_URL = 'https://localhost:7000/api';

export interface PlayerPrize {
  id: string;
  name: string;
  description: string;
  businessName: string;
  redemptionCode: string;
  expiresAt: string;
  isRedeemed: boolean;
  value?: number;
}

export interface TokenTransaction {
  id: string;
  type: 'earned' | 'spent' | 'purchased';
  amount: number;
  description: string;
  timestamp: string;
}

export interface WalletData {
  playerId: string;
  tokenBalance: number;
  prizes: PlayerPrize[];
  recentTransactions: TokenTransaction[];
}

class WalletService {
  async getWallet(): Promise<WalletData> {
    const response = await axios.get(`${API_BASE_URL}/player/wallet`);
    return response.data;
  }

  async getTokenBalance(): Promise<{ balance: number }> {
    const response = await axios.get(`${API_BASE_URL}/player/balance`);
    return response.data;
  }

  async getTransactionHistory(limit: number = 20): Promise<TokenTransaction[]> {
    const response = await axios.get(`${API_BASE_URL}/player/transactions`, {
      params: { limit }
    });
    return response.data.transactions;
  }

  async getPrizes(): Promise<PlayerPrize[]> {
    const response = await axios.get(`${API_BASE_URL}/player/prizes`);
    return response.data.prizes;
  }

  async redeemPrize(prizeId: string): Promise<{ success: boolean; message: string }> {
    const response = await axios.post(`${API_BASE_URL}/player/prizes/${prizeId}/redeem`);
    return response.data;
  }
}

export const walletService = new WalletService();