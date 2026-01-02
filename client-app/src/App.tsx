import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';
import Home from './components/Home';
import Register from './components/Register';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import NearbyBusinesses from './components/NearbyBusinesses';
import BusinessDetail from './components/BusinessDetail';
import QRScanner from './components/QRScanner';
import WheelSpin from './components/WheelSpin';
import Wallet from './components/Wallet';
import ProtectedRoute from './components/ProtectedRoute';

function App() {
  return (
    <Router>
      <div className="App">
        <header className="App-header">
          <nav>
            <h1>Chanzup</h1>
          </nav>
        </header>
        <main>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/register" element={<Register />} />
            <Route path="/login" element={<Login />} />
            <Route 
              path="/dashboard" 
              element={
                <ProtectedRoute>
                  <Dashboard />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/nearby" 
              element={
                <ProtectedRoute>
                  <NearbyBusinesses />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/business/:businessId" 
              element={
                <ProtectedRoute>
                  <BusinessDetail />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/qr-scanner" 
              element={
                <ProtectedRoute>
                  <QRScanner />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/wheel-spin" 
              element={
                <ProtectedRoute>
                  <WheelSpin />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/wallet" 
              element={
                <ProtectedRoute>
                  <Wallet />
                </ProtectedRoute>
              } 
            />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;