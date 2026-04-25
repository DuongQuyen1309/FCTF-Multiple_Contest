export interface Contest {
  id: number;
  name: string;
  description?: string;
  slug: string;
  ownerId?: number;
  ownerName?: string;
  semesterName?: string;
  state: 'draft' | 'visible' | 'paused' | 'ended';
  userMode: 'users' | 'teams';
  startTime: string | null;
  endTime: string | null;
  createdAt: string;
  participantCount: number;
  challengeCount: number;
}

export interface CreateContestPayload {
  name: string;
  description?: string;
  slug: string;
  semesterName?: string;
  startTime?: string;
  endTime?: string;
  userMode: 'users' | 'teams';
}

export interface PullChallengeItem {
  bankChallengeId: number;
  name?: string;
  value?: number;
  maxAttempts?: number;
  state?: string;
  timeLimit?: number;
  cooldown?: number;
  requireDeploy?: boolean;
  maxDeployCount?: number;
  connectionProtocol?: string;
  connectionInfo?: string;
}

export interface PullChallengesPayload {
  challenges: PullChallengeItem[];
}

export interface ImportParticipantsPayload {
  emails: string[];
  role?: 'contestant' | 'jury' | 'challenge_writer';
}

export interface BankChallenge {
  id: number;
  name: string;
  category?: string;
  description?: string;
  value?: number;
  type?: string;
  state: string;
  maxAttempts?: number;
  requireDeploy: boolean;
  maxDeployCount?: number;
  connectionProtocol?: string;
}

export interface ContestChallenge {
  id: number;
  contestId: number;
  bankId?: number;
  name: string;
  category?: string;
  value?: number;
  state: string;
  maxAttempts?: number;
  requireDeploy: boolean;
  solveCount: number;
}
