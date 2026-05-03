# 🎯 HƯỚNG DẪN TEST ADMIN SAU KHI FIX LỖI TEAM_ID

## ✅ ĐÃ FIX XONG

Đã sửa lỗi `AttributeError: 'Admins' object has no attribute 'team_id'` trong các file:
1. ✅ `FCTF-ManagementPlatform/CTFd/utils/user/__init__.py` (2 chỗ)
2. ✅ `FCTF-ManagementPlatform/CTFd/DeployHistory.py` (4 chỗ)

## 📋 BƯỚC 1: KHỞI ĐỘNG LẠI MANAGEMENTPLATFORM

### Cách 1: Dùng script (Khuyến nghị)
```bash
# Double-click file này:
start-management-platform.bat
```

### Cách 2: Chạy thủ công
```bash
cd FCTF-ManagementPlatform
python serve.py
```

**Chờ đến khi thấy:**
```
* Running on http://127.0.0.1:8000
* Running on http://192.168.x.x:8000
```

## 📋 BƯỚC 2: ĐĂNG NHẬP VỚI ADMIN

### 1. Mở trình duyệt
```
http://localhost:8000/login
```

### 2. Nhập thông tin đăng nhập
- **Username**: `admin`
- **Password**: `Admin@123` (hoặc password bạn đã set trong database)

### 3. Click nút "Login"

## ✅ KẾT QUẢ MONG ĐỢI

### ✅ THÀNH CÔNG nếu:
1. **Không có lỗi** trong console của trình duyệt
2. **Được redirect** đến trang admin challenges:
   ```
   http://localhost:8000/admin/challenges
   ```
3. **Thấy giao diện** admin với menu bên trái:
   - Challenges
   - Submissions
   - Users
   - Teams
   - Scoreboard
   - Pages
   - Notifications
   - Config
   - ...

### ❌ THẤT BẠI nếu:
1. Vẫn thấy lỗi `AttributeError: 'Admins' object has no attribute 'team_id'`
2. Trang bị trắng hoặc lỗi 500
3. Không được redirect đến trang admin

## 📋 BƯỚC 3: TEST CÁC CHỨC NĂNG ADMIN

### Test 1: Xem danh sách Challenges
```
http://localhost:8000/admin/challenges
```
- ✅ Thấy danh sách challenges (có thể rỗng nếu chưa có data)
- ✅ Không có lỗi

### Test 2: Xem danh sách Users
```
http://localhost:8000/admin/users
```
- ✅ Thấy danh sách users
- ✅ Thấy tài khoản admin của bạn

### Test 3: Xem Deploy History (nếu có challenges)
```
http://localhost:8000/deploy_History/<challenge_id>
```
- ✅ Không có lỗi team_id
- ✅ Có thể xem logs

### Test 4: Xem Profile
```
http://localhost:8000/settings
```
- ✅ Thấy thông tin profile của admin
- ✅ Không có lỗi

## 🔍 KIỂM TRA LOGS

### Trong terminal chạy ManagementPlatform:
```bash
# Nếu thành công, sẽ thấy:
[2026-04-26 XX:XX:XX] INFO - admin logged in
[2026-04-26 XX:XX:XX] INFO - GET /admin/challenges 200

# Nếu thất bại, sẽ thấy:
[2026-04-26 XX:XX:XX] ERROR - AttributeError: 'Admins' object has no attribute 'team_id'
```

### Trong browser console (F12):
```javascript
// Nếu thành công:
// Không có lỗi màu đỏ

// Nếu thất bại:
// POST http://localhost:8000/login 500 (INTERNAL SERVER ERROR)
```

## 🐛 NẾU VẪN CÒN LỖI

### 1. Kiểm tra file đã được sửa chưa
```bash
# Kiểm tra file utils/user/__init__.py
cd FCTF-ManagementPlatform/CTFd/utils/user
grep "getattr(user, field, None)" __init__.py

# Nếu thấy dòng này → ✅ Đã fix
# Nếu không thấy → ❌ Chưa fix
```

### 2. Kiểm tra cache
```bash
# Xóa cache Python
cd FCTF-ManagementPlatform
find . -type d -name "__pycache__" -exec rm -rf {} +
find . -type f -name "*.pyc" -delete

# Hoặc trên Windows:
Get-ChildItem -Path . -Include __pycache__ -Recurse -Force | Remove-Item -Force -Recurse
Get-ChildItem -Path . -Include *.pyc -Recurse -Force | Remove-Item -Force
```

### 3. Khởi động lại server
```bash
# Ctrl+C để dừng server
# Chạy lại:
python serve.py
```

### 4. Kiểm tra database
```sql
-- Kiểm tra tài khoản admin
SELECT Id, Username, Email, Type, Password 
FROM Users 
WHERE Username = 'admin';

-- Kết quả mong đợi:
-- Id: 2
-- Username: admin
-- Email: admin@test.com
-- Type: admin
-- Password: $bcrypt-sha256$v=2,t=2b,r=10$...
```

## 📝 GHI CHÚ

### Tại sao cần fix này?
- Admin users **KHÔNG có** thuộc tính `team_id`
- Chỉ có contestants (type='user') mới có `team_id`
- Code cũ cố truy cập `user.team_id` cho tất cả users → Lỗi với admin

### Fix đã làm gì?
- Dùng `getattr(user, field, None)` thay vì `getattr(user, field)`
- Thêm check `hasattr(user, 'team_id')` trước khi truy cập
- Đảm bảo admin có thể đăng nhập và sử dụng hệ thống bình thường

## 🔗 TÀI LIỆU LIÊN QUAN

- `FIX_ADMIN_TEAM_ID_ERROR.md` - Chi tiết kỹ thuật về fix
- `test_admin_team_id_fix.py` - Script test logic của fix
- `HUONG_DAN_LOGIN_ADMIN_DUNG.md` - Hướng dẫn login admin

## 📞 HỖ TRỢ

Nếu vẫn gặp vấn đề:
1. Kiểm tra logs trong terminal
2. Kiểm tra console trong browser (F12)
3. Đọc file `FIX_ADMIN_TEAM_ID_ERROR.md` để hiểu rõ hơn
4. Chạy script test: `python test_admin_team_id_fix.py`

---
**Ngày tạo**: 2026-04-26
**Status**: ✅ SẴN SÀNG TEST
