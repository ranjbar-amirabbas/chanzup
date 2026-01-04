import axios from 'axios';

const API_BASE_URL = 'https://localhost:8106/api';

export interface Business {
  businessId: string;
  name: string;
  address: string;
  distance: number;
  activeCampaigns: number;
  location: {
    latitude: number;
    longitude: number;
  };
  campaigns: Campaign[];
}

export interface Campaign {
  id: string;
  name: string;
  description: string;
  tokenCostPerSpin: number;
  topPrizes: string[];
}

export interface NearbyBusinessesRequest {
  latitude: number;
  longitude: number;
  radius?: number;
}

class BusinessService {
  async getNearbyBusinesses(params: NearbyBusinessesRequest): Promise<Business[]> {
    const response = await axios.get(`${API_BASE_URL}/player/nearby`, {
      params: {
        lat: params.latitude,
        lng: params.longitude,
        radius: params.radius || 5000
      }
    });
    
    return response.data.businesses;
  }

  async getBusinessDetails(businessId: string): Promise<Business> {
    const response = await axios.get(`${API_BASE_URL}/businesses/${businessId}`);
    return response.data;
  }

  async getCampaignDetails(campaignId: string): Promise<Campaign> {
    const response = await axios.get(`${API_BASE_URL}/campaigns/${campaignId}`);
    return response.data;
  }
}

export const businessService = new BusinessService();