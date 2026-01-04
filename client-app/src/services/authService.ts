import axios from 'axios';

const API_BASE_URL = 'https://localhost:8106/api';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone: string;
}

export interface AuthResponse {
  playerId: string;
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

class AuthService {
  private tokenRefreshPromise: Promise<string> | null = null;

  constructor() {
    this.setupAxiosInterceptors();
  }

  private setupAxiosInterceptors() {
    // Request interceptor to add auth token
    axios.interceptors.request.use(
      (config) => {
        const token = this.getAccessToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor to handle token refresh
    axios.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config;

        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true;

          try {
            const newToken = await this.refreshToken();
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            return axios(originalRequest);
          } catch (refreshError) {
            this.logout();
            window.location.href = '/login';
            return Promise.reject(refreshError);
          }
        }

        return Promise.reject(error);
      }
    );
  }

  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await axios.post(`${API_BASE_URL}/auth/login`, credentials);
    const authData = response.data;
    
    this.setTokens(authData.accessToken, authData.refreshToken);
    localStorage.setItem('playerId', authData.playerId);
    
    return authData;
  }

  async register(userData: RegisterRequest): Promise<AuthResponse> {
    const response = await axios.post(`${API_BASE_URL}/auth/register/player`, userData);
    const authData = response.data;
    
    this.setTokens(authData.accessToken, authData.refreshToken);
    localStorage.setItem('playerId', authData.playerId);
    
    return authData;
  }

  async refreshToken(): Promise<string> {
    if (this.tokenRefreshPromise) {
      return this.tokenRefreshPromise;
    }

    this.tokenRefreshPromise = this.performTokenRefresh();
    
    try {
      const newToken = await this.tokenRefreshPromise;
      return newToken;
    } finally {
      this.tokenRefreshPromise = null;
    }
  }

  private async performTokenRefresh(): Promise<string> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await axios.post(`${API_BASE_URL}/auth/refresh`, {
      refreshToken
    });

    const { accessToken, refreshToken: newRefreshToken } = response.data;
    this.setTokens(accessToken, newRefreshToken);
    
    return accessToken;
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('playerId');
    delete axios.defaults.headers.common['Authorization'];
  }

  getAccessToken(): string | null {
    return localStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  getPlayerId(): string | null {
    return localStorage.getItem('playerId');
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  private setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('token', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    axios.defaults.headers.common['Authorization'] = `Bearer ${accessToken}`;
  }

  // Decode JWT token to check expiration
  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Date.now() / 1000;
      return payload.exp < currentTime;
    } catch {
      return true;
    }
  }
}

export const authService = new AuthService();