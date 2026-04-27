export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: '/api/Auth/login-contestant',
    SELECT_CONTEST: '/api/Auth/select-contest',
    REGISTER: '/api/Auth/register-contestant',
    REGISTRATION_METADATA: '/api/Auth/registration-metadata',
    LOGOUT: '/api/Auth/logout',
    CHANGE_PASSWORD: '/api/Auth/change-password',
  },
  CONFIG: {
    DATE_CONFIG: '/api/Config/get_date_config',
    PUBLIC: '/api/Config/get_public_config',
    CONTEST_ACCESS: '/api/Config/contest_access',
  },
  CHALLENGES: {
    BY_TOPIC: '/api/Challenge/by-topic',
    LIST: '/api/Challenge/list_challenge/',
    DETAIL: (id: string | number) => `/api/Challenge/${id}`,
    SUBMIT: (id: string | number) => `/api/Challenge/${id}/submit`,
    START: '/api/Challenge/start',
    STOP: '/api/Challenge/stop-by-user',
    CHECK_CACHE: '/api/Challenge/check_cache',
    START_CHECKING: '/api/Challenge/check-status',
    INSTANCES: '/api/Challenge/instances',
  },
  HINTS: {
    GET_ALL: (challengeId: string | number) => `/api/Hint/${challengeId}/all`,
    GET_DETAIL: (hintId: string | number) => `/api/Hint/${hintId}`,
    UNLOCK: '/api/Hint/unlock',
  },
  TICKET: {
    LIST: '/api/Ticket/tickets-user',
    CREATE: '/api/Ticket/sendticket',
    DETAIL: (id: string) => `/api/Ticket/tickets/${id}`,
    DELETE: (id: string) => `/api/Ticket/tickets/${id}`,
  },
  USER: {
    PROFILE: '/api/Users/profile',
  },
  ACTION_LOGS: {
    GET: '/api/ActionLogs/get-logs-team',
    POST: '/api/ActionLogs/save-logs',
  },
  FLAGS: {
    SUBMIT: '/api/Challenge/attempt',
  },
  SCOREBOARD: {
    TOP_STANDINGS: '/api/Scoreboard/top/200',
    BRACKETS: '/api/Scoreboard/brackets',
    FREEZE_STATUS: '/api/Scoreboard/freeze-status',
  },
} as const;
