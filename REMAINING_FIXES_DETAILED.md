# Remaining Fixes - Detailed Guide

## ✅ Đã hoàn thành

1. ✅ Tạo `MultiContestHelper.cs` - Helper methods
2. ✅ Tạo `ContestContext.cs` - Context service
3. ✅ Tạo `ContestContextMiddleware.cs` - Middleware
4. ✅ Update `Program.cs` - Register services
5. ✅ Sửa `ActionLogsServices.cs` - User.TeamId
6. ✅ Sửa `AuthService.cs` - User.TeamId
7. ✅ Sửa `TicketService.cs` - User.TeamId

## ❌ Còn 42 lỗi cần sửa

### Approach: Inject ContestContext vào các Services

Tất cả các services cần:
1. Inject `ContestContext` vào constructor
2. Sử dụng `_contestContext.ContestId` thay vì hardcode
3. Query `ContestsChallenge` thay vì `Challenge`
4. Sử dụng `MultiContestHelper` cho User-Team relationship

### Template để sửa:

```csharp
public class SomeService
{
    private readonly AppDbContext _context;
    private readonly ContestContext _contestContext; // ADD THIS
    
    public SomeService(
        AppDbContext context,
        ContestContext contestContext) // ADD THIS
    {
        _context = context;
        _contestContext = contestContext; // ADD THIS
    }
    
    public async Task<Result> SomeMethod()
    {
        var contestId = _contestContext.ContestId; // USE THIS
        
        // OLD: var challenge = await _context.Challenges.FindAsync(id);
        // NEW:
        var contestChallenge = await _context.ContestsChallenges
            .FirstOrDefaultAsync(cc => 
                cc.ContestId == contestId && 
                cc.Id == contestChallengeId);
        
        // OLD: if (challenge.State == "visible")
        // NEW: if (contestChallenge.State == "visible")
        
        // OLD: var teamId = user.TeamId;
        // NEW: var teamId = await _context.GetUserTeamIdInContest(userId, contestId);
        
        // OLD: solve.ChallengeId
        // NEW: solve.ContestChallengeId
        
        // OLD: solve.Challenge.Name
        // NEW: solve.ContestChallenge.Name
    }
}
```

## Files cần sửa chi tiết

### 1. TeamService.cs (7 lỗi)

**Constructor:**
```csharp
private readonly ContestContext _contestContext;

public TeamService(
    AppDbContext context,
    ContestContext contestContext)
{
    _context = context;
    _contestContext = contestContext;
}
```

**Line 54:** `challenge.State`
```csharp
// OLD:
var challenge = await _context.Challenges.FindAsync(challengeId);
if (challenge.State != "visible")

// NEW:
var contestChallenge = await _context.ContestsChallenges
    .FirstOrDefaultAsync(cc => 
        cc.ContestId == _contestContext.ContestId && 
        cc.Id == contestChallengeId);
if (contestChallenge.State != "visible")
```

**Lines 97-103:** `solve.ChallengeId`, `solve.Challenge`
```csharp
// OLD:
.Where(s => s.ChallengeId == challengeId)
.Select(s => new {
    Name = s.Challenge.Name,
    Category = s.Challenge.Category,
    Value = s.Challenge.Value,
    Type = s.Challenge.Type
})

// NEW:
.Where(s => s.ContestChallengeId == contestChallengeId)
.Select(s => new {
    Name = s.ContestChallenge.Name,
    Category = s.ContestChallenge.BankChallenge.Category,
    Value = s.ContestChallenge.Value,
    Type = s.ContestChallenge.BankChallenge.Type
})
```

**Lines 112-113:** `user.Team`
```csharp
// OLD:
TeamId = user.Team?.Id,
TeamName = user.Team?.Name

// NEW:
var team = await _context.GetUserTeamInContest(user.Id, _contestContext.ContestId);
TeamId = team?.Id,
TeamName = team?.Name
```

### 2. HintService.cs (11 lỗi)

**Constructor:** Same as TeamService

**Multiple lines:** Replace all:
- `solve.ChallengeId` → `solve.ContestChallengeId`
- `user.TeamId` → `await _context.GetUserTeamIdInContest(user.Id, _contestContext.ContestId)`
- `challenge.State` → `contestChallenge.State`
- `user.Team` → `await _context.GetUserTeamInContest(user.Id, _contestContext.ContestId)`

### 3. ScoreboardService.cs (6 lỗi)

**Constructor:** Same as TeamService

**Lines 77, 81, 105, 109:**
```csharp
// OLD:
.Where(s => s.ChallengeId == challengeId)
.Select(s => s.Challenge.Name)

// NEW:
.Where(s => s.ContestChallengeId == contestChallengeId)
.Select(s => s.ContestChallenge.Name)
```

### 4. FileService.cs (3 lỗi)

**Constructor:** Same as TeamService

**Line 88:** `challenge.State`
```csharp
// OLD:
var challenge = await _context.Challenges.FindAsync(challengeId);
if (challenge.State != "visible")

// NEW:
var contestChallenge = await _context.ContestsChallenges
    .FirstOrDefaultAsync(cc => 
        cc.ContestId == _contestContext.ContestId && 
        cc.Id == contestChallengeId);
if (contestChallenge.State != "visible")
```

**Lines 119-120:** `solve.ChallengeId`
```csharp
// OLD:
.Where(s => s.ChallengeId == challengeId)
.Any(s => s.ChallengeId == challengeId)

// NEW:
.Where(s => s.ContestChallengeId == contestChallengeId)
.Any(s => s.ContestChallengeId == contestChallengeId)
```

### 5. ContestService.cs (10 lỗi)

**Constructor:** Same as TeamService

**Lines 327, 337:** `_logger.LogWarning`
```csharp
// AppLogger không có LogWarning method
// Sử dụng LogError thay thế:
_logger.LogError(null, null, "Warning message", data: new { ... });
```

**Lines 347-355:** `challenge.Value`, `challenge.MaxAttempts`, etc.
```csharp
// OLD:
var challenge = await _context.Challenges.FindAsync(challengeId);
Value = challenge.Value,
MaxAttempts = challenge.MaxAttempts,
State = challenge.State,
RequireDeploy = challenge.RequireDeploy,
ConnectionInfo = challenge.ConnectionInfo

// NEW:
var contestChallenge = await _context.ContestsChallenges
    .FirstOrDefaultAsync(cc => 
        cc.ContestId == _contestContext.ContestId && 
        cc.Id == contestChallengeId);
Value = contestChallenge.Value,
MaxAttempts = contestChallenge.MaxAttempts,
State = contestChallenge.State,
RequireDeploy = contestChallenge.RequireDeploy,
ConnectionInfo = contestChallenge.ConnectionInfo
```

**Line 447:** `ToHashSetAsync`
```csharp
// OLD:
var hashSet = await query.ToHashSetAsync();

// NEW:
var list = await query.ToListAsync();
var hashSet = list.ToHashSet();
```

**Lines 562-566:** Same as 347-355

### 6. HintController.cs (2 lỗi)

**Lines 88, 93:**
```csharp
// OLD:
var challenge = await _context.Challenges.FindAsync(challengeId);
if (challenge.State != "visible")
var hasSolved = solves.Any(s => s.ChallengeId == challengeId);

// NEW:
var contestChallenge = await _context.ContestsChallenges
    .FirstOrDefaultAsync(cc => 
        cc.ContestId == contestContext.ContestId && 
        cc.Id == contestChallengeId);
if (contestChallenge.State != "visible")
var hasSolved = solves.Any(s => s.ContestChallengeId == contestChallengeId);
```

## Tổng kết

Tất cả các sửa đổi đều follow pattern:
1. Inject ContestContext
2. Sử dụng ContestId từ context
3. Query ContestsChallenge thay vì Challenge
4. Sử dụng ContestChallengeId thay vì ChallengeId
5. Sử dụng MultiContestHelper cho User-Team

## Next Steps

1. Sửa từng file theo template trên
2. Build và test
3. Tạo Contest APIs mới
4. Update JWT generation
5. Update Frontend

**Estimated time:** 3-4 hours for all fixes
