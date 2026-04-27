# Backend Fixes Summary

## Các lỗi chính cần sửa

### 1. Challenge Model Changes
**Lỗi:** Code đang dùng `Challenge.State`, `Challenge.Value`, `Challenge.MaxAttempts`, etc.
**Sửa:** Các thuộc tính này đã chuyển sang `ContestsChallenge`

**Mapping:**
- `Challenge.State` → `ContestsChallenge.State`
- `Challenge.Value` → `ContestsChallenge.Value`
- `Challenge.MaxAttempts` → `ContestsChallenge.MaxAttempts`
- `Challenge.RequireDeploy` → `ContestsChallenge.RequireDeploy`
- `Challenge.ConnectionInfo` → `ContestsChallenge.ConnectionInfo`
- `Challenge.TimeLimit` → `ContestsChallenge.TimeLimit`

### 2. Solf Model Changes
**Lỗi:** Code đang dùng `Solf.ChallengeId` và `Solf.Challenge`
**Sửa:** Đã chuyển sang `Solf.ContestChallengeId` và `Solf.ContestChallenge`

**Mapping:**
- `Solf.ChallengeId` → `Solf.ContestChallengeId`
- `Solf.Challenge` → `Solf.ContestChallenge`

### 3. User-Team Relationship Changes
**Lỗi:** Code đang dùng `User.TeamId` và `User.Team`
**Sửa:** Đã chuyển sang many-to-many qua `UserTeam` junction table

**Cách sửa:**
```csharp
// Thay vì:
var teamId = user.TeamId;
var team = user.Team;

// Dùng:
var userTeams = await _context.Set<UserTeam>()
    .Where(ut => ut.UserId == user.Id && ut.TeamId == teamId)
    .FirstOrDefaultAsync();
    
// Hoặc lấy team của user trong contest:
var team = await _context.Teams
    .Where(t => t.ContestId == contestId && 
                t.Users.Any(u => u.Id == userId))
    .FirstOrDefaultAsync();
```

### 4. Files cần sửa

#### Services:
1. ActionLogsServices.cs - Line 47: user.TeamId
2. TeamService.cs - Lines 54, 97-103, 112-113: Challenge.State, Solf.Challenge, User.Team
3. HintService.cs - Multiple lines: Solf.ChallengeId, User.TeamId, Challenge.State, User.Team
4. ScoreboardService.cs - Lines 77, 81, 105, 109: Solf.ChallengeId, Solf.Challenge
5. FileService.cs - Lines 88, 119-120: Challenge.State, Solf.ChallengeId
6. ContestService.cs - Lines 327, 337, 347-355, 447, 562-566: Challenge properties, LogWarning, ToHashSetAsync
7. AuthService.cs - Line 487: User.TeamId
8. TicketService.cs - Line 165, 185: User.TeamId, TEntity.t

#### Controllers:
1. HintController.cs - Lines 88, 93: Challenge.State, Solf.ChallengeId

## Chiến lược sửa

### Bước 1: Tạo Helper Methods
Tạo các helper methods để lấy team của user trong contest cụ thể

### Bước 2: Sửa từng Service
Sửa từng service một cách có hệ thống

### Bước 3: Update Controllers
Update controllers để sử dụng contestId từ JWT

### Bước 4: Test
Test từng phần sau khi sửa
