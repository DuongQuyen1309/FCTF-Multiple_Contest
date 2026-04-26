# 🔄 Restart Backend Sau Khi Sửa Code

## ✅ Đã Sửa

File: `ControlCenterAndChallengeHostingServer/ContestantBE/Services/AuthService.cs`

**Thay đổi:**
```csharp
// CŨ - Chỉ cho phép user
if (user == null || user.Type != "user")
{
    return BaseResponseDTO<AuthResponseDTO>.Fail("Invalid username or password");
}

// MỚI - Cho phép user, admin, teacher
if (user == null)
{
    return BaseResponseDTO<AuthResponseDTO>.Fail("Invalid username or password");
}

var allowedTypes = new[] { "user", "admin", "teacher" };
if (!allowedTypes.Contains(user.Type))
{
    return BaseResponseDTO<AuthResponseDTO>.Fail("Invalid username or password");
}
```

---

## 🔄 Cách Restart Backend

### **Cách 1: Dừng và chạy lại script**

1. **Tìm cửa sổ Command Prompt** đang chạy `start-backend.bat`
2. **Nhấn Ctrl+C** để dừng backend
3. **Chạy lại:** Double-click `start-backend.bat`

### **Cách 2: Chạy thủ công**

```bash
# Dừng backend hiện tại (Ctrl+C)

# Chạy lại
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run --launch-profile http
```

### **Cách 3: Trong Visual Studio**

1. **Stop** backend (Shift+F5)
2. **Start** lại (F5)

---

## ✅ Sau Khi Restart

1. **Đợi backend khởi động** (10-30 giây)
2. **Mở `test-admin-login.html`**
3. **Login với:**
   - Username: `admin`
   - Password: `Admin@123` (hoặc password bạn đã set)
4. **Kết quả:** Login thành công! ✅

---

## 🔍 Kiểm Tra Backend Đã Restart

Mở browser và truy cập:
```
http://localhost:5069/api/Auth/login-contestant
```

Nếu thấy response (dù là lỗi) thì backend đã chạy.

---

## ⚠️ Lưu Ý

- Phải **restart backend** sau khi sửa code C#
- Không cần restart nếu chỉ sửa file HTML/JavaScript
- Backend sẽ compile lại khi restart
