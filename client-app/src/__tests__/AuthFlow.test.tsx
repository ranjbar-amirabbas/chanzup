import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import Register from '../components/Register';
import Login from '../components/Login';
import { authService } from '../services/authService';

// Mock the auth service
jest.mock('../services/authService', () => ({
  authService: {
    register: jest.fn(),
    login: jest.fn()
  }
}));

// Mock react-router-dom
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

describe('Authentication Flow Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Registration Flow', () => {
    test('successful registration redirects to dashboard', async () => {
      const mockAuthResponse = {
        playerId: 'player123',
        accessToken: 'token123',
        refreshToken: 'refresh123',
        expiresIn: 3600
      };

      (authService.register as jest.Mock).mockResolvedValue(mockAuthResponse);

      render(
        <BrowserRouter>
          <Register />
        </BrowserRouter>
      );

      // Fill out registration form
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
      fireEvent.change(screen.getByLabelText('Phone'), {
        target: { value: '+1234567890' }
      });

      // Submit form
      fireEvent.click(screen.getByText('Create Account'));

      await waitFor(() => {
        expect(authService.register).toHaveBeenCalledWith({
          email: 'test@example.com',
          password: 'password123',
          firstName: 'John',
          lastName: 'Doe',
          phone: '+1234567890'
        });
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
      });
    });

    test('registration error displays error message', async () => {
      const errorMessage = 'Email already exists';
      (authService.register as jest.Mock).mockRejectedValue({
        response: { data: { error: { message: errorMessage } } }
      });

      render(
        <BrowserRouter>
          <Register />
        </BrowserRouter>
      );

      // Fill out and submit form
      fireEvent.change(screen.getByLabelText('Email'), {
        target: { value: 'existing@example.com' }
      });
      fireEvent.change(screen.getByLabelText('Password'), {
        target: { value: 'password123' }
      });
      fireEvent.click(screen.getByText('Create Account'));

      await waitFor(() => {
        expect(screen.getByText(errorMessage)).toBeInTheDocument();
      });
    });
  });

  describe('Login Flow', () => {
    test('successful login redirects to dashboard', async () => {
      const mockAuthResponse = {
        playerId: 'player123',
        accessToken: 'token123',
        refreshToken: 'refresh123',
        expiresIn: 3600
      };

      (authService.login as jest.Mock).mockResolvedValue(mockAuthResponse);

      render(
        <BrowserRouter>
          <Login />
        </BrowserRouter>
      );

      // Fill out login form
      fireEvent.change(screen.getByLabelText('Email'), {
        target: { value: 'test@example.com' }
      });
      fireEvent.change(screen.getByLabelText('Password'), {
        target: { value: 'password123' }
      });

      // Submit form
      fireEvent.click(screen.getByText('Sign In'));

      await waitFor(() => {
        expect(authService.login).toHaveBeenCalledWith({
          email: 'test@example.com',
          password: 'password123'
        });
        expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
      });
    });

    test('login error displays error message', async () => {
      const errorMessage = 'Invalid credentials';
      (authService.login as jest.Mock).mockRejectedValue({
        response: { data: { error: { message: errorMessage } } }
      });

      render(
        <BrowserRouter>
          <Login />
        </BrowserRouter>
      );

      // Fill out and submit form
      fireEvent.change(screen.getByLabelText('Email'), {
        target: { value: 'wrong@example.com' }
      });
      fireEvent.change(screen.getByLabelText('Password'), {
        target: { value: 'wrongpassword' }
      });
      fireEvent.click(screen.getByText('Sign In'));

      await waitFor(() => {
        expect(screen.getByText(errorMessage)).toBeInTheDocument();
      });
    });
  });
});