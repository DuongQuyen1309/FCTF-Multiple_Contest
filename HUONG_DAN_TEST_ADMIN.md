# Hướng Dẫn Test Màn Hình Admin

## 1. Chuẩn Bị

### Kiểm tra Backend đang chạy
```bash
# Backend phải chạy ở port 5000
curl http://localhost:5000/api/health
```

### Kiểm tra Frontend đang chạy
```bash
# Frontend phải chạy ở port 5173 (hoặc port khác)
cd ContestantPortal
npm run dev
```

## 2. Tạo Tài Khoản Admin Trong Database

Bạn cần có một user với `type = "admin"` trong database. Có 2 cách:

### Cách 1: Sử dụng SQL trực tiếp
```sql
-- Kiểm tra user hiện có
SELECT Id, Username, Email, Type FROM Users;

-- Tạo user admin mới (nếu chưa có)
INSERT INTO Users (Username, Email, PasswordHash, Type, CreatedAt)
VALUES ('admin', 'admin@test.com', 'hashed_password_here', 'admin', GETDATE());

-- Hoặc cập nhật user hiện có thành admin
UPDATE Users 
SET Type = 'admin' 
WHERE Username = 'your_username';
```

### Cách 2: Sử dụng API endpoint (nếu có)
Kiểm tra xem backend có endpoint để tạo admin không.

## 3. Test Login Admin

### Sử dụng file test-admin-login.html

Tôi đã tạo file `test-admin-login.html` cho bạn. Mở file này trong browser và:

1. Nhập username của admin (ví dụ: `admin`)
2. Nhập password
3. Click "Login as Admin"
4. Kiểm tra console để xem response

### Kiểm tra Response

Response thành công sẽ có dạng:
```json
{
  "generatedToken": "eyJhbGc...",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@test.com",
    "type": "admin",
    "team": null
  }
}
```

**Quan trọng**: Trường `type` phải là `"admin"`

## 4. Test Các Màn Hình Admin

### Màn hình chỉ dành cho Admin:

1. **Contest List** (`/contests`)
   - Admin có thể tạo contest mới
   - Có nút "Create New Contest"

2. **Create Contest** (`/contest/create`)
   - Form tạo contest mới
   - Chỉ admin mới thấy

3. **Pull Challenges** (`/contest/:contestId/pull-challenges`)
   - Import challenges từ CTFd
   - Chỉ admin mới có quyền

4. **Import Participants** (`/contest/:contestId/import-participants`)
   - Import danh sách participants
   - Chỉ admin mới có quyền

5. **Instances Management** (`/contest/:contestId/instances`)
   - Quản lý challenge instances
   - Admin có thể xem tất cả instances

6. **Action Logs** (`/contest/:contestId/action-logs`)
   - Xem logs của hệ thống
   - Chỉ admin mới thấy

### Cách Test Từng Màn Hình:

#### A. Test Contest List
```
1. Login với tài khoản admin
2. Truy cập: http://localhost:5173/contests
3. Kiểm tra:
   - Có nút "Create New Contest" không?
   - Có thể xem danh sách contests không?
```

#### B. Test Create Contest
```
1. Từ Contest List, click "Create New Contest"
2. Hoặc truy cập: http://localhost:5173/contest/create
3. Kiểm tra:
   - Form hiển thị đầy đủ không?
   - Có thể tạo contest mới không?
```

#### C. Test Pull Challenges
```
1. Chọn một contest
2. Truy cập: http://localhost:5173/contest/1/pull-challenges
3. Kiểm tra:
   - Form nhập CTFd URL hiển thị không?
   - Có thể pull challenges không?
```

#### D. Test Import Participants
```
1. Chọn một contest
2. Truy cập: http://localhost:5173/contest/1/import-participants
3. Kiểm tra:
   - Form upload CSV hiển thị không?
   - Có thể import participants không?
```

#### E. Test Instances
```
1. Chọn một contest
2. Truy cập: http://localhost:5173/contest/1/instances
3. Kiểm tra:
   - Danh sách instances hiển thị không?
   - Admin có thể xem tất cả instances không?
```

#### F. Test Action Logs
```
1. Chọn một contest
2. Truy cập: http://localhost:5173/contest/1/action-logs
3. Kiểm tra:
   - Logs hiển thị không?
   - Có filter và search không?
```

## 5. Test Phân Quyền

### Test với User Thường (type = "user")
```
1. Login với tài khoản user thường
2. Thử truy cập các URL admin:
   - /contest/create → Nên bị chặn hoặc không hiển thị
   - /contest/1/pull-challenges → Nên bị chặn
   - /contest/1/import-participants → Nên bị chặn
```

### Kiểm tra trong Layout/Navigation
```
1. Login với admin → Kiểm tra menu có các option admin không
2. Login với user → Kiểm tra menu không có các option admin
```

## 6. Debug Khi Có Lỗi

### Kiểm tra Console
```javascript
// Mở DevTools (F12) và kiểm tra:
localStorage.getItem('auth_token')  // Phải có token
localStorage.getItem('user_info')   // Phải có user info với type: "admin"
```

### Kiểm tra Network Tab
```
1. Mở DevTools → Network tab
2. Login lại
3. Kiểm tra request POST /api/auth/login
4. Xem response có đúng không
```

### Kiểm tra Backend Logs
```bash
# Xem logs của backend để debug
# Kiểm tra xem API có trả về đúng user type không
```

## 7. Checklist Test Admin

- [ ] Login thành công với tài khoản admin
- [ ] User info có `type: "admin"`
- [ ] Token được lưu vào localStorage
- [ ] Truy cập được `/contests`
- [ ] Thấy nút "Create New Contest"
- [ ] Truy cập được `/contest/create`
- [ ] Truy cập được `/contest/:id/pull-challenges`
- [ ] Truy cập được `/contest/:id/import-participants`
- [ ] Truy cập được `/contest/:id/instances`
- [ ] Truy cập được `/contest/:id/action-logs`
- [ ] User thường KHÔNG truy cập được các màn hình admin

## 8. Lưu Ý Quan Trọng

1. **Type phải chính xác**: `"admin"` (lowercase, không phải "Admin" hay "ADMIN")
2. **Token phải hợp lệ**: Kiểm tra token không bị expired
3. **Backend phải hỗ trợ**: API phải trả về trường `type` trong user object
4. **Frontend chưa có guard**: Hiện tại PrivateRoute chỉ check authentication, chưa check role. Bạn có thể cần thêm AdminRoute component.

## 9. Nếu Cần Thêm AdminRoute Component

Nếu bạn muốn bảo vệ routes admin tốt hơn, tôi có thể tạo component AdminRoute:

```typescript
// Sẽ check cả isAuthenticated VÀ user.type === 'admin'
<AdminRoute>
  <CreateContest />
</AdminRoute>
```

Bạn có muốn tôi tạo component này không?
