# 🔍 Debug Login Issue - Step by Step

## Vấn đề hiện tại

Sau khi đăng nhập, token không được lưu vào localStorage:
```
[AuthContext] Token exists: false
[AuthContext] User data exists: false
```

## Các bước debug

### Bước 1: Kiểm tra Backend có đang chạy không

```bash
# Kiểm tra backend
curl http://localhost:5069/api/Config/get_public_config
```

Hoặc mở file `test-backend-login.html` trong browser và click "Check Backend Health".

**Kết quả mong đợi**: Backend trả về response 200 OK

**Nếu lỗi**: 
- Backend chưa chạy → Start backend
- Port sai → Kiểm tra port trong launchSettings.json
- CORS issue → Kiểm tra CORS configuration trong backend

---

### Bước 2: Test Login API trực tiếp

Mở file `test-backend-login.html` trong browser:

1. Nhập username và password (credentials hợp lệ)
2. Click "Test Login"
3. Xem log chi tiết

**Kết quả mong đợi**:
```json
{
  "generatedToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "testuser",
    "email": "test@example.com",
    "type": "user",
    "contestId": 0,
    "team": null
  }
}
```

**Nếu không có `generatedToken`**: Backend có vấn đề trong việc generate token

**Nếu request failed**: 
- CORS issue
- Backend không chạy
- Network issue

---

### Bước 3: Kiểm tra Frontend Logs

Mở DevTools Console (F12) và đăng nhập lại. Bạn sẽ thấy các log:

```
[Login] Form submitted
[Login] Username: <username>
[Login] Calling login function...
[AuthService] Login request to: http://localhost:5069/api/Auth/login-contestant
[AuthService] Response status: 200
[AuthService] Login response data: {...}
[AuthService] Token from response: exists
[AuthService] User from response: exists
[AuthService] Token saved to localStorage
[AuthService] User saved to localStorage
[AuthContext] Login attempt for user: <username>
[AuthContext] Token saved: true
[AuthContext] User saved: true
[Login] Login successful, navigating to /contests
```

**Nếu thấy**:
- `[AuthService] Token from response: MISSING` → Backend không trả về token
- `[AuthService] ERROR: No token in response!` → Backend response thiếu token
- Request failed → Network/CORS issue

---

### Bước 4: Kiểm tra localStorage

Sau khi login, mở DevTools > Application > Local Storage > http://localhost:5173

Kiểm tra:
- `auth_token`: Phải có giá trị (JWT token string)
- `user_info`: Phải có JSON object

**Nếu không có**: Token không được lưu → Có lỗi trong authService.setToken()

---

### Bước 5: Kiểm tra Network Tab

Mở DevTools > Network tab, filter "Auth", đăng nhập lại.

Tìm request `login-contestant`:
- **Status**: Phải là 200 OK
- **Response**: Phải có `generatedToken` và `user`
- **Headers**: Kiểm tra CORS headers

**Nếu status 401**: Credentials sai
**Nếu status 500**: Backend error
**Nếu status 0 hoặc CORS error**: CORS configuration issue

---

## Các nguyên nhân có thể

### 1. Backend không chạy
```bash
# Start backend
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run
```

### 2. CORS Issue

Kiểm tra file `Program.cs` hoặc `Startup.cs` trong backend:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ...

app.UseCors("AllowFrontend");
```

### 3. Backend không trả về token

Kiểm tra `AuthController.cs` dòng 38-42:

```csharp
return Ok(new
{
    generatedToken = result.Data.token,  // ← Phải có
    user = result.Data
});
```

Kiểm tra `AuthService.cs` dòng 638-645:

```csharp
var authResponse = new AuthResponseDTO
{
    // ...
    token = jwt  // ← Phải được set
};
```

### 4. Frontend API URL sai

Kiểm tra `.env.local`:
```
VITE_API_BASE_URL=http://localhost:5069
```

Restart frontend sau khi thay đổi .env:
```bash
npm run dev
```

### 5. Token generation failed

Kiểm tra backend logs khi login. Nếu thấy error trong `GenerateUserToken`, có thể:
- JWT secret key chưa được config
- Token helper có lỗi

---

## Quick Fix Checklist

- [ ] Backend đang chạy trên port 5069
- [ ] Frontend đang chạy trên port 5173
- [ ] CORS được config đúng trong backend
- [ ] `.env.local` có `VITE_API_BASE_URL=http://localhost:5069`
- [ ] Credentials đúng (username/password hợp lệ)
- [ ] Browser console không có CORS errors
- [ ] Network tab shows 200 OK for login request
- [ ] Response có `generatedToken` field
- [ ] localStorage có thể write (không bị block bởi browser)

---

## Test với curl

```bash
# Test login API
curl -X POST http://localhost:5069/api/Auth/login-contestant \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"password123"}'
```

**Kết quả mong đợi**:
```json
{
  "generatedToken": "eyJ...",
  "user": {...}
}
```

---

## Nếu vẫn không work

1. Xóa toàn bộ localStorage: `localStorage.clear()`
2. Hard refresh browser: Ctrl+Shift+R (Windows) hoặc Cmd+Shift+R (Mac)
3. Restart backend
4. Restart frontend
5. Thử với browser khác (Chrome, Firefox, Edge)
6. Kiểm tra backend logs chi tiết
7. Thử login với Postman/Insomnia để loại trừ frontend issue

---

## Contact

Nếu vẫn gặp vấn đề, cung cấp:
1. Screenshot của Console logs
2. Screenshot của Network tab (login request)
3. Backend logs khi login
4. Response từ `test-backend-login.html`
