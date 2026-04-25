# 🏗️ FCTF Multiple Contest - Architecture Diagram

## 📊 System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         FCTF Platform                            │
│                    Multiple Contest System                       │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┼───────────────┐
                │               │               │
         ┌──────▼──────┐ ┌─────▼─────┐ ┌──────▼──────┐
         │   Frontend  │ │  Backend  │ │Infrastructure│
         │  React/TS   │ │  .NET/C#  │ │ Docker/K8s  │
         └─────────────┘ └───────────┘ └─────────────┘
```

---

## 🔄 Authentication Flow

### **Old Flow (Single Contest):**
```
┌──────────┐
│  Login   │ ──────────────────────────────────────┐
└──────────┘                                        │
     │                                              │
     │ username, password, contestId               │
     ▼                                              │
┌──────────────────────────────────────┐           │
│  Backend: AuthService.LoginContestant │           │
└──────────────────────────────────────┘           │
     │                                              │
     │ Validate credentials                         │
     │ Check contest participation                  │
     ▼                                              │
┌──────────────────────────────────────┐           │
│  Generate JWT Token                   │           │
│  { userId, contestId, teamId }        │           │
└──────────────────────────────────────┘           │
     │                                              │
     │ Return token                                 │
     ▼                                              │
┌──────────────────────────────────────┐           │
│  Frontend: Store token                │           │
│  Redirect to /challenges              │◄──────────┘
└──────────────────────────────────────┘
     │
     │ Access challenges with contestId in JWT
     ▼
┌──────────────────────────────────────┐
│  View Challenges (Contest Specific)   │
└──────────────────────────────────────┘
```

### **New Flow (Multiple Contest):**
```
┌──────────┐
│  Login   │ ──────────────────────────────────────┐
└──────────┘                                        │
     │                                              │
     │ username, password (NO contestId)           │
     ▼                                              │
┌──────────────────────────────────────┐           │
│  Backend: AuthService.LoginContestant │           │
└──────────────────────────────────────┘           │
     │                                              │
     │ Validate credentials only                    │
     ▼                                              │
┌──────────────────────────────────────┐           │
│  Generate Temporary JWT Token         │           │
│  { userId, contestId: 0, teamId: 0 } │           │
└──────────────────────────────────────┘           │
     │                                              │
     │ Return temporary token                       │
     ▼                                              │
┌──────────────────────────────────────┐           │
│  Frontend: Store token                │           │
│  Redirect to /contests                │◄──────────┘
└──────────────────────────────────────┘
     │
     │ Display contest list
     ▼
┌──────────────────────────────────────┐
│  Contest List Page                    │
│  - Test Contest 1                     │
│  - Test Contest 2                     │
│  - Test Contest 3                     │
└──────────────────────────────────────┘
     │
     │ User clicks "Test Contest 1"
     ▼
┌──────────────────────────────────────┐
│  Frontend: Call selectContest(1)     │
└──────────────────────────────────────┘
     │
     │ POST /auth/select-contest { contestId: 1 }
     ▼
┌──────────────────────────────────────┐
│  Backend: AuthService.SelectContest   │
└──────────────────────────────────────┘
     │
     │ Validate contest exists
     │ Validate user is participant
     │ Get teamId from contest_participants
     ▼
┌──────────────────────────────────────┐
│  Generate Contest-Specific JWT Token  │
│  { userId, contestId: 1, teamId: X }  │
└──────────────────────────────────────┘
     │
     │ Return new token
     ▼
┌──────────────────────────────────────┐
│  Frontend: Replace token              │
│  Redirect to /contest/1/challenges    │
└──────────────────────────────────────┘
     │
     │ Access challenges with contestId=1 in JWT
     ▼
┌──────────────────────────────────────┐
│  View Challenges (Contest 1 Only)     │
└──────────────────────────────────────┘
```

---

## 🗄️ Database Schema

```
┌─────────────────────────────────────────────────────────────────┐
│                         Database Tables                          │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│    users     │         │   contests   │         │  challenges  │
├──────────────┤         ├──────────────┤         ├──────────────┤
│ id (PK)      │         │ id (PK)      │         │ id (PK)      │
│ name         │         │ name         │         │ name         │
│ email        │         │ slug         │         │ category     │
│ password     │         │ owner_id (FK)│         │ description  │
│ type         │         │ semester_name│         │ value        │
│ verified     │         │ state        │         │ type         │
│ team_id      │         │ user_mode    │         │ state        │
└──────────────┘         │ start_time   │         │ flags        │
       │                 │ end_time     │         └──────────────┘
       │                 └──────────────┘                │
       │                        │                        │
       │                        │                        │
       │         ┌──────────────┴──────────────┐        │
       │         │                             │        │
       ▼         ▼                             ▼        ▼
┌─────────────────────────────┐    ┌──────────────────────────┐
│   contest_participants      │    │  contests_challenges     │
├─────────────────────────────┤    ├──────────────────────────┤
│ id (PK)                     │    │ id (PK)                  │
│ contest_id (FK) ────────────┼───►│ contest_id (FK)          │
│ user_id (FK) ───────────────┤    │ bank_id (FK) ────────────┤
│ team_id (FK)                │    │ name                     │
│ role                        │    │ value                    │
│ score                       │    │ max_attempts             │
│ joined_at                   │    │ state                    │
└─────────────────────────────┘    │ time_limit               │
                                    │ cooldown                 │
                                    │ require_deploy           │
                                    │ max_deploy_count         │
                                    │ connection_protocol      │
                                    │ connection_info          │
                                    │ deploy_status            │
                                    └──────────────────────────┘
                                               │
                                               │
                                               ▼
                                    ┌──────────────────────────┐
                                    │      submissions         │
                                    ├──────────────────────────┤
                                    │ id (PK)                  │
                                    │ contest_id (FK)          │
                                    │ contest_challenge_id (FK)│
                                    │ user_id (FK)             │
                                    │ team_id (FK)             │
                                    │ provided                 │
                                    │ type (correct/incorrect) │
                                    │ date                     │
                                    └──────────────────────────┘
```

### **Key Relationships:**
- `users` ←→ `contest_participants` ←→ `contests` (Many-to-Many)
- `challenges` (Bank) → `contests_challenges` (Instances)
- `contests_challenges` → `submissions`

---

## 🔐 JWT Token Structure

### **Temporary Token (After Login):**
```json
{
  "userId": 3,
  "contestId": 0,
  "teamId": 0,
  "exp": 1234567890,
  "iat": 1234567890
}
```
**Purpose:** Allow user to view contest list only

### **Contest Token (After Select Contest):**
```json
{
  "userId": 3,
  "contestId": 1,
  "teamId": 5,
  "exp": 1234567890,
  "iat": 1234567890
}
```
**Purpose:** Allow user to access contest-specific resources

---

## 🔑 Redis Key Structure

```
contest:{contestId}:auth:user:{userId}
contest:{contestId}:challenge:{challengeId}:attempts:{userId}
contest:{contestId}:challenge:{challengeId}:cooldown:{userId}
contest:{contestId}:scoreboard
contest:{contestId}:team:{teamId}:score
```

**Benefits:**
- Data isolation between contests
- Easy to flush contest-specific data
- No key collision
- Clear ownership

---

## 🎯 API Endpoints

### **Authentication:**
```
POST   /api/Auth/login                    # Login (no contestId)
POST   /api/Auth/select-contest           # Select contest
POST   /api/Auth/logout                   # Logout
POST   /api/Auth/change-password          # Change password
```

### **Contest Management:**
```
GET    /api/Contest/list                  # Get all contests
GET    /api/Contest/{contestId}           # Get contest details
POST   /api/Contest/create                # Create contest
POST   /api/Contest/{contestId}/pull-challenges      # Pull challenges
POST   /api/Contest/{contestId}/import-participants  # Import participants
GET    /api/Contest/bank/challenges       # Get challenge bank
GET    /api/Contest/{contestId}/challenges # Get contest challenges
```

### **Challenge Operations:**
```
GET    /api/Challenge                     # Get challenges (uses contestId from JWT)
GET    /api/Challenge/{id}                # Get challenge details
POST   /api/Challenge/{id}/submit         # Submit flag
POST   /api/Challenge/{id}/start          # Start challenge instance
POST   /api/Challenge/{id}/stop           # Stop challenge instance
```

### **Scoreboard:**
```
GET    /api/Scoreboard                    # Get scoreboard (uses contestId from JWT)
GET    /api/Scoreboard/public             # Get public scoreboard
```

---

## 🛡️ Access Control Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Request with JWT Token                        │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │  JWT Middleware       │
                    │  Validate token       │
                    └───────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │  Extract claims:      │
                    │  - userId             │
                    │  - contestId          │
                    │  - teamId             │
                    └───────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │  UserContext          │
                    │  Store in context     │
                    └───────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │  RequireContestAttribute│
                    │  Check contestId > 0  │
                    └───────────────────────┘
                                │
                    ┌───────────┴───────────┐
                    │                       │
                    ▼                       ▼
            contestId = 0            contestId > 0
                    │                       │
                    ▼                       ▼
            ┌───────────────┐      ┌───────────────┐
            │  403 Forbidden│      │  Allow Access │
            └───────────────┘      └───────────────┘
                                            │
                                            ▼
                                ┌───────────────────────┐
                                │  Controller Action    │
                                │  Use contestId from   │
                                │  UserContext          │
                                └───────────────────────┘
                                            │
                                            ▼
                                ┌───────────────────────┐
                                │  Service Layer        │
                                │  Query with contestId │
                                └───────────────────────┘
                                            │
                                            ▼
                                ┌───────────────────────┐
                                │  Database             │
                                │  WHERE contest_id = X │
                                └───────────────────────┘
```

---

## 🎭 User Roles & Permissions

```
┌─────────────────────────────────────────────────────────────────┐
│                          User Roles                              │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐
│    Admin     │
├──────────────┤
│ ✅ View all contests
│ ✅ Create contests
│ ✅ Pull challenges
│ ✅ Import participants
│ ✅ Manage all contests
│ ✅ Upload challenges to bank
└──────────────┘

┌──────────────┐
│   Teacher    │
├──────────────┤
│ ✅ View own contests
│ ✅ View participated contests
│ ✅ Create contests
│ ✅ Pull challenges
│ ✅ Import participants
│ ✅ Manage own contests
│ ❌ Cannot manage other teachers' contests
└──────────────┘

┌──────────────┐
│   Student    │
├──────────────┤
│ ✅ View participated contests only
│ ✅ Select contest
│ ✅ View challenges
│ ✅ Submit flags
│ ✅ View scoreboard
│ ✅ Manage instances
│ ❌ Cannot create contests
│ ❌ Cannot pull challenges
│ ❌ Cannot import participants
└──────────────┘
```

---

## 🔄 Challenge Pull Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Challenge Bank (Template)                     │
│                      challenges table                            │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ Teacher/Admin pulls challenge
                                ▼
                    ┌───────────────────────┐
                    │  Pull Challenge UI    │
                    │  - Select challenges  │
                    │  - Configure settings │
                    └───────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │  Configuration Dialog │
                    │  Override:            │
                    │  - name               │
                    │  - value              │
                    │  - maxAttempts        │
                    │  - state              │
                    │  - timeLimit          │
                    │  - cooldown           │
                    │  - maxDeployCount     │
                    │  - connectionProtocol │
                    │  - connectionInfo     │
                    └───────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │  Backend: ContestService│
                    │  PullChallengesToContest│
                    └───────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │  Create Instance:     │
                    │  contests_challenges  │
                    │  - Copy from bank     │
                    │  - Apply overrides    │
                    │  - Link to contest    │
                    └───────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                Contest-Specific Challenge Instance               │
│                  contests_challenges table                       │
└─────────────────────────────────────────────────────────────────┘
```

**Key Points:**
- Bank challenge remains unchanged
- Each contest gets its own instance
- Instances can have different configurations
- Same bank challenge can be pulled to multiple contests

---

## 📊 Data Isolation

```
┌─────────────────────────────────────────────────────────────────┐
│                         Contest 1                                │
├─────────────────────────────────────────────────────────────────┤
│  Participants: student1, student2, student3                      │
│  Challenges: Web1, Crypto1, Forensics1                          │
│  Submissions: 15 submissions                                     │
│  Scoreboard: Contest 1 specific                                  │
│  Redis Keys: contest:1:*                                         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         Contest 2                                │
├─────────────────────────────────────────────────────────────────┤
│  Participants: student1, student4                                │
│  Challenges: Pwn1, Reverse1, Misc1                              │
│  Submissions: 8 submissions                                      │
│  Scoreboard: Contest 2 specific                                  │
│  Redis Keys: contest:2:*                                         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         Contest 3                                │
├─────────────────────────────────────────────────────────────────┤
│  Participants: (empty)                                           │
│  Challenges: (empty)                                             │
│  Submissions: 0 submissions                                      │
│  Scoreboard: Empty                                               │
│  Redis Keys: contest:3:*                                         │
└─────────────────────────────────────────────────────────────────┘
```

**Isolation Mechanisms:**
1. Database queries filtered by `contest_id`
2. JWT token includes `contestId`
3. Redis keys prefixed with `contest:{contestId}:`
4. Access control validates contest participation

---

## 🚀 Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Production Environment                      │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Frontend   │     │   Backend    │     │  Challenge   │
│   (React)    │────►│   (.NET)     │────►│  Gateway     │
│   Nginx      │     │   K8s Pod    │     │   (Go)       │
└──────────────┘     └──────────────┘     └──────────────┘
                             │                     │
                             │                     │
                             ▼                     ▼
                     ┌──────────────┐     ┌──────────────┐
                     │   MariaDB    │     │  Challenge   │
                     │   (Primary)  │     │  Instances   │
                     └──────────────┘     │  (K8s Pods)  │
                             │             └──────────────┘
                             │
                             ▼
                     ┌──────────────┐
                     │    Redis     │
                     │   (Cache)    │
                     └──────────────┘
                             │
                             │
                             ▼
                     ┌──────────────┐
                     │  RabbitMQ    │
                     │  (Queue)     │
                     └──────────────┘
```

---

## 📈 Scalability

```
Multiple Contests → Multiple Participants → Multiple Challenges
        │                   │                       │
        ▼                   ▼                       ▼
┌──────────────┐    ┌──────────────┐      ┌──────────────┐
│  Contest 1   │    │  1000 users  │      │  50 challs   │
│  Contest 2   │    │  500 users   │      │  30 challs   │
│  Contest 3   │    │  2000 users  │      │  40 challs   │
│  ...         │    │  ...         │      │  ...         │
└──────────────┘    └──────────────┘      └──────────────┘
        │                   │                       │
        └───────────────────┴───────────────────────┘
                            │
                            ▼
                ┌───────────────────────┐
                │  Isolated Data        │
                │  - Database scoped    │
                │  - Redis prefixed     │
                │  - JWT validated      │
                └───────────────────────┘
```

---

**This architecture ensures:**
- ✅ Data isolation between contests
- ✅ Scalability for multiple contests
- ✅ Security through JWT and access control
- ✅ Flexibility for contest configuration
- ✅ Easy management for admins/teachers

