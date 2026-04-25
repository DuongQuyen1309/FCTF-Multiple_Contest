import { API_BASE_URL } from './api';
import type { Contest, CreateContestPayload, PullChallengesPayload, ImportParticipantsPayload } from '../types/contestTypes';

class ContestService {
  private getAuthHeaders(): HeadersInit {
    const token = localStorage.getItem('auth_token');
    return {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
    };
  }

  async getAllContests(): Promise<Contest[]> {
    const token = localStorage.getItem('auth_token');
    console.log('[ContestService] Fetching contests with token:', token ? 'exists' : 'missing');
    
    const response = await fetch(`${API_BASE_URL}/api/Contest`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });

    console.log('[ContestService] Response status:', response.status);

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      console.error('[ContestService] Error fetching contests:', error);
      
      // If 401, redirect to login
      if (response.status === 401) {
        console.warn('[ContestService] Unauthorized - redirecting to login');
        localStorage.clear();
        window.location.href = '/login';
      }
      
      throw new Error(error.message || 'Failed to fetch contests');
    }

    const data = await response.json();
    console.log('[ContestService] Contests loaded:', data.data?.length || 0);
    return data.data || [];
  }

  async getContestById(contestId: number): Promise<Contest> {
    const response = await fetch(`${API_BASE_URL}/api/Contest/${contestId}`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || 'Failed to fetch contest');
    }

    const data = await response.json();
    return data.data;
  }

  async createContest(payload: CreateContestPayload): Promise<Contest> {
    const response = await fetch(`${API_BASE_URL}/api/Contest/create`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || 'Failed to create contest');
    }

    const data = await response.json();
    return data.data;
  }

  async pullChallengesToContest(contestId: number, payload: PullChallengesPayload): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/api/Contest/${contestId}/pull-challenges`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || 'Failed to pull challenges');
    }

    const data = await response.json();
    return data.data;
  }

  async importParticipants(contestId: number, payload: ImportParticipantsPayload): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/api/Contest/${contestId}/import-participants`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || 'Failed to import participants');
    }

    const data = await response.json();
    return data.data;
  }

  async getBankChallenges(): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/api/Contest/bank/challenges`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || 'Failed to fetch bank challenges');
    }

    const data = await response.json();
    return data.data || [];
  }

  async getContestChallenges(contestId: number): Promise<any[]> {
    const response = await fetch(`${API_BASE_URL}/api/Contest/${contestId}/challenges`, {
      method: 'GET',
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || 'Failed to fetch contest challenges');
    }

    const data = await response.json();
    return data.data || [];
  }
}

export const contestService = new ContestService();
