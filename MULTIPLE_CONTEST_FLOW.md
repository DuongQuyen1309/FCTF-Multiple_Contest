# Multiple Contest Flow - Implementation Summary

## 🎯 Overview

Hệ thống đã được chuyển đổi từ **single contest** sang **multiple contest** architecture. User có thể tham gia nhiều contests và switch giữa chúng.

## 🔄 Authentication Flow

### **Old Flow (Single Contest):**
```
Login → username + password + contestId → JWT with contestId → Challenges
```

### **New Flow (Multiple Contest):**
```
1. Login → username + password → Temporary JWT (contestId = 0)
2. Redirect to Contest List → Display contests user can access
3. Select Contest → Call /auth/select-contest → New JWT with contestId
4. Navigate to Contest Dashboard → Access contest-specific resources
```

## 📋 Key Changes

### **Backend Changes:**

#### 1. **LoginDTO** - Removed contestId requirement
```csharp
public class LoginDTO
{
    public string? username { get; set; }
    public string? password { get; set; }
    public string? captchaToken { get; set; }
    // contestId REMOVED
}
```

#### 2. **New API Endpoint: SelectContest**
```csharp
POST /auth/select-contest
Body: { "contestId": 123 }
Response: {
  "token": "new-jwt-with-contestId",
  "contestId": 123,
  "contestName": "Spring 2024 CTF",
  "teamId": 456,
  "teamName": "Team Alpha"
}
```

#### 3. **JWT Token Structure:**
- **Initial Login Token:** `{ userId, teamId: 0, contestId: 0 }`
- **Contest Token:** `{ userId, teamId, contestId }`

#### 4. **RequireContest Attribute**
```csharp
[RequireContest] // Ensures contestId > 0
public class ChallengeController : BaseController
```

#### 5. **UserContext Updates**
```csharp
public int ContestId
{
    get
    {
        var contestIdClaim = _httpContextAccessor.HttpContext!.User.FindFirstValue("contestId");
        return string.IsNullOrEmpty(contestIdClaim) ? 0 : int.Parse(contestIdClaim);
    }
}
```

### **Frontend Changes:**

#### 1. **AuthContext - Added selectContest**
```typescript
interface AuthContextType {
  isAuthenticated: boolean;
  user: User | null;
  login: (username: string, password: string, captchaToken?: string) => Promise<void>;
  selectContest: (contestId: number) => Promise<void>; // NEW
  logout: () => Promise<void>;
  loading: boolean;
}
```

#### 2. **Contest Selection Flow**
```typescript
const handleContestClick = async (contestId: number) => {
  try {
    await selectContest(contestId); // Get new JWT with contestId
    toast.success('Contest selected');
    navigate(`/contest/${contestId}/challenges`);
  } catch (error: any) {
    toast.error(error.message || 'Failed to select contest');
  }
};
```

#### 3. **Login Redirect**
```typescript
// After successful login
navigate('/contests'); // Redirect to contest list instead of challenges
```

## 🔐 Security & Access Control

### **Contest Access Rules:**

1. **Admin:**
   - Sees ALL contests
   - Can create contests
   - Can manage any contest

2. **Teacher:**
   - Sees contests they own OR participate in
   - Can create contests
   - Can manage their own contests

3. **Student (User):**
   - Sees ONLY contests they participate in
   - Cannot create contests
   - Can only access contest resources

### **JWT Token Validation:**

```csharp
// All challenge/scoreboard/instance endpoints check:
if (UserContext.ContestId <= 0)
{
    return BadRequest("No contest selected");
}

// All queries are scoped by contestId:
var challenges = await _context.ContestsChallenges
    .Where(cc => cc.ContestId == contestId)
    .ToListAsync();
```

### **Redis Key Structure:**

All Redis keys are prefixed with `contest:{contestId}`:
```
contest:123:deploy_challenge_456_789
contest:123:active_deploys_team_789
contest:123:submission_cooldown_456_789
contest:123:attempt_count_456_789
contest:123:auth:user:101
```

## 📊 Database Schema

### **Key Tables:**

1. **contests** - Contest definitions
2. **contest_participants** - User-Contest relationships
3. **contests_challenges** - Challenge instances per contest
4. **challenges** - Challenge bank (templates)
5. **submissions** - Scoped by contestId + contestChallengeId
6. **solves** - Scoped by contestId + contestChallengeId

### **Important Relationships:**

```
User ──< ContestParticipant >── Contest
                │
                └── Team (optional)

Contest ──< ContestsChallenge >── Challenge (bank)
            │
            ├── Submissions
            ├── Solves
            ├── DeployHistories
            └── ChallengeStartTrackings
```

## 🎨 Frontend Routes

### **Public Routes:**
- `/login` - Login page
- `/register` - Registration page
- `/public/scoreboard` - Public scoreboard

### **Protected Routes (No Contest Required):**
- `/contests` - Contest list
- `/contest/create` - Create contest (admin/teacher)
- `/profile` - User profile

### **Protected Routes (Contest Required):**
- `/contest/:contestId/challenges` - Challenge list
- `/contest/:contestId/challenge/:id` - Challenge detail
- `/contest/:contestId/scoreboard` - Scoreboard
- `/contest/:contestId/instances` - Active instances
- `/contest/:contestId/tickets` - Support tickets
- `/contest/:contestId/action-logs` - Activity logs
- `/contest/:contestId/pull-challenges` - Pull challenges (admin)
- `/contest/:contestId/import-participants` - Import users (admin)

## 🚀 Usage Examples

### **Student Flow:**
```
1. Login with username/password
2. See list of contests they're registered for
3. Click on "Spring 2024 CTF"
4. System calls /auth/select-contest with contestId
5. Receive new JWT token with contestId
6. Navigate to challenges page
7. All API calls now include contestId from JWT
```

### **Teacher Flow:**
```
1. Login with username/password
2. See list of contests (owned + participating)
3. Click "Create Contest"
4. Fill form and create new contest
5. Click "Pull Challenges" to add challenges from bank
6. Configure challenge settings (points, attempts, etc.)
7. Click "Import Participants" to add students
8. Upload Excel file with student emails
9. Students can now see and join the contest
```

### **Admin Flow:**
```
1. Login with username/password
2. See ALL contests in system
3. Can manage any contest
4. Can create/edit/delete contests
5. Can pull challenges and import participants for any contest
```

## ⚠️ Important Notes

### **Token Expiration:**
- Initial login token: 1 day
- Contest selection token: 7 days
- User must re-select contest if token expires

### **Contest Switching:**
- User can switch between contests by going back to `/contests`
- Each contest selection generates a new JWT token
- Old token becomes invalid

### **Data Isolation:**
- Each contest has isolated data (challenges, submissions, scores)
- Same challenge from bank can be in multiple contests
- Each instance is independent (different pods, URLs, ports)

### **Redis Isolation:**
- All Redis keys prefixed with `contest:{contestId}`
- Prevents data collision between contests
- Each contest has separate rate limits, cooldowns, etc.

## 🔧 Migration Checklist

- [x] Remove contestId from LoginDTO
- [x] Create SelectContestDTO and endpoint
- [x] Update LoginContestant to generate temporary token
- [x] Implement SelectContest method
- [x] Add RequireContest attribute
- [x] Update UserContext to handle contestId = 0
- [x] Update frontend AuthContext with selectContest
- [x] Update ContestList to call selectContest on click
- [x] Update Login to redirect to /contests
- [x] Create contest management pages (Create, Pull, Import)
- [x] Update GetAllContests to filter by participation
- [x] Add contest selection API endpoint

## 📝 Testing Scenarios

### **Test 1: Login Flow**
1. Login with valid credentials
2. Should redirect to `/contests`
3. Should see only contests user participates in
4. Token should have contestId = 0

### **Test 2: Contest Selection**
1. Click on a contest
2. Should call `/auth/select-contest`
3. Should receive new token with contestId
4. Should navigate to contest challenges
5. Should be able to access challenge endpoints

### **Test 3: Access Control**
1. Try to access `/contest/123/challenges` without selecting contest
2. Should return error "No contest selected"
3. After selecting contest, should work normally

### **Test 4: Contest Switching**
1. Select Contest A
2. Access challenges in Contest A
3. Go back to `/contests`
4. Select Contest B
5. Should receive new token with Contest B's contestId
6. Should see Contest B's challenges (not Contest A's)

### **Test 5: Admin Functions**
1. Login as admin
2. Create new contest
3. Pull challenges from bank
4. Configure challenge settings
5. Import participants
6. Verify students can see and access contest

## 🎉 Benefits

1. **Scalability:** Support unlimited contests
2. **Isolation:** Each contest has independent data
3. **Flexibility:** Same challenge can be reused across contests
4. **Security:** Token-based access control per contest
5. **User Experience:** Clear contest selection flow
6. **Multi-tenancy:** Multiple contests can run simultaneously

## 📚 API Documentation

### **POST /auth/login-contestant**
```json
Request:
{
  "username": "student1",
  "password": "password123",
  "captchaToken": "optional-token"
}

Response:
{
  "generatedToken": "jwt-token-with-contestId-0",
  "user": {
    "id": 1,
    "username": "student1",
    "email": "student1@example.com",
    "type": "user",
    "team": null
  }
}
```

### **POST /auth/select-contest**
```json
Request:
{
  "contestId": 123
}

Response:
{
  "message": "Contest selected successfully",
  "data": {
    "token": "jwt-token-with-contestId-123",
    "contestId": 123,
    "contestName": "Spring 2024 CTF",
    "teamId": 456,
    "teamName": "Team Alpha"
  }
}
```

### **GET /api/Contest/list**
```json
Response:
{
  "data": [
    {
      "id": 123,
      "name": "Spring 2024 CTF",
      "description": "Spring semester CTF competition",
      "slug": "spring-2024-ctf",
      "state": "visible",
      "startTime": "2024-03-01T00:00:00Z",
      "endTime": "2024-03-31T23:59:59Z",
      "participantCount": 50,
      "challengeCount": 20
    }
  ]
}
```

---

**Last Updated:** 2024
**Version:** 1.0
**Status:** ✅ Implemented
