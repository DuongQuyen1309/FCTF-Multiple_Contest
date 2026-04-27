import { API_ENDPOINTS } from '../config/endpoints';
import type { User, Team } from '../models/user.model';
import type { LoginCredentials, AuthResponse } from '../models/auth.model';
import type {
  RegisterContestantPayload,
  RegistrationFieldDefinition,
  RegistrationMetadata,
} from '../models/registration.model';
import { API_BASE_URL } from './api';
class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'user_info';
  private readonly AUTH_REQUEST_TIMEOUT_MS = 15000;

  private async fetchWithTimeout(
    input: RequestInfo | URL,
    init: RequestInit,
    timeoutMs = this.AUTH_REQUEST_TIMEOUT_MS
  ): Promise<Response> {
    const controller = new AbortController();
    const timeoutId = window.setTimeout(() => {
      controller.abort();
    }, timeoutMs);

    try {
      return await fetch(input, {
        ...init,
        signal: controller.signal,
      });
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw new Error('Request timed out. Please try again.');
      }

      throw error;
    } finally {
      window.clearTimeout(timeoutId);
    }
  }

  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    console.log('[AuthService] Login request to:', `${API_BASE_URL}${API_ENDPOINTS.AUTH.LOGIN}`);
    console.log('[AuthService] Credentials:', { username: credentials.username, hasCaptcha: !!credentials.captchaToken });
    
    const response = await this.fetchWithTimeout(`${API_BASE_URL}${API_ENDPOINTS.AUTH.LOGIN}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credentials),
    });
    
    console.log('[AuthService] Response status:', response.status);
    
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      console.error('[AuthService] Login failed:', errorData);
      throw new Error(errorData.message || 'Login failed');
    }

    const data: AuthResponse = await response.json();
    console.log('[AuthService] Login response data:', data);
    console.log('[AuthService] Token from response:', data.generatedToken ? 'exists' : 'MISSING');
    console.log('[AuthService] User from response:', data.user ? 'exists' : 'MISSING');
    
    if (!data.generatedToken) {
      console.error('[AuthService] ERROR: No token in response!');
      throw new Error('No authentication token received from server');
    }
    
    if (!data.user) {
      console.error('[AuthService] ERROR: No user data in response!');
      throw new Error('No user data received from server');
    }
    
    this.setToken(data.generatedToken);
    this.setUser(data.user);
    
    console.log('[AuthService] Token saved to localStorage');
    console.log('[AuthService] User saved to localStorage');
    
    return data;
  }

  async selectContest(contestId: number): Promise<{ token: string; contestId: number; contestName: string; teamId?: number; teamName?: string }> {
    console.log('[AuthService] selectContest called with contestId:', contestId);
    console.log('[AuthService] Current token:', this.getToken() ? 'exists' : 'missing');
    
    const response = await this.fetchWithTimeout(`${API_BASE_URL}${API_ENDPOINTS.AUTH.SELECT_CONTEST}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${this.getToken()}`,
      },
      body: JSON.stringify({ contestId }),
    });

    console.log('[AuthService] selectContest response status:', response.status);

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      console.error('[AuthService] selectContest failed:', errorData);
      throw new Error(errorData.message || 'Failed to select contest');
    }

    const result = await response.json();
    console.log('[AuthService] selectContest result:', result);
    
    const data = result.data;
    
    // Update token and user info with contest context
    console.log('[AuthService] Updating token with new contest token');
    this.setToken(data.token);
    
    const currentUser = this.getUser();
    if (currentUser) {
      console.log('[AuthService] Updating user info with team data');
      this.setUser({
        ...currentUser,
        team: data.teamId ? { id: data.teamId, teamName: data.teamName || '' } : null,
      });
    }
    
    console.log('[AuthService] Token after update:', this.getToken() ? 'exists' : 'missing');
    console.log('[AuthService] User after update:', this.getUser() ? 'exists' : 'missing');
    
    return data;
  }

  async getRegistrationMetadata(): Promise<RegistrationMetadata> {
    const response = await this.fetchWithTimeout(`${API_BASE_URL}${API_ENDPOINTS.AUTH.REGISTRATION_METADATA}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || 'Unable to load registration metadata');
    }

    const data = await response.json();
    const payload = data?.data ?? {};

    const normalizeFields = (fields: unknown): RegistrationFieldDefinition[] => {
      if (!Array.isArray(fields)) {
        return [];
      }

      return fields
        .filter((field): field is Record<string, unknown> => Boolean(field) && typeof field === 'object')
        .map((field) => {
          const fieldType: RegistrationFieldDefinition['fieldType'] = field.fieldType === 'boolean' ? 'boolean' : 'text';

          return {
            id: typeof field.id === 'number' ? field.id : 0,
            name: typeof field.name === 'string' ? field.name : '',
            fieldType,
            description: typeof field.description === 'string' ? field.description : undefined,
            required: field.required === true,
          };
        })
        .filter((field) => field.id > 0);
    };

    const constraintsRaw = (payload.constraints && typeof payload.constraints === 'object') ? payload.constraints as Record<string, unknown> : {};

    return {
      userFields: normalizeFields(payload.userFields),
      constraints: {
        numUsersLimit: Number(constraintsRaw.numUsersLimit ?? 0),
      },
    };
  }

  async registerContestant(payload: RegisterContestantPayload): Promise<void> {
    const response = await this.fetchWithTimeout(`${API_BASE_URL}${API_ENDPOINTS.AUTH.REGISTER}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || 'Registration failed');
    }
  }

  async logout(): Promise<void> {
    const token = this.getToken();

    try {
      if (token) {
        await fetch(`${API_BASE_URL}${API_ENDPOINTS.AUTH.LOGOUT}`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
        });
      }
    } catch (error) {
      console.error('Logout API failed:', error);
    } finally {
      this.clearSession();
    }
  }

  clearSession(): void {
    // Keep existing behavior: clear auth data from local storage.
    localStorage.clear();
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  getUser(): User | null {
    const userStr = localStorage.getItem(this.USER_KEY);
    return userStr ? JSON.parse(userStr) : null;
  }

  setUser(user: User): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getTeam(): Team | null {
    const user = this.getUser();
    return user?.team || null;
  }
}

export const authService = new AuthService();