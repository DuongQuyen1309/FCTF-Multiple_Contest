# Hướng dẫn sửa lỗi đăng nhập (Login Issue Fix)

## Vấn đề đã phát hiện (Issues Found)

1. ❌ **Frontend đọc sai tên biến môi trường**: `VITE_API_URL` thay vì `VITE_API_BASE_URL`
2. ❌ **Endpoint API sai**: Thiếu prefix `/api/` và tên controller sai
3. ❌ **Backend đang chạy trên port 5069** thay vì port 5000

## Đã sửa (Fixed)

### 1. File `ContestantPortal/src/services/api.ts`
- ✅ Đổi từ `VITE_API_URL` → `VITE_API_BASE_URL`

### 2. File `ContestantPortal/src/config/endpoints.ts`
- ✅ Cập nhật tất cả endpoints với prefix `/api/` và tên controller đúng
- Ví dụ: `/auth/login-contestant` → `/api/Auth/login-contestant`

### 3. File `ContestantPortal/.env.local`
- ✅ Tạo file với cấu hình đúng:
```
VITE_API_BASE_URL=http://localhost:5000
VITE_CLOUDFLARE_TURNSTILE_SITE_KEY=
```

### 4. File `launchSettings.json`
- ✅ Đã cập nhật backend để chạy trên port 5000

## Các bước thực hiện (Steps to Execute)

### Bước 1: Dừng tất cả services đang chạy
```powershell
# Dừng backend (Ctrl+C trong terminal backend)
# Dừng frontend (Ctrl+C trong terminal frontend)
```

### Bước 2: Xóa cache frontend
```powershell
cd ContestantPortal

# Xóa cache Vite
rmdir /s /q node_modules\.vite
rmdir /s /q dist

# Hoặc nếu thư mục không tồn tại, bỏ qua lỗi
```

### Bước 3: Khởi động lại backend
```powershell
cd ..\ControlCenterAndChallengeHostingServer\ContestantBE
dotnet run
```

**Kiểm tra log backend phải hiển thị:**
```
Now listening on: http://localhost:5000
```

**QUAN TRỌNG**: Nếu vẫn thấy port 5069, hãy:
1. Dừng backend (Ctrl+C)
2. Kiểm tra file `Properties/launchSettings.json` đã được cập nhật chưa
3. Chạy lại `dotnet run`

### Bước 4: Khởi động lại frontend
```powershell
cd ..\..\ContestantPortal
npm run dev
```

### Bước 5: Kiểm tra trong browser
1. Mở browser và truy cập `http://localhost:5173`
2. Mở Developer Tools (F12)
3. Vào tab Console
4. Thử đăng nhập với:
   - Username: `student1`
   - Password: `test123`

### Bước 6: Kiểm tra kết quả

**✅ Thành công nếu thấy:**
- Request: `POST http://localhost:5000/api/Auth/login-contestant`
- Response: Status 200 OK với token

**❌ Vẫn lỗi nếu thấy:**
- `undefined` trong URL
- Port 5069 thay vì 5000
- 404 Not Found

## Xử lý lỗi (Troubleshooting)

### Lỗi 1: Vẫn thấy `undefined` trong URL
**Nguyên nhân**: Frontend chưa đọc được `.env.local`

**Giải pháp**:
```powershell
# Xóa hoàn toàn cache
cd ContestantPortal
rmdir /s /q node_modules\.vite
rmdir /s /q dist

# Khởi động lại
npm run dev
```

### Lỗi 2: Backend vẫn chạy trên port 5069
**Nguyên nhân**: File `launchSettings.json` chưa được cập nhật hoặc backend chưa restart

**Giải pháp**:
```powershell
# Kiểm tra file
Get-Content ControlCenterAndChallengeHostingServer\ContestantBE\Properties\launchSettings.json

# Tìm dòng "applicationUrl" phải là:
# "applicationUrl": "http://localhost:5000"

# Nếu vẫn là 5069, cập nhật thủ công rồi restart backend
```

### Lỗi 3: 404 Not Found
**Nguyên nhân**: Endpoint path không đúng

**Kiểm tra**:
- Backend log có hiển thị request không?
- URL trong browser console có đúng format `/api/Auth/login-contestant` không?

### Lỗi 4: CORS Error
**Nguyên nhân**: Backend chưa cấu hình CORS cho frontend

**Giải pháp**: Kiểm tra file `Program.cs` hoặc `Startup.cs` có cấu hình CORS cho `http://localhost:5173`

## Kiểm tra cuối cùng (Final Verification)

### 1. Kiểm tra backend đang chạy
```powershell
# Trong browser hoặc PowerShell
curl http://localhost:5000/api/Config/get_public_config
```

### 2. Kiểm tra frontend đọc được env
```javascript
// Trong browser console (F12)
console.log(import.meta.env.VITE_API_BASE_URL)
// Phải hiển thị: http://localhost:5000
```

### 3. Kiểm tra database có user
```powershell
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT id, name, email, verified, banned FROM users WHERE name='student1';"
```

**Kết quả mong đợi**:
```
+----+----------+-------------------+----------+--------+
| id | name     | email             | verified | banned |
+----+----------+-------------------+----------+--------+
|  5 | student1 | student1@test.com |        1 |      0 |
+----+----------+-------------------+----------+--------+
```

## Tóm tắt các thay đổi (Summary of Changes)

| File | Thay đổi | Lý do |
|------|----------|-------|
| `ContestantPortal/src/services/api.ts` | `VITE_API_URL` → `VITE_API_BASE_URL` | Khớp với tên biến trong `.env.local` |
| `ContestantPortal/src/config/endpoints.ts` | Thêm `/api/` prefix và sửa tên controller | Khớp với routing của backend ASP.NET Core |
| `ContestantPortal/.env.local` | Tạo file mới với port 5000 | Cấu hình đúng API base URL |
| `launchSettings.json` | Port 5069 → 5000 | Thống nhất port giữa frontend và backend |

## Lưu ý quan trọng (Important Notes)

1. **Luôn restart cả backend và frontend** sau khi thay đổi cấu hình
2. **Xóa cache Vite** nếu thay đổi file `.env.local`
3. **Hard refresh browser** (Ctrl+Shift+R) sau khi restart frontend
4. **Kiểm tra backend log** để đảm bảo đang chạy đúng port
5. **Kiểm tra browser console** để xem request URL có đúng không

## Nếu vẫn không được (If Still Not Working)

Cung cấp thông tin sau:
1. Backend log khi khởi động (đặc biệt là dòng "Now listening on...")
2. Browser console log khi đăng nhập
3. Network tab trong Developer Tools (F12) - request URL và response
4. Kết quả của lệnh: `Get-Content ContestantPortal\.env.local`
