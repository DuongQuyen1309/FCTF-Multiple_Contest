/**
 * Debug Component for Challenges
 * 
 * Sử dụng component này để debug vấn đề challenges không hiển thị
 * 
 * Cách sử dụng:
 * 1. Import component này vào App.tsx
 * 2. Thêm route: <Route path="/debug-challenges" element={<DebugChallenges />} />
 * 3. Truy cập: http://localhost:5173/debug-challenges
 */

import { useState } from 'react';
import { Box, Typography, Button, Paper, Divider } from '@mui/material';
import { authService } from '../services/authService';
import { challengeService } from '../services/challengeService';
import { fetchWithAuth } from '../services/api';
import { API_ENDPOINTS } from '../config/endpoints';

interface DebugInfo {
  timestamp: string;
  section: string;
  status: 'success' | 'error' | 'warning' | 'info';
  message: string;
  data?: any;
}

export function DebugChallenges() {
  const [logs, setLogs] = useState<DebugInfo[]>([]);
  const [loading, setLoading] = useState(false);

  const addLog = (section: string, status: DebugInfo['status'], message: string, data?: any) => {
    setLogs(prev => [...prev, {
      timestamp: new Date().toISOString(),
      section,
      status,
      message,
      data
    }]);
  };

  const clearLogs = () => {
    setLogs([]);
  };

  const runDiagnostics = async () => {
    clearLogs();
    setLoading(true);

    try {
      // 1. Check Token
      addLog('Token', 'info', 'Checking JWT token...');
      const token = authService.getToken();
      
      if (!token) {
        addLog('Token', 'error', 'No token found in localStorage');
        setLoading(false);
        return;
      }

      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        addLog('Token', 'success', 'Token decoded successfully', payload);
        
        const contestId = payload.contestId;
        const teamId = payload.teamId;
        const userId = payload.userId;
        
        addLog('Token', 'info', `ContestId: ${contestId}, TeamId: ${teamId}, UserId: ${userId}`);

        if (!contestId) {
          addLog('Token', 'warning', 'No contestId in token - user may not have selected a contest');
        }

        // 2. Check Contest Access
        addLog('Contest Access', 'info', 'Checking contest access...');
        try {
          const access = await challengeService.getContestAccess();
          addLog('Contest Access', 'success', `Access: ${access.canAccess}, Reason: ${access.reason}`, access);
        } catch (error: any) {
          addLog('Contest Access', 'error', `Failed to check contest access: ${error.message}`);
        }

        // 3. Get Categories
        addLog('Categories', 'info', 'Fetching categories...');
        try {
          const categories = await challengeService.getCategories();
          
          if (Array.isArray(categories) && categories.length > 0) {
            addLog('Categories', 'success', `Found ${categories.length} categories`, categories);
            
            // 4. Get Challenges for each category
            for (const category of categories) {
              addLog('Challenges', 'info', `Fetching challenges for category: ${category.topic_name}`);
              
              try {
                const challenges = await challengeService.getChallengesByTopic(category.topic_name);
                
                if (Array.isArray(challenges) && challenges.length > 0) {
                  addLog('Challenges', 'success', `Found ${challenges.length} challenges in ${category.topic_name}`, challenges);
                } else {
                  addLog('Challenges', 'warning', `No challenges found in ${category.topic_name}`);
                }
              } catch (error: any) {
                addLog('Challenges', 'error', `Failed to fetch challenges for ${category.topic_name}: ${error.message}`);
              }
            }
          } else {
            addLog('Categories', 'warning', 'No categories found - this means no challenges are available');
          }
        } catch (error: any) {
          addLog('Categories', 'error', `Failed to fetch categories: ${error.message}`);
        }

        // 5. Test Raw API Endpoints
        addLog('Raw API', 'info', 'Testing raw API endpoints...');
        
        try {
          const byTopicResponse = await fetchWithAuth(API_ENDPOINTS.CHALLENGES.BY_TOPIC);
          const byTopicData = await byTopicResponse.json();
          addLog('Raw API', byTopicResponse.ok ? 'success' : 'error', 
            `GET /api/Challenge/by-topic - Status: ${byTopicResponse.status}`, 
            byTopicData);
        } catch (error: any) {
          addLog('Raw API', 'error', `Failed to call by-topic endpoint: ${error.message}`);
        }

      } catch (error: any) {
        addLog('Token', 'error', `Failed to decode token: ${error.message}`);
      }

    } catch (error: any) {
      addLog('General', 'error', `Unexpected error: ${error.message}`);
    }

    setLoading(false);
  };

  const testSelectContest = async (contestId: number) => {
    addLog('Select Contest', 'info', `Attempting to select contest ${contestId}...`);
    
    try {
      await authService.selectContest(contestId);
      addLog('Select Contest', 'success', `Successfully selected contest ${contestId}`);
      
      // Verify token was updated
      const token = authService.getToken();
      if (token) {
        const payload = JSON.parse(atob(token.split('.')[1]));
        addLog('Select Contest', 'info', 'New token payload', payload);
      }
    } catch (error: any) {
      addLog('Select Contest', 'error', `Failed to select contest: ${error.message}`);
    }
  };

  const getStatusColor = (status: DebugInfo['status']) => {
    switch (status) {
      case 'success': return '#4caf50';
      case 'error': return '#f44336';
      case 'warning': return '#ff9800';
      case 'info': return '#2196f3';
      default: return '#757575';
    }
  };

  return (
    <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
      <Typography variant="h4" gutterBottom sx={{ fontFamily: 'monospace', color: '#ff6b35' }}>
        🔍 Challenges Debug Tool
      </Typography>
      
      <Box sx={{ mb: 3, display: 'flex', gap: 2 }}>
        <Button 
          variant="contained" 
          onClick={runDiagnostics}
          disabled={loading}
          sx={{ bgcolor: '#ff6b35', '&:hover': { bgcolor: '#e55a2b' } }}
        >
          {loading ? 'Running Diagnostics...' : 'Run Full Diagnostics'}
        </Button>
        
        <Button 
          variant="outlined" 
          onClick={clearLogs}
          disabled={loading}
        >
          Clear Logs
        </Button>

        <Button 
          variant="outlined" 
          onClick={() => testSelectContest(3)}
          disabled={loading}
        >
          Select Contest 3
        </Button>
      </Box>

      <Paper sx={{ p: 2, bgcolor: '#1e1e1e', color: '#fff', fontFamily: 'monospace', fontSize: '0.875rem' }}>
        <Typography variant="h6" gutterBottom sx={{ color: '#ff6b35' }}>
          Diagnostic Logs ({logs.length})
        </Typography>
        
        <Divider sx={{ my: 2, bgcolor: '#444' }} />
        
        {logs.length === 0 ? (
          <Typography sx={{ color: '#888' }}>
            No logs yet. Click "Run Full Diagnostics" to start.
          </Typography>
        ) : (
          <Box sx={{ maxHeight: '70vh', overflow: 'auto' }}>
            {logs.map((log, index) => (
              <Box key={index} sx={{ mb: 2, pb: 2, borderBottom: '1px solid #333' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                  <Box 
                    sx={{ 
                      width: 8, 
                      height: 8, 
                      borderRadius: '50%', 
                      bgcolor: getStatusColor(log.status) 
                    }} 
                  />
                  <Typography sx={{ color: '#888', fontSize: '0.75rem' }}>
                    {new Date(log.timestamp).toLocaleTimeString()}
                  </Typography>
                  <Typography sx={{ color: '#ff6b35', fontWeight: 'bold' }}>
                    [{log.section}]
                  </Typography>
                  <Typography sx={{ color: getStatusColor(log.status), textTransform: 'uppercase', fontSize: '0.75rem' }}>
                    {log.status}
                  </Typography>
                </Box>
                
                <Typography sx={{ color: '#fff', mb: 1 }}>
                  {log.message}
                </Typography>
                
                {log.data && (
                  <Box 
                    sx={{ 
                      bgcolor: '#0a0a0a', 
                      p: 1, 
                      borderRadius: 1, 
                      overflow: 'auto',
                      maxHeight: 300
                    }}
                  >
                    <pre style={{ margin: 0, fontSize: '0.75rem', color: '#4caf50' }}>
                      {JSON.stringify(log.data, null, 2)}
                    </pre>
                  </Box>
                )}
              </Box>
            ))}
          </Box>
        )}
      </Paper>

      <Box sx={{ mt: 3 }}>
        <Typography variant="h6" gutterBottom sx={{ fontFamily: 'monospace' }}>
          Quick Checks
        </Typography>
        
        <Paper sx={{ p: 2 }}>
          <Typography variant="body2" paragraph>
            <strong>Expected Flow:</strong>
          </Typography>
          <ol style={{ fontSize: '0.875rem' }}>
            <li>Token should contain contestId, teamId, and userId</li>
            <li>Contest access should return canAccess: true</li>
            <li>Categories endpoint should return array of categories</li>
            <li>Each category should have challenges (unless empty)</li>
          </ol>

          <Divider sx={{ my: 2 }} />

          <Typography variant="body2" paragraph>
            <strong>Common Issues:</strong>
          </Typography>
          <ul style={{ fontSize: '0.875rem' }}>
            <li><strong>No contestId in token:</strong> User hasn't selected a contest. Click "Select Contest 3".</li>
            <li><strong>Empty categories:</strong> No challenges in database for this contest, or all are hidden.</li>
            <li><strong>401 Unauthorized:</strong> Token expired or invalid. Try logging out and back in.</li>
            <li><strong>403 Forbidden:</strong> User not registered for this contest.</li>
          </ul>
        </Paper>
      </Box>
    </Box>
  );
}
