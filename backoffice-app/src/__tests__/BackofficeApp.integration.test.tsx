import React from 'react';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import App from '../App';

// Mock axios for API calls
jest.mock('axios', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
    get: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
  }
}));

// Mock recharts to avoid rendering issues in tests
jest.mock('recharts', () => ({
  LineChart: () => <div data-testid="line-chart">LineChart</div>,
  Line: () => <div>Line</div>,
  XAxis: () => <div>XAxis</div>,
  YAxis: () => <div>YAxis</div>,
  CartesianGrid: () => <div>CartesianGrid</div>,
  Tooltip: () => <div>Tooltip</div>,
  Legend: () => <div>Legend</div>,
  ResponsiveContainer: ({ children }: any) => <div data-testid="responsive-container">{children}</div>,
  BarChart: () => <div data-testid="bar-chart">BarChart</div>,
  Bar: () => <div>Bar</div>,
  PieChart: () => <div data-testid="pie-chart">PieChart</div>,
  Pie: () => <div>Pie</div>,
  Cell: () => <div>Cell</div>,
}));

const renderWithRouter = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('Backoffice App Basic Tests', () => {
  beforeEach(() => {
    localStorage.clear();
    jest.clearAllMocks();
  });

  test('should render home page', () => {
    renderWithRouter(<App />);
    
    expect(screen.getByText('Welcome to Chanzup Business Portal')).toBeInTheDocument();
    expect(screen.getByText('Register Your Business')).toBeInTheDocument();
    expect(screen.getByText('Business Login')).toBeInTheDocument();
  });

  test('should navigate to registration page', () => {
    renderWithRouter(<App />);
    
    const registerButton = screen.getByText('Register Your Business');
    expect(registerButton).toBeInTheDocument();
    expect(registerButton.closest('a')).toHaveAttribute('href', '/register');
  });

  test('should navigate to login page', () => {
    renderWithRouter(<App />);
    
    const loginButton = screen.getByText('Business Login');
    expect(loginButton).toBeInTheDocument();
    expect(loginButton.closest('a')).toHaveAttribute('href', '/login');
  });
});