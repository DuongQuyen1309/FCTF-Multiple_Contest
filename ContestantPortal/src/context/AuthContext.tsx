import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { authService } from '../services/authService';
import type { User } from '../models/user.model';

interface AuthContextType {
  isAuthenticated: boolean;
  user: User | null;
  login: (username: string, password: string, captchaToken?: string) => Promise<void>;
  selectContest: (contestId: number) => Promise<void>;
  logout: () => Promise<void>;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    console.log('[AuthContext] Initializing auth state...');
    const token = authService.getToken();
    const userData = authService.getUser();
    
    console.log('[AuthContext] Token exists:', !!token);
    console.log('[AuthContext] User data exists:', !!userData);
    
    if (token && userData) {
      setIsAuthenticated(true);
      setUser(userData);
      console.log('[AuthContext] User authenticated:', userData.username);
    } else {
      console.log('[AuthContext] No valid session found');
    }
    setLoading(false);
  }, []);

  const login = async (username: string, password: string, captchaToken?: string) => {
    console.log('[AuthContext] Login attempt for user:', username);
    const response = await authService.login({ username, password, captchaToken });
    
    console.log('[AuthContext] Login response received:', response);
    
    // Double-check localStorage was updated
    const savedToken = authService.getToken();
    const savedUser = authService.getUser();
    
    console.log('[AuthContext] Token saved:', !!savedToken);
    console.log('[AuthContext] User saved:', !!savedUser);
    
    if (!savedToken || !savedUser) {
      console.error('[AuthContext] Failed to save auth data to localStorage');
      throw new Error('Failed to save authentication data');
    }
    
    // Update state after confirming localStorage is set
    setIsAuthenticated(true);
    setUser(response.user);
    console.log('[AuthContext] Auth state updated successfully');
  };

  const selectContest = async (contestId: number) => {
    console.log('[AuthContext] selectContest called with contestId:', contestId);
    console.log('[AuthContext] Token before API call:', !!authService.getToken());
    
    try {
      const data = await authService.selectContest(contestId);
      console.log('[AuthContext] selectContest API response:', data);
      
      // Update user with team info from selected contest
      if (user) {
        const updatedUser = {
          ...user,
          team: data.teamId ? { id: data.teamId, teamName: data.teamName || '' } : null,
        };
        console.log('[AuthContext] Updating user with team info:', updatedUser);
        setUser(updatedUser);
      }
      
      console.log('[AuthContext] Token after API call:', !!authService.getToken());
    } catch (error) {
      console.error('[AuthContext] selectContest error:', error);
      throw error;
    }
  };

  const logout = async () => {
    await authService.logout();
    setIsAuthenticated(false);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, login, selectContest, logout, loading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
