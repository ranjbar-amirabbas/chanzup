import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { gameService, QRScanResponse } from '../services/gameService';
import { locationService } from '../services/locationService';

const QRScanner: React.FC = () => {
  const [scanning, setScanning] = useState(false);
  const [scanResult, setScanResult] = useState<QRScanResponse | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [cameraPermission, setCameraPermission] = useState<'granted' | 'denied' | 'prompt'>('prompt');
  const [manualCode, setManualCode] = useState('');
  const videoRef = useRef<HTMLVideoElement>(null);
  const navigate = useNavigate();
  const location = useLocation();
  const businessId = location.state?.businessId;

  useEffect(() => {
    checkCameraPermission();
    return () => {
      stopCamera();
    };
  }, []);

  const checkCameraPermission = async () => {
    try {
      const result = await navigator.permissions.query({ name: 'camera' as PermissionName });
      setCameraPermission(result.state);
      
      result.addEventListener('change', () => {
        setCameraPermission(result.state);
      });
    } catch (error) {
      console.error('Error checking camera permission:', error);
    }
  };

  const startCamera = async () => {
    try {
      setError('');
      const stream = await navigator.mediaDevices.getUserMedia({ 
        video: { 
          facingMode: 'environment' // Use back camera if available
        } 
      });
      
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        setScanning(true);
        setCameraPermission('granted');
      }
    } catch (err: any) {
      setError('Unable to access camera. Please check permissions.');
      setCameraPermission('denied');
    }
  };

  const stopCamera = () => {
    if (videoRef.current && videoRef.current.srcObject) {
      const stream = videoRef.current.srcObject as MediaStream;
      stream.getTracks().forEach(track => track.stop());
      videoRef.current.srcObject = null;
    }
    setScanning(false);
  };

  const handleScanQR = async (qrCode: string) => {
    try {
      setLoading(true);
      setError('');

      // Get current location
      const userLocation = await locationService.getCurrentLocation();

      // Scan QR code
      const result = await gameService.scanQRCode({
        qrCode,
        location: userLocation,
        timestamp: new Date().toISOString()
      });

      setScanResult(result);
      stopCamera();
    } catch (err: any) {
      setError(err.response?.data?.error?.message || 'Failed to scan QR code');
    } finally {
      setLoading(false);
    }
  };

  const handleManualScan = () => {
    if (manualCode.trim()) {
      handleScanQR(manualCode.trim());
    }
  };

  const handleSpinWheel = () => {
    if (scanResult) {
      navigate('/wheel-spin', { 
        state: { 
          campaignId: scanResult.campaign.id,
          sessionId: scanResult.sessionId,
          campaign: scanResult.campaign
        } 
      });
    }
  };

  const handleDemoScan = () => {
    if (businessId) {
      const demoQRCode = gameService.generateDemoQRCode(businessId);
      handleScanQR(demoQRCode);
    }
  };

  if (scanResult) {
    return (
      <div className="container">
        <div style={{ textAlign: 'center', padding: '20px' }}>
          <div style={{ fontSize: '48px', marginBottom: '20px' }}>🎉</div>
          <h2>QR Code Scanned Successfully!</h2>
          
          <div style={{ 
            backgroundColor: '#d4edda', 
            border: '1px solid #c3e6cb',
            borderRadius: '8px',
            padding: '20px',
            margin: '20px 0',
            textAlign: 'left'
          }}>
            <h3 style={{ margin: '0 0 15px 0', color: '#155724' }}>
              Tokens Earned: +{scanResult.tokensEarned}
            </h3>
            <p style={{ margin: '0 0 10px 0' }}>
              <strong>New Balance:</strong> {scanResult.newBalance} tokens
            </p>
            <p style={{ margin: '0 0 10px 0' }}>
              <strong>Campaign:</strong> {scanResult.campaign.name}
            </p>
            <p style={{ margin: '0' }}>
              <strong>Remaining Spins Today:</strong> {scanResult.campaign.remainingSpinsToday}
            </p>
          </div>

          {scanResult.canSpin ? (
            <div>
              <p style={{ marginBottom: '20px' }}>
                You can now spin the wheel! Each spin costs {scanResult.campaign.tokenCostPerSpin} tokens.
              </p>
              <button 
                onClick={handleSpinWheel}
                className="btn btn-primary"
                style={{ marginRight: '10px' }}
              >
                🎰 Spin the Wheel
              </button>
            </div>
          ) : (
            <div>
              <p style={{ color: '#856404', marginBottom: '20px' }}>
                You don't have enough tokens to spin or have reached your daily limit.
              </p>
            </div>
          )}

          <button 
            onClick={() => navigate('/dashboard')}
            className="btn btn-secondary"
            style={{ marginTop: '10px' }}
          >
            Back to Dashboard
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ marginBottom: '20px' }}>
        <button 
          onClick={() => navigate(-1)} 
          style={{ 
            background: 'none', 
            border: 'none', 
            color: '#007bff', 
            cursor: 'pointer',
            fontSize: '16px'
          }}
        >
          ← Back
        </button>
        <h2 style={{ margin: '10px 0' }}>QR Code Scanner</h2>
        <p>Scan the QR code at the business location to earn tokens</p>
      </div>

      {error && (
        <div style={{ 
          color: '#721c24', 
          backgroundColor: '#f8d7da', 
          border: '1px solid #f5c6cb',
          borderRadius: '4px',
          padding: '10px', 
          marginBottom: '20px' 
        }}>
          {error}
        </div>
      )}

      {/* Camera Scanner */}
      <div style={{ marginBottom: '30px' }}>
        <h3>Camera Scanner</h3>
        
        {!scanning && cameraPermission !== 'denied' && (
          <div style={{ textAlign: 'center', marginBottom: '20px' }}>
            <button onClick={startCamera} className="btn btn-primary">
              📷 Start Camera
            </button>
          </div>
        )}

        {scanning && (
          <div style={{ position: 'relative', marginBottom: '20px' }}>
            <video
              ref={videoRef}
              autoPlay
              playsInline
              style={{
                width: '100%',
                maxWidth: '400px',
                height: '300px',
                objectFit: 'cover',
                border: '2px solid #007bff',
                borderRadius: '8px'
              }}
            />
            <div style={{
              position: 'absolute',
              top: '50%',
              left: '50%',
              transform: 'translate(-50%, -50%)',
              width: '200px',
              height: '200px',
              border: '2px solid #fff',
              borderRadius: '8px',
              pointerEvents: 'none'
            }} />
            <div style={{ textAlign: 'center', marginTop: '10px' }}>
              <button onClick={stopCamera} className="btn btn-secondary">
                Stop Camera
              </button>
            </div>
          </div>
        )}

        {cameraPermission === 'denied' && (
          <div style={{ 
            backgroundColor: '#fff3cd', 
            border: '1px solid #ffeaa7',
            borderRadius: '4px',
            padding: '15px',
            marginBottom: '20px'
          }}>
            <p style={{ margin: '0' }}>
              Camera access is required to scan QR codes. Please enable camera permissions in your browser settings.
            </p>
          </div>
        )}
      </div>

      {/* Manual Entry */}
      <div style={{ marginBottom: '30px' }}>
        <h3>Manual Entry</h3>
        <p>If you can't use the camera, enter the QR code manually:</p>
        
        <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
          <input
            type="text"
            value={manualCode}
            onChange={(e) => setManualCode(e.target.value)}
            placeholder="Enter QR code"
            style={{ flex: 1, padding: '10px', border: '1px solid #ddd', borderRadius: '4px' }}
          />
          <button 
            onClick={handleManualScan}
            disabled={!manualCode.trim() || loading}
            className="btn btn-primary"
          >
            {loading ? 'Scanning...' : 'Scan'}
          </button>
        </div>
      </div>

      {/* Demo Mode */}
      {businessId && (
        <div style={{ 
          backgroundColor: '#e3f2fd', 
          border: '1px solid #bbdefb',
          borderRadius: '8px',
          padding: '20px',
          marginBottom: '20px'
        }}>
          <h3 style={{ margin: '0 0 10px 0' }}>Demo Mode</h3>
          <p style={{ margin: '0 0 15px 0' }}>
            For testing purposes, you can simulate scanning a QR code at this business:
          </p>
          <button onClick={handleDemoScan} className="btn btn-secondary">
            🧪 Demo Scan
          </button>
        </div>
      )}

      <div style={{ 
        backgroundColor: '#f8f9fa', 
        borderRadius: '8px',
        padding: '20px',
        marginTop: '20px'
      }}>
        <h4 style={{ margin: '0 0 10px 0' }}>How to scan:</h4>
        <ol style={{ margin: '0', paddingLeft: '20px' }}>
          <li>Visit a participating business location</li>
          <li>Find the Chanzup QR code displayed at the business</li>
          <li>Use the camera scanner or enter the code manually</li>
          <li>Earn tokens and spin the wheel to win prizes!</li>
        </ol>
      </div>
    </div>
  );
};

export default QRScanner;