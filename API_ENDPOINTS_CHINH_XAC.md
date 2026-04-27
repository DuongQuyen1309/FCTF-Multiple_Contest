# 📡 API Endpoints Chính Xác

## ✅ Backend đang chạy ở: http://localhost:5069

## 🔐 Authentication Endpoints

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/Auth/login-contestant` | POST | Login (cho cả user và admin) |
| `/api/Auth/register-contestant` | POST | Đăng ký tài khoản mới |
| `/api/Auth/registration-metadata` | GET | Lấy metadata đăng ký |
| `/api/Auth/logout` | POST | Logout (cần token) |
| `/api/Auth/change-password` | POST | Đổi password (cần token) |
| `/api/Auth/select-contest` | POST | Chọn contest (cần token) |

## ⚠️ Lưu Ý Quan Trọng

### 1. Endpoint Login
- ❌ KHÔNG PHẢI: `/api/auth/login`
- ✅ ĐÚNG: `/api/Auth/login-contestant`
- Chữ "A" trong "Auth" viết HOA
- Có thêm "-contestant" ở cuối

### 2. Response Format
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

### 3. Request Format
```json
{
  "username": "admin",
  "password": "your_password"
}
```

## 🎯 Test Ngay

Bây giờ các file test đã được cập nhật:
- ✅ test-admin-login.html → Dùng endpoint đúng
- ✅ check-backend.html → Dùng endpoint đúng

**Hãy thử lại test-admin-login.html ngay bây giờ!**
