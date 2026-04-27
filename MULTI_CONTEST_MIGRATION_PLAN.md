# Multi-Contest Migration Plan

## Tổng quan
Chuyển đổi hệ thống từ Single Contest sang Multi-Contest Architecture

## Database Schema (Đã có)
- ✅ `challenges` - Bank challenges (template)
- ✅ `contests` - Các cuộc thi
- ✅ `contests_challenges` - Challenge instances trong từng contest
- ✅ `contest_participants` - User tham gia contest
- ✅ `users` - User tổng của hệ thống
- ✅ `users_teams` - Many-to-many relationship
- ✅ `solves` - FK → contest_challenge_id
- ✅ `submissions` - FK → contest_challenge_id

## Các thay đổi cần thực hiện

### 1. JWT Token Changes
**Hiện tại:** Token chỉ có user info
**Cần:** Token phải có `contestId` và `contestChallengeId`

```csharp
// JWT Claims cần có:
- userId
- username
- email
- type (admin/user)
- contestId (contest đang tham gia)
- teamId (nếu có)
```

### 2. Redis Key Structure
**Hiện tại:** Keys không có prefix contest
**Cần:** Prefix theo `contest:{contestId}:...`

```
contest:{contestId}:deploy_challenge_{contestChallengeId}_{teamId}
contest:{contestId}:active_deploys_team_{teamId}
contest:{contestId}:auth:user:{userId}
```

### 3. API Changes

#### A. Challenge Management (Admin)
1. **Upload Challenge to Bank** (Đã có)
   - POST `/api/challenges` → Lưu vào `challenges` table
   
2. **Pull Challenge to Contest** (CẦN TẠO MỚI)
   - POST `/api/contests/{contestId}/challenges/pull`
   - Body: `{ bankChallengeId, overrides?: {...} }`
   - Logic:
     - Copy từ `challenges` → `contests_challenges`
     - Nếu có overrides thì update các field
     - Nếu không có overrides thì copy nguyên từ bank

3. **Update Contest Challenge Config** (CẦN TẠO MỚI)
   - PUT `/api/contests/{contestId}/challenges/{contestChallengeId}`
   - Cho phép update: value, state, maxAttempts, timeLimit, requireDeploy, etc.

#### B. Contest Participant Management (CẦN TẠO MỚI)
1. **Import Participants from Excel**
   - POST `/api/contests/{contestId}/participants/import`
   - Body: FormData with Excel file
   - Logic:
     - Đọc email từ Excel
     - Tìm user trong `users` table
     - Nếu không có → Insert vào `users` (email only, verified=false)
     - Insert vào `contest_participants`

2. **List Contest Participants**
   - GET `/api/contests/{contestId}/participants`

3. **Remove Participant**
   - DELETE `/api/contests/{contestId}/participants/{userId}`

#### C. Contest Selection Flow
1. **List Contests** (CẦN TẠO MỚI)
   - GET `/api/contests` → List contests user có quyền truy cập
   
2. **Select Contest** (CẦN TẠO MỚI)
   - POST `/api/auth/select-contest`
   - Body: `{ contestId }`
   - Response: New JWT token with contestId

### 4. Frontend Changes

#### A. Admin Flow
```
Login → Contest List Page → Select Contest → Dashboard (hiện tại)
                                              ├─ Challenges
                                              ├─ Users
                                              ├─ Tickets
                                              └─ Action Logs
```

**Sidebar Structure:**
```
- Contests (new)
  - Contest A
  - Contest B
- [Selected Contest Name]
  - Dashboard
  - Challenges
  - Users
  - Tickets
  - Action Logs
```

#### B. Student Flow
```
Login → Contest List Page → Select Contest → Challenges Page
```

### 5. Code Changes Required

#### Backend (C#)
1. ✅ **ResourceShared** - Đã sửa xong
2. ❌ **ContestantBE/Services** - Cần sửa:
   - ActionLogsServices.cs
   - TeamService.cs
   - HintService.cs
   - ScoreboardService.cs
   - FileService.cs
   - ContestService.cs
   - AuthService.cs
   - TicketService.cs

3. ❌ **ContestantBE/Controllers** - Cần sửa:
   - Tất cả controllers phải check contestId từ JWT
   - Thêm ContestController mới

#### Frontend (React)
1. ❌ **Contest List Page** - Cần tạo mới
2. ❌ **Contest Selection** - Cần tạo mới
3. ❌ **Sidebar** - Cần update
4. ❌ **AuthContext** - Cần update để lưu contestId
5. ❌ **API calls** - Cần thêm contestId vào headers/params

### 6. Migration Steps

#### Phase 1: Backend Core (ĐANG LÀM)
- [x] Fix ResourceShared models
- [ ] Fix all Services to use ContestsChallenge instead of Challenge
- [ ] Fix all Services to use UserTeam junction table
- [ ] Update JWT generation to include contestId
- [ ] Update Redis keys with contest prefix

#### Phase 2: New APIs
- [ ] Create ContestController
- [ ] Create Contest Participant APIs
- [ ] Create Pull Challenge API
- [ ] Create Update Contest Challenge API

#### Phase 3: Frontend
- [ ] Create Contest List Page
- [ ] Update Sidebar
- [ ] Update AuthContext
- [ ] Update all API calls

#### Phase 4: Testing
- [ ] Test multi-contest isolation
- [ ] Test JWT with contestId
- [ ] Test Redis key isolation
- [ ] Test challenge deployment per contest

## Lưu ý quan trọng

1. **Isolation giữa các Contest:**
   - Mỗi contest phải độc lập hoàn toàn
   - Token của Contest A không được dùng cho Contest B
   - Redis keys phải có prefix contest_id
   - Challenge deployments phải độc lập

2. **Challenge Bank vs Contest Challenge:**
   - `challenges` table = Bank (template)
   - `contests_challenges` table = Instance trong contest cụ thể
   - Khi pull challenge: Copy từ bank → instance
   - Mỗi contest có instance riêng của cùng 1 challenge

3. **User Management:**
   - `users` table = Tổng user của hệ thống
   - `contest_participants` = User được phép tham gia contest cụ thể
   - Import Excel: Tự động tạo user nếu chưa có

## Next Steps
1. Sửa tất cả lỗi compile trong ContestantBE
2. Tạo các API mới
3. Update Frontend
