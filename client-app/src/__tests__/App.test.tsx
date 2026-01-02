import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import App from '../App';
import { authService } from '../services/authService';

// Mock the auth service
jest.mock('../services/authService', () => ({
  authService: {
    isAuthenticated: jest.fn(),
    login: jest.fn(),
    register: jest.fn(),
    logout: jest.fn()
  }
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

describe('App Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (authService.isAuthenticated as jest.Mock).mockReturnValue(false);
  });

  test('renders home page by default', () => {
    render(<App />);
    
    expect(screen.getByText('Welcome to Chanzup')).toBeInTheDocument();
    expect(screen.getByText('Discover local businesses and win amazing prizes!')).toBeInTheDocument();
    expect(screen.getByText('Get Started')).toBeInTheDocument();
    expect(screen.getByText('Sign In')).toBeInTheDocument();
  });

  test('navigates to registration page', () => {
    render(<App />);
    
    const getStartedButton = screen.getByText('Get Started');
    fireEvent.click(getStartedButton);
    
    expect(screen.getByText('Create Your Account')).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
  });

  test('navigates to login page', () => {
    render(<App />);
    
    const signInButton = screen.getByText('Sign In');
    fireEvent.click(signInButton);
    
    expect(screen.getByText('Sign In')).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
  });

  test('shows dashboard when authenticated', () => {
    (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
    
    render(<App />);
    
    // Navigate to dashboard
    window.history.pushState({}, 'Dashboard', '/dashboard');
    
    render(<App />);
    
    expect(screen.getByText('Player Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Token Balance')).toBeInTheDocument();
    expect(screen.getByText('Nearby Businesses')).toBeInTheDocument();
    expect(screen.getByText('My Prizes')).toBeInTheDocument();
    expect(screen.getByText('QR Scanner')).toBeInTheDocument();
  });

  test('redirects to login when accessing protected route without authentication', () => {
    (authService.isAuthenticated as jest.Mock).mockReturnValue(false);
    
    // Try to access dashboard without authentication
    window.history.pushState({}, 'Dashboard', '/dashboard');
    
    render(<App />);
    
    // Should redirect to login
    expect(screen.getByText('Sign In')).toBeInTheDocument();
  });
});