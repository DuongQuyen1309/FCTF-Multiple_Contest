export interface Team {
  id: number;
  teamName: string;
}

export interface User {
  id: number;
  username: string;
  email: string;
  type?: string; // admin | teacher | user
  team: Team | null;
}