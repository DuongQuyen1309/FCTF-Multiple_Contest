# 🎯 Tóm tắt giải pháp: Fix lỗi redirect về login

## Vấn đề

Sau khi đăng nhập thành công, khi vào trang `/contests`, ứng dụng tự động redirect về `/login`.

Từ logs:
```
[AuthContext] Token exists: false
[AuthContext] User data exists: false
[AuthContext] No valid session found
```

→ **Token không được lưu vào localStorage sau khi login**

---

## Các thay đổi đã thực hiện

### 1. ✅ Thêm logging chi tiết

**Files đã sửa:**
- `ContestantPortal/src/services/authService.ts`
- `ContestantPortal/src/context/AuthContext.tsx`
- `ContestantPortal/src/pages/Login.tsx`
- `ContestantPortal/src/services/contestService.ts`

**Mục đích**: Debug và track flow của authentication

---

### 2. ✅ Fix navigation method

**File**: `ContestantPortal/src/pages/Login.tsx`

**Thay đổi**: 
```typescript
// Trước
window.location.href = '/contests';

// Sau
navigate('/contests');
```

**Lý do**: Tránh full page reload, giữ nguyên auth state

---

### 3. ✅ Cải thiện AuthContext

**File**: `ContestantPortal/src/context/AuthContext.tsx`

**Thay đổi**:
- Kiểm tra localStorage trước khi update state
- Validate token và user data tồn tại
- Thêm error handling

---

### 4. ✅ Fix redirect loop

**File**: `ContestantPortal/src/services/api.ts`

**Thay đổi**:
```typescript
if (response.status === 401) {
  const currentPath = window.location.pathname;
  if (currentPath !== '/login' && currentPath !== '/register') {
    authService.clearSession();
    window.location.href = '/login';
  }
}
```

**Lý do**: Tránh infinite redirect loop

---

### 5. ✅ Thêm 401 handling trong contestService

**File**: `ContestantPortal/src/services/contestService.ts`

**Thay đổi**: Xử lý 401 response và redirect về login

---

### 6. ✅ Cải thiện PrivateRoute

**File**: `ContestantPortal/src/components/PrivateRoute.tsx`

**Thay đổi**: Sử dụng `PageLoader` thay vì text "Loading..."

---

### 7. ✅ Validation trong authService

**File**: `ContestantPortal/src/services/authService.ts`

**Thay đổi**: Validate response có `generatedToken` và `user` trước khi lưu

---

## Tools để debug

### 1. test-backend-login.html

File HTML standalone để test backend API trực tiếp:
- Test login endpoint
- Check backend health
- Verify response format
- Test localStorage

**Cách dùng**: Mở file trong browser, nhập credentials, click "Test Login"

---

### 2. DEBUG_LOGIN_ISSUE.md

Hướng dẫn debug chi tiết từng bước:
- Kiểm tra backend
- Kiểm tra frontend logs
- Kiểm tra localStorage
- Kiểm tra Network tab
- Các nguyên nhân có thể
- Quick fix checklist

---

## Các bước tiếp theo

### Bước 1: Kiểm tra backend có chạy không

```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run
```

Hoặc kiểm tra:
```bash
curl http://localhost:5069/api/Config/get_public_config
```

---

### Bước 2: Test login với test-backend-login.html

1. Mở `test-backend-login.html` trong browser
2. Click "Check Backend Health" → Phải thấy "Backend is UP!"
3. Nhập username/password hợp lệ
4. Click "Test Login"
5. Xem response có `generatedToken` không

**Nếu không có token**: Backend có vấn đề

**Nếu có token**: Frontend có vấn đề

---

### Bước 3: Đăng nhập qua frontend và xem logs

1. Mở DevTools Console (F12)
2. Xóa localStorage: `localStorage.clear()`
3. Đăng nhập lại
4. Xem logs trong console

**Logs mong đợi**:
```
[Login] Form submitted
[AuthService] Login request to: http://localhost:5069/api/Auth/login-contestant
[AuthService] Response status: 200
[AuthService] Token from response: exists
[AuthService] User from response: exists
[AuthService] Token saved to localStorage
[AuthContext] Token saved: true
[Login] Login successful, navigating to /contests
```

**Nếu thấy**:
- `Token from response: MISSING` → Backend không trả về token
- `Response status: 401` → Credentials sai
- `Response status: 500` → Backend error
- Network error → Backend không chạy hoặc CORS issue

---

### Bước 4: Kiểm tra localStorage

DevTools > Application > Local Storage > http://localhost:5173

Phải có:
- `auth_token`: JWT token string
- `user_info`: JSON object với user data

---

### Bước 5: Kiểm tra Network tab

DevTools > Network > Filter "Auth"

Tìm request `login-contestant`:
- Status: 200 OK
- Response có `generatedToken` và `user`

---

## Các nguyên nhân có thể

### 1. Backend không chạy ⚠️ (Khả năng cao nhất)

**Triệu chứng**:
- Console error: "Failed to fetch"
- Network tab: Request failed
- test-backend-login.html: "Cannot connect to backend"

**Giải pháp**: Start backend

---

### 2. Backend không trả về token

**Triệu chứng**:
- Response status 200
- Nhưng không có `generatedToken` trong response

**Giải pháp**: Kiểm tra backend code trong `AuthController.cs` và `AuthService.cs`

---

### 3. CORS Issue

**Triệu chứng**:
- Console error: "CORS policy"
- Network tab: CORS error

**Giải pháp**: Đã config CORS trong backend (AllowAll), nên không nên có vấn đề này

---

### 4. Credentials sai

**Triệu chứng**:
- Response status 401
- Error message: "Invalid username or password"

**Giải pháp**: Dùng credentials đúng

---

### 5. Token generation failed trong backend

**Triệu chứng**:
- Response status 500
- Backend logs có error

**Giải pháp**: Kiểm tra backend logs, có thể thiếu JWT secret key

---

## Kiểm tra nhanh

```bash
# 1. Backend có chạy không?
curl http://localhost:5069/api/Config/get_public_config

# 2. Login API có hoạt động không?
curl -X POST http://localhost:5069/api/Auth/login-contestant \
  -H "Content-Type: application/json" \
  -d '{"username":"YOUR_USERNAME","password":"YOUR_PASSWORD"}'

# Phải thấy response có "generatedToken"
```

---

## Kết luận

Với các logging đã thêm, bạn có thể dễ dàng xác định vấn đề nằm ở đâu:

1. **Backend không chạy** → Start backend
2. **Backend không trả về token** → Fix backend code
3. **Frontend không lưu token** → Đã fix với validation
4. **Race condition** → Đã fix với navigate thay vì window.location
5. **Redirect loop** → Đã fix với path checking

Hãy chạy lại và xem logs để xác định chính xác vấn đề!
