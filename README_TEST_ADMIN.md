# 🎯 Hướng Dẫn Test Màn Hình Admin - Đầy Đủ

## 📋 Tổng Quan

Bạn có 6 file công cụ để test màn hình admin:

1. **check-backend.html** - Kiểm tra backend có chạy không ⭐ **BẮT ĐẦU TỪ ĐÂY**
2. **start-backend.bat** - Script khởi động backend (Windows)
3. **start-backend.ps1** - Script khởi động backend (PowerShell)
4. **test-admin-login.html** - Test login với tài khoản admin
5. **test-admin-screens.html** - Test tất cả màn hình admin
6. **create-admin-user.sql** - Script tạo tài khoản admin trong database

## 🚀 Quy Trình Test (Từng Bước)

### Bước 1: Kiểm Tra Backend ⭐ **BẮT ĐẦU TỪ ĐÂY**

```
1. Mở file: check-backend.html trong browser
2. Xem kết quả:
   - ✅ Backend Đang Chạy → Chuyển sang Bước 3
   - ❌ Backend Chưa Chạy → Chuyển sang Bước 2
```

### Bước 2: Khởi Động Backend (Nếu Chưa Chạy)

#### Cách 1: Dùng Script (Dễ nhất)
```bash
# Double-click file này:
start-backend.bat

# Hoặc trong PowerShell:
.\start-backend.ps1
```

#### Cách 2: Thủ Công
```bash
# Mở PowerShell hoặc Command Prompt
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run --launch-profile http
```

#### Cách 3: Visual Studio
```
1. Mở file ContestantBE.csproj trong Visual Studio
2. Chọn profile "http"
3. Nhấn F5
```

**Đợi backend khởi động (10-30 giây), sau đó quay lại Bước 1 để kiểm tra.**

### Bước 3: Tạo Tài Khoản Admin

#### Cách 1: Cập nhật user hiện có (Khuyến nghị)
```sql
-- Mở SQL Server Management Studio hoặc Azure Data Studio
-- Chạy query này:

UPDATE Users 
SET Type = 'admin' 
WHERE Username = 'your_username';  -- Thay bằng username thực tế

-- Kiểm tra:
SELECT Id, Username, Email, Type FROM Users WHERE Type = 'admin';
```

#### Cách 2: Dùng file SQL
```
1. Mở file: create-admin-user.sql
2. Đọc hướng dẫn trong file
3. Chạy các query phù hợp
```

### Bước 4: Test Login Admin

```
1. Mở file: test-admin-login.html trong browser
2. Nhập username và password của admin
3. Click "Login as Admin"
4. Kiểm tra kết quả:
   - ✅ Login Admin Thành Công → Chuyển sang Bước 5
   - ❌ Login Thất Bại → Kiểm tra lại username/password
   - ❌ Tài khoản không phải Admin → Quay lại Bước 3
```

### Bước 5: Test Các Màn Hình Admin

```
1. Mở file: test-admin-screens.html trong browser
2. Click "Kiểm Tra Auth" để xác nhận đã login
3. Click vào từng màn hình để test:
   - Contest List
   - Create Contest
   - Pull Challenges
   - Import Participants
   - Instances
   - Action Logs
   - Challenges
   - Scoreboard
   - Tickets
   - Profile
4. Đánh dấu checklist khi test xong mỗi màn hình
```

## 📊 Checklist Test Admin

- [ ] Backend đang chạy ở http://localhost:5069
- [ ] Có tài khoản với type = "admin" trong database
- [ ] Login thành công với tài khoản admin
- [ ] User info có type: "admin"
- [ ] Token được lưu vào localStorage
- [ ] Truy cập được Contest List
- [ ] Thấy nút "Create New Contest"
- [ ] Truy cập được Create Contest
- [ ] Truy cập được Pull Challenges
- [ ] Truy cập được Import Participants
- [ ] Truy cập được Instances
- [ ] Truy cập được Action Logs
- [ ] User thường KHÔNG truy cập được các màn hình admin

## 🌐 URLs Quan Trọng

| Service | URL |
|---------|-----|
| Backend API | http://localhost:5069 |
| Swagger UI | http://localhost:5069/swagger |
| Login API | http://localhost:5069/api/Auth/login-contestant |
| Frontend | http://localhost:5173 |

## 🔧 Troubleshooting

### Lỗi: ERR_CONNECTION_REFUSED
```
Nguyên nhân: Backend chưa chạy
Giải pháp: Quay lại Bước 2 để khởi động backend
```

### Lỗi: Login Thất Bại
```
Nguyên nhân: Username/password sai hoặc user không tồn tại
Giải pháp: 
1. Kiểm tra username/password
2. Kiểm tra user có tồn tại trong database không
```

### Lỗi: Tài khoản không phải Admin
```
Nguyên nhân: User type không phải "admin"
Giải pháp: Quay lại Bước 3 để cập nhật type = 'admin'
```

### Lỗi: Token không được lưu
```
Nguyên nhân: Backend không trả về token hoặc user
Giải pháp:
1. Mở DevTools (F12) → Console
2. Xem logs chi tiết
3. Kiểm tra response từ API
```

### Backend không khởi động được
```
Nguyên nhân: .NET SDK chưa cài hoặc dependencies thiếu
Giải pháp:
1. Kiểm tra .NET SDK: dotnet --version
2. Restore dependencies: dotnet restore
3. Kiểm tra connection string trong appsettings.json
```

### Port 5069 bị chiếm
```
Nguyên nhân: Có process khác đang dùng port 5069
Giải pháp:
# PowerShell
netstat -ano | findstr :5069
taskkill /PID <process_id> /F
```

## 💡 Tips

1. **Luôn mở DevTools (F12)** khi test để xem logs chi tiết
2. **Kiểm tra Console** để debug lỗi
3. **Kiểm tra Network tab** để xem request/response
4. **Kiểm tra localStorage** để xem token và user info
5. **Test với user thường** để đảm bảo phân quyền hoạt động đúng

## 📁 Cấu Trúc Files

```
.
├── check-backend.html          ⭐ BẮT ĐẦU TỪ ĐÂY
├── start-backend.bat           Script khởi động (Windows)
├── start-backend.ps1           Script khởi động (PowerShell)
├── test-admin-login.html       Test login admin
├── test-admin-screens.html     Test màn hình admin
├── create-admin-user.sql       Script tạo admin
├── KHOI_DONG_BACKEND.md        Hướng dẫn khởi động backend
├── HUONG_DAN_TEST_ADMIN.md     Hướng dẫn test chi tiết
└── README_TEST_ADMIN.md        File này
```

## 🎓 Workflow Tóm Tắt

```
1. check-backend.html
   ↓ (Nếu backend chưa chạy)
2. start-backend.bat
   ↓ (Đợi backend khởi động)
3. create-admin-user.sql
   ↓ (Tạo/cập nhật admin)
4. test-admin-login.html
   ↓ (Login với admin)
5. test-admin-screens.html
   ↓ (Test từng màn hình)
6. ✅ HOÀN THÀNH!
```

## 📞 Cần Giúp Đỡ?

Nếu gặp vấn đề:
1. Kiểm tra console logs (F12)
2. Kiểm tra backend logs
3. Đọc phần Troubleshooting ở trên
4. Kiểm tra file HUONG_DAN_TEST_ADMIN.md để biết thêm chi tiết

## ⚠️ Lưu Ý Quan Trọng

1. **Backend phải chạy ở port 5069** (không phải 5000)
2. **User type phải là "admin"** (lowercase, không phải "Admin")
3. **Token phải được lưu vào localStorage** sau khi login
4. **Mở DevTools** để xem logs chi tiết khi test
5. **Test với user thường** để đảm bảo phân quyền hoạt động

---

**Chúc bạn test thành công! 🎉**
