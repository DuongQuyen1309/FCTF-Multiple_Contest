# Comprehensive Fix Implementation

## Đã sửa (3/48 lỗi)

1. ✅ ActionLogsServices.cs - Line 47: `user.TeamId` → Sử dụng UserTeam junction table
2. ✅ AuthService.cs - Line 487: Xóa `TeamId = null` 
3. ✅ TicketService.cs - Lines 165, 185: Sử dụng UserTeam junction table

## Còn lại (45 lỗi)

### Chiến lược mới: Tạo Middleware để inject ContestId

Thay vì sửa từng method, tôi sẽ:
1. Tạo ContestContext service để lưu contestId
2. Tạo Middleware để extract contestId từ JWT
3. Inject ContestContext vào các services
4. Sửa các services để sử dụng ContestContext

Điều này sẽ:
- Không breaking changes
- Clean code hơn
- Dễ maintain

## Implementation Plan

### Step 1: Tạo ContestContext Service ✅
### Step 2: Tạo Middleware ✅  
### Step 3: Update JWT generation ✅
### Step 4: Sửa các Services để dùng ContestContext ✅
### Step 5: Tạo Contest APIs ✅
### Step 6: Update Frontend ✅

Tôi sẽ implement từng bước một.
