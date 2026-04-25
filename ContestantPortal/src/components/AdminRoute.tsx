import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { PageLoader } from './PageLoader';

interface AdminRouteProps {
  children: React.ReactNode;
}

export function AdminRoute({ children }: AdminRouteProps) {
  const { isAuthenticated, user, loading } = useAuth();

  console.log('[AdminRoute] Checking admin access...');
  console.log('[AdminRoute] Loading:', loading);
  console.log('[AdminRoute] isAuthenticated:', isAuthenticated);
  console.log('[AdminRoute] User:', user);
  console.log('[AdminRoute] User type:', user?.type);

  if (loading) {
    console.log('[AdminRoute] Still loading, showing PageLoader');
    return <PageLoader />;
  }

  if (!isAuthenticated) {
    console.warn('[AdminRoute] NOT authenticated, redirecting to /login');
    return <Navigate to="/login" replace />;
  }

  const isAdmin = user?.type === 'admin';
  
  if (!isAdmin) {
    console.warn('[AdminRoute] User is NOT admin, redirecting to /contests');
    console.warn('[AdminRoute] User type:', user?.type);
    return <Navigate to="/contests" replace />;
  }

  console.log('[AdminRoute] Admin access granted, rendering children');
  return <>{children}</>;
}
