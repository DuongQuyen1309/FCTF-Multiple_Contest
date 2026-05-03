# Detailed Fix Plan - Multi-Contest Migration

## Tổng quan
Hiện tại có **48 lỗi compile** trong ContestantBE project. Tất cả đều liên quan đến việc chuyển từ single-contest sang multi-contest architecture.

## Vấn đề chính: THIẾU CONTEST CONTEXT

**Root cause:** Hầu hết các methods không có `contestId` parameter, nhưng giờ cần contestId để:
- Lấy đúng ContestsChallenge (thay vì Challenge)
- Lấy đúng Team của User trong Contest cụ thể
- Lấy đúng Solve records

## Giải pháp đề xuất

### Phase 1: Quick Fix để Build được (Khuyến nghị làm trước)

Thêm `contestId` parameter vào các methods cần thiết. Tạm thời hardcode hoặc lấy từ config.

**Ưu điểm:**
- Build được ngay
- Test được từng phần
- Dễ debug

**Nhược điểm:**
- Phải refactor lại sau
- Nhiều breaking changes

### Phase 2: Proper Implementation (Làm sau)

Implement JWT với contestId và lấy từ HttpContext.

## Chi tiết từng file cần sửa

### 1. ActionLogsServices.cs

**Lỗi:** Line 47 - `user.TeamId`

**Context:**
```csharp
public async Task<List<ActionLogsDTO>> GetActionLogs(...)
{
    // ...
    TeamId = user.TeamId  // ❌ User không còn TeamId
}
```

**Sửa:**
```csharp
// Cần biết contestId
TeamId = await _context.GetUserTeamIdInContest(user.Id, contestId)
```

**Vấn đề:** Method này không có contestId parameter!

**Giải pháp:**
1. Thêm parameter: `GetActionLogs(..., int contestId)`
2. Hoặc lấy từ JWT trong Controller

---

### 2. TeamService.cs

**Nhiều lỗi:**
- Line 54: `challenge.State` → Cần `contestChallenge.State`
- Line 97-103: `solve.ChallengeId`, `solve.Challenge` → Cần `solve.ContestChallengeId`, `solve.ContestChallenge`
- Line 112-113: `user.Team` → Cần query qua UserTeam

**Context:** Method `GetTeamInfo`

**Sửa:**
```csharp
// Thay vì query Challenge
var challenge = await _context.Challenges.FindAsync(id);
if (challenge.State != "visible") // ❌

// Phải query ContestsChallenge
var contestChallenge = await _context.ContestsChallenges
    .FirstOrDefaultAsync(cc => cc.ContestId == contestId && cc.Id == contestChallengeId);
if (contestChallenge.State != "visible") // ✅
```

---

### 3. HintService.cs

**Rất nhiều lỗi** - File này cần refactor toàn bộ

**Các lỗi:**
- `solve.ChallengeId` → `solve.ContestChallengeId`
- `user.TeamId` → Cần query
- `challenge.State` → `contestChallenge.State`
- `user.Team` → Cần query

**Chiến lược:**
1. Thêm `contestId` vào tất cả methods
2. Thay tất cả `Challenge` queries thành `ContestsChallenge`
3. Thay tất cả `user.TeamId` thành helper method calls

---

### 4. ScoreboardService.cs

**Lỗi:**
- Lines 77, 81, 105, 109: `solve.ChallengeId`, `solve.Challenge`

**Sửa:**
```csharp
// Cũ:
.Where(s => s.ChallengeId == challengeId)
.Select(s => s.Challenge.Name)

// Mới:
.Where(s => s.ContestChallengeId == contestChallengeId)
.Select(s => s.ContestChallenge.Name)
```

---

### 5. FileService.cs

**Lỗi:**
- Line 88: `challenge.State`
- Lines 119-120: `solve.ChallengeId`

**Tương tự như trên**

---

### 6. ContestService.cs

**Lỗi phức tạp:**
- Lines 327, 337: `_logger.LogWarning` - AppLogger không có method này
- Lines 347-355: `challenge.Value`, `challenge.MaxAttempts`, etc.
- Line 447: `ToHashSetAsync` không tồn tại
- Lines 562-566: Tương tự lines 347-355

**Sửa:**

```csharp
// Line 327, 337:
_logger.LogWarning(...) // ❌ AppLogger không có method này

// Có thể:
_logger.LogError(null, null, "Warning message", data: new { ... }); // ✅

// Lines 347-355:
// Thay vì query Challenge, query ContestsChallenge
var contestChallenge = await _context.ContestsChallenges
    .FirstOrDefaultAsync(cc => cc.ContestId == contestId && cc.Id == contestChallengeId);

// Line 447:
.ToHashSetAsync() // ❌ Không tồn tại

// Dùng:
var list = await query.ToListAsync();
var hashSet = list.ToHashSet(); // ✅
```

---

### 7. AuthService.cs

**Lỗi:** Line 487 - `User.TeamId = ...`

**Context:** Đang cố gán giá trị cho property không tồn tại

**Sửa:**
```csharp
// Cũ:
user.TeamId = teamId; // ❌

// Mới:
// Thêm user vào team qua UserTeam junction table
await _context.AddUserToTeam(user.Id, teamId); // ✅
```

---

### 8. TicketService.cs

**Lỗi:**
- Line 165: `user.TeamId`
- Line 185: `t.t` - Typo?

**Sửa:**
```csharp
// Line 165:
var teamId = await _context.GetUserTeamIdInContest(user.Id, contestId);

// Line 185: Cần xem context để hiểu
```

---

### 9. HintController.cs

**Lỗi:**
- Line 88: `challenge.State`
- Line 93: `solve.ChallengeId`

**Sửa:** Tương tự như Services

---

## Quyết định cần thiết

### Câu hỏi 1: ContestId từ đâu?

**Option A: Thêm vào tất cả method signatures**
```csharp
public async Task<Result> DoSomething(int userId, int contestId, ...)
```
- Ưu: Rõ ràng, dễ test
- Nhược: Breaking changes nhiều

**Option B: Lấy từ JWT Token**
```csharp
var contestId = int.Parse(User.FindFirstValue("contestId"));
```
- Ưu: Không breaking changes
- Nhược: Phải update JWT generation trước

**Option C: Inject ContestContext Service**
```csharp
public class ContestContext
{
    public int ContestId { get; set; }
}
```
- Ưu: Clean, không breaking changes
- Nhược: Phức tạp hơn

### Câu hỏi 2: Sửa tuần tự hay song song?

**Option A: Sửa từng file, test từng file**
- Ưu: An toàn, dễ debug
- Nhược: Chậm

**Option B: Sửa tất cả cùng lúc**
- Ưu: Nhanh
- Nhược: Khó debug nếu có lỗi

### Câu hỏi 3: Có cần tạo Migration APIs ngay không?

**APIs cần tạo:**
1. Pull Challenge from Bank to Contest
2. Import Contest Participants
3. List Contests
4. Select Contest

**Có nên tạo ngay hay sửa lỗi compile trước?**

## Đề xuất của tôi

### Step 1: Fix để Build được (1-2 giờ)
1. Tạo Helper methods (✅ Đã xong)
2. Thêm `contestId` parameter vào các methods cần thiết
3. Sửa tất cả lỗi compile
4. Build thành công

### Step 2: Update JWT (30 phút)
1. Thêm `contestId` vào JWT claims
2. Update AuthService để generate JWT mới

### Step 3: Tạo Migration APIs (2-3 giờ)
1. ContestController với CRUD
2. Pull Challenge API
3. Import Participants API

### Step 4: Update Frontend (3-4 giờ)
1. Contest List Page
2. Contest Selection
3. Update Sidebar
4. Update all API calls

### Step 5: Testing (1-2 giờ)
1. Test multi-contest isolation
2. Test JWT
3. Test Redis keys
4. Test deployments

**Tổng thời gian ước tính: 8-12 giờ**

## Bạn muốn tôi làm gì tiếp theo?

1. **Bắt đầu sửa tất cả lỗi compile** (Option A - Thêm contestId parameter)
2. **Tạo ContestContext Service trước** (Option C - Clean approach)
3. **Tạo document chi tiết hơn** về từng file
4. **Khác** (bạn chỉ định)

**Lưu ý:** Tôi khuyến nghị Option 1 - Sửa để build được trước, sau đó refactor dần.
