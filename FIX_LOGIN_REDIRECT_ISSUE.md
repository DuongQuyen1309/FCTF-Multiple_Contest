# Fix: Vấn đề redirect về trang login sau khi đăng nhập thành công

## Vấn đề

Sau khi đăng nhập thành công, khi điều hướng đến trang `/contests`, ứng dụng tự động redirect về trang `/login`.

## Nguyên nhân

Có 3 nguyên nhân chính có thể gây ra vấn đề này:

### 1. Race Condition trong Authentication Flow
- **Vấn đề**: Sử dụng `window.location.href` để điều hướng sau login gây ra full page reload
- **Hậu quả**: AuthContext có thể chưa kịp load token từ localStorage trước khi PrivateRoute kiểm tra authentication
- **File**: `ContestantPortal/src/pages/Login.tsx`

### 2. API Call trả về 401 Unauthorized
- **Vấn đề**: Ngay sau khi login, nếu có API call nào trả về 401, code sẽ tự động clear session và redirect về login
- **Hậu quả**: Token mới có thể chưa được backend nhận diện hoặc đã hết hạn
- **File**: `ContestantPortal/src/services/api.ts`, `ContestantPortal/src/services/contestService.ts`

### 3. Redirect Loop
- **Vấn đề**: Khi đã ở trang login mà vẫn bị redirect về login
- **Hậu quả**: Tạo infinite loop
- **File**: `ContestantPortal/src/services/api.ts`

## Các thay đổi đã thực hiện

### 1. ✅ Thay đổi navigation method trong Login.tsx

**Trước:**
```typescript
window.location.href = '/contests';
```

**Sau:**
```typescript
navigate('/contests');
```

**Lý do**: Sử dụng React Router's `navigate` giữ nguyên state của ứng dụng, tránh full page reload.

---

### 2. ✅ Cải thiện logic trong AuthContext.tsx

**Thay đổi**:
- Kiểm tra localStorage **trước** khi update state
- Đảm bảo token và user data đã được lưu thành công
- Thêm logging để debug

**Lý do**: Đảm bảo state được update đồng bộ với localStorage.

---

### 3. ✅ Thêm safeguard trong api.ts

**Thay đổi**:
```typescript
if (response.status === 401) {
  // Only redirect if we're not already on the login page
  const currentPath = window.location.pathname;
  if (currentPath !== '/login' && currentPath !== '/register') {
    authService.clearSession();
    window.location.href = '/login';
  }
}
```

**Lý do**: Tránh redirect loop khi đã ở trang login.

---

### 4. ✅ Thêm xử lý 401 trong contestService.ts

**Thay đổi**:
- Thêm logging để track API calls
- Xử lý 401 response và redirect về login
- Log token status

**Lý do**: `contestService` không sử dụng `fetchWithAuth`, cần xử lý 401 riêng.

---

### 5. ✅ Cải thiện PrivateRoute.tsx

**Thay đổi**:
- Sử dụng `PageLoader` component thay vì text "Loading..."

**Lý do**: UX tốt hơn khi đang kiểm tra authentication.

---

## Cách debug

### Bước 1: Mở Browser Console

Khi bạn đăng nhập, bạn sẽ thấy các log như sau:

```
[AuthContext] Login attempt for user: <username>
[AuthContext] Login response received: {...}
[AuthContext] Token saved: true
[AuthContext] User saved: true
[AuthContext] Auth state updated successfully
```

### Bước 2: Kiểm tra localStorage

Mở DevTools > Application > Local Storage và kiểm tra:
- `auth_token`: Phải có giá trị (JWT token)
- `user_info`: Phải có object chứa thông tin user

### Bước 3: Kiểm tra API calls

Khi vào trang `/contests`, bạn sẽ thấy:

```
[AuthContext] Initializing auth state...
[AuthContext] Token exists: true
[AuthContext] User data exists: true
[AuthContext] User authenticated: <username>
[ContestService] Fetching contests with token: exists
[ContestService] Response status: 200
[ContestService] Contests loaded: <number>
```

### Bước 4: Nếu vẫn bị redirect

Nếu bạn thấy log:
```
[ContestService] Response status: 401
[ContestService] Unauthorized - redirecting to login
```

**Có nghĩa là**: Backend không nhận diện token. Kiểm tra:

1. **Token format**: Kiểm tra xem token có đúng format JWT không
2. **Token expiry**: Token có thể đã hết hạn ngay sau khi tạo
3. **Backend validation**: Backend có thể có vấn đề trong việc validate token

### Bước 5: Kiểm tra Backend

Kiểm tra backend logs để xem:
- Token có được gửi đúng trong header không
- Backend có validate token thành công không
- Có lỗi gì trong quá trình validate không

## Các bước tiếp theo nếu vẫn gặp vấn đề

### 1. Kiểm tra token expiry time

Trong backend, kiểm tra xem token có thời gian hết hạn quá ngắn không (ví dụ: 1 giây).

### 2. Kiểm tra CORS

Đảm bảo backend cho phép frontend gửi Authorization header.

### 3. Kiểm tra token format

Đảm bảo token được trả về từ backend có format đúng:
```json
{
  "generatedToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "user123",
    ...
  }
}
```

### 4. Kiểm tra contestId trong token

API `/api/Contest/list` có thể yêu cầu `contestId` trong token. Nếu token chưa có `contestId`, API sẽ trả về 401.

**Giải pháp**: Cho phép API `/api/Contest/list` hoạt động mà không cần `contestId` trong token.

## Test

1. Xóa localStorage: `localStorage.clear()`
2. Đăng nhập lại
3. Kiểm tra console logs
4. Kiểm tra localStorage
5. Kiểm tra Network tab trong DevTools

## Kết luận

Các fix đã được áp dụng sẽ giải quyết hầu hết các vấn đề về race condition và redirect loop. Nếu vẫn gặp vấn đề, nguyên nhân có thể nằm ở backend (token validation, expiry time, hoặc yêu cầu contestId).
