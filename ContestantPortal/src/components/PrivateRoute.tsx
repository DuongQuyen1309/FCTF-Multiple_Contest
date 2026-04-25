import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { PageLoader } from './PageLoader';

interface PrivateRouteProps {
  children: React.ReactNode;
}

export function PrivateRoute({ children }: PrivateRouteProps) {
  const { isAuthenticated, loading } = useAuth();

  console.log('[PrivateRoute] Checking authentication...');
  console.log('[PrivateRoute] Loading:', loading);
  console.log('[PrivateRoute] isAuthenticated:', isAuthenticated);
  console.log('[PrivateRoute] Token in localStorage:', localStorage.getItem('auth_token') ? 'exists' : 'missing');

  if (loading) {
    console.log('[PrivateRoute] Still loading, showing PageLoader');
    return <PageLoader />;
  }

  if (!isAuthenticated) {
    console.warn('[PrivateRoute] NOT authenticated, redirecting to /login');
  } else {
    console.log('[PrivateRoute] Authenticated, rendering children');
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}