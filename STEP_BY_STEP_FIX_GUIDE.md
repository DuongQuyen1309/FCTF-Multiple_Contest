# Step-by-Step Fix Guide

## Tình trạng hiện tại
- ✅ ResourceShared project: Đã build thành công
- ❌ ContestantBE project: 48 lỗi compile

## Chiến lược

### Option 1: Sửa thủ công từng file (Chậm nhưng chính xác)
- Ưu điểm: Kiểm soát tốt, hiểu rõ logic
- Nhược điểm: Mất nhiều thời gian (48 lỗi)

### Option 2: Tạo Helper Extension Methods (Khuyến nghị)
- Tạo extension methods để bridge giữa old và new architecture
- Sau đó dần dần refactor

### Option 3: Tạm thời comment code lỗi
- Comment các phần lỗi để build được
- Sau đó sửa từng phần

## Quyết định: Option 2 - Tạo Helper Methods

### 1. Tạo Helper Class

```csharp
// ControlCenterAndChallengeHostingServer/ResourceShared/Utils/MultiContestHelper.cs

namespace ResourceShared.Utils
{
    public static class MultiContestHelper
    {
        /// <summary>
        /// Get team of user in specific contest
        /// </summary>
        public static async Task<Team?> GetUserTeamInContest(
            this AppDbContext context, 
            int userId, 
            int contestId)
        {
            return await context.Teams
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => 
                    t.ContestId == contestId && 
                    t.Users.Any(u => u.Id == userId));
        }

        /// <summary>
        /// Get team ID of user in specific contest
        /// </summary>
        public static async Task<int?> GetUserTeamIdInContest(
            this AppDbContext context, 
            int userId, 
            int contestId)
        {
            var team = await GetUserTeamInContest(context, userId, contestId);
            return team?.Id;
        }

        /// <summary>
        /// Check if user is in a team in specific contest
        /// </summary>
        public static async Task<bool> IsUserInTeamInContest(
            this AppDbContext context, 
            int userId, 
            int contestId)
        {
            return await context.Teams
                .AnyAsync(t => 
                    t.ContestId == contestId && 
                    t.Users.Any(u => u.Id == userId));
        }

        /// <summary>
        /// Get contest challenge from bank challenge ID
        /// </summary>
        public static async Task<ContestsChallenge?> GetContestChallenge(
            this AppDbContext context,
            int contestId,
            int bankChallengeId)
        {
            return await context.ContestsChallenges
                .FirstOrDefaultAsync(cc => 
                    cc.ContestId == contestId && 
                    cc.BankId == bankChallengeId);
        }
    }
}
```

### 2. Sửa JWT Token Service

Cần thêm contestId vào JWT claims khi user select contest.

### 3. Sửa từng Service

#### Ví dụ: ActionLogsServices.cs Line 47
```csharp
// Cũ:
TeamId = user.TeamId

// Mới:
TeamId = await _context.GetUserTeamIdInContest(user.Id, contestId)
```

## Vấn đề: ContestId từ đâu?

Đây là vấn đề lớn nhất. Hiện tại code không có contestId context.

### Giải pháp:

#### Option A: Lấy từ JWT Token (Khuyến nghị)
```csharp
// Trong Controller/Service
var contestIdStr = User.FindFirstValue("contestId");
int.TryParse(contestIdStr, out var contestId);
```

#### Option B: Thêm parameter contestId vào tất cả methods
```csharp
public async Task<Result> GetSomething(int userId, int contestId)
```

#### Option C: Tạo Context Service
```csharp
public class ContestContext
{
    public int ContestId { get; set; }
    public int UserId { get; set; }
}

// Inject vào services
public class MyService
{
    private readonly ContestContext _contestContext;
    
    public MyService(ContestContext contestContext)
    {
        _contestContext = contestContext;
    }
}
```

## Quyết định tiếp theo

Vì đây là migration lớn, tôi đề xuất:

1. **Tạm thời:** Thêm parameter `contestId` vào các methods cần thiết
2. **Sau này:** Refactor để lấy từ JWT token

## Bạn muốn tôi làm gì tiếp theo?

A. Tạo Helper class và bắt đầu sửa từng file
B. Tạo document chi tiết hơn về từng file cần sửa
C. Tạo một branch mới và thử nghiệm approach
D. Khác (bạn chỉ định)

**Lưu ý:** Do có 48 lỗi và mỗi lỗi cần context riêng, việc sửa sẽ mất khá nhiều thời gian. Tôi khuyến nghị làm từng phần và test sau mỗi phần.
