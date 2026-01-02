import axios from 'axios';
import { Location } from './locationService';

const API_BASE_URL = 'https://localhost:7000/api';

export interface QRScanRequest {
  qrCode: string;
  location: Location;
  timestamp: string;
}

export interface QRScanResponse {
  sessionId: string;
  tokensEarned: number;
  newBalance: number;
  canSpin: boolean;
  campaign: {
    id: string;
    name: string;
    tokenCostPerSpin: number;
    remainingSpinsToday: number;
  };
}

export interface WheelSpinRequest {
  campaignId: string;
  sessionId: string;
}

export interface Prize {
  id: string;
  name: string;
  description: string;
  redemptionCode: string;
  expiresAt: string;
}

export interface WheelSpinResponse {
  spinId: string;
  result: 'prize' | 'no_prize';
  prize?: Prize;
  tokensSpent: number;
  newBalance: number;
  animation: {
    duration: number;
    finalAngle: number;
    segments: WheelSegment[];
  };
}

export interface WheelSegment {
  id: string;
  label: string;
  color: string;
  probability: number;
  startAngle: number;
  endAngle: number;
}

class GameService {
  async scanQRCode(request: QRScanRequest): Promise<QRScanResponse> {
    const response = await axios.post(`${API_BASE_URL}/qr/scan`, request);
    return response.data;
  }

  async spinWheel(request: WheelSpinRequest): Promise<WheelSpinResponse> {
    const response = await axios.post(`${API_BASE_URL}/wheel/spin`, request);
    return response.data;
  }

  async getPlayerBalance(): Promise<{ balance: number }> {
    const response = await axios.get(`${API_BASE_URL}/player/balance`);
    return response.data;
  }

  // Simulate QR code for demo purposes
  generateDemoQRCode(businessId: string): string {
    return `cmp_${businessId}_demo`;
  }

  // Parse QR code to extract campaign/business info
  parseQRCode(qrCode: string): { campaignId?: string; businessId?: string } {
    try {
      // Expected format: cmp_businessId_campaignId or similar
      const parts = qrCode.split('_');
      if (parts.length >= 2) {
        return {
          campaignId: parts[0],
          businessId: parts[1]
        };
      }
    } catch (error) {
      console.error('Error parsing QR code:', error);
    }
    return {};
  }
}

export const gameService = new GameService();