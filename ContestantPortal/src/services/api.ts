import { authService } from './authService';

export const API_BASE_URL = window?.__ENV__?.VITE_API_BASE_URL || import.meta.env.VITE_API_BASE_URL;
export async function fetchWithAuth(url: string, options: RequestInit = {}, API = API_BASE_URL) {
  const token = authService.getToken();
  
  console.log('[API] fetchWithAuth called');
  console.log('[API] URL:', url);
  console.log('[API] Token:', token ? `${token.substring(0, 50)}...` : 'MISSING');
  
  const headers = {
    'Content-Type': 'application/json',
    ...(token && { Authorization: `Bearer ${token}` }),
    ...options.headers,
  };

  const response = await fetch(`${API}${url}`, {
    ...options,
    headers,
  });

  console.log('[API] Response status:', response.status);

  // On 401, clear local session only (token is already invalid/expired).
  // Let components handle 403 (Forbidden - valid token but insufficient permissions).
  if (response.status === 401) {
    console.error('[API] 401 Unauthorized - Clearing session and redirecting to login');
    console.error('[API] Failed URL:', url);
    console.error('[API] Token that failed:', token ? `${token.substring(0, 50)}...` : 'MISSING');
    
    // Only redirect if we're not already on the login page
    const currentPath = window.location.pathname;
    if (currentPath !== '/login' && currentPath !== '/register') {
      authService.clearSession();
      window.location.href = '/login';
    }
  }

  return response;
}

export async function fetchData(url: string, options: RequestInit = {}, API = API_BASE_URL) {
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  return fetch(`${API}${url}`, {
    ...options,
    headers,
  });
}

export async function downloadFile(url: string): Promise<Blob> {
  const token = authService.getToken();
  
  const headers: HeadersInit = {
    ...(token && { Authorization: `Bearer ${token}` }),
  };

  const response = await fetch(`${API_BASE_URL}${url}`, {
    method: 'GET',
    headers,
  });

  // On 401, clear local session only (token is already invalid/expired).
  if (response.status === 401) {
    // Only redirect if we're not already on the login page
    const currentPath = window.location.pathname;
    if (currentPath !== '/login' && currentPath !== '/register') {
      authService.clearSession();
      window.location.href = '/login';
    }
  }

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  return response.blob();
}