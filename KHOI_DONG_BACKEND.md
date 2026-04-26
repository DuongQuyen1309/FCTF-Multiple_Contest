# Hướng Dẫn Khởi Động Backend

## ⚠️ Thông Tin Quan Trọng

Backend của bạn chạy ở **PORT 5069**, không phải 5000!

## 🚀 Cách Khởi Động Backend

### Cách 1: Sử dụng Visual Studio (Khuyến nghị)

1. Mở file `ControlCenterAndChallengeHostingServer/ContestantBE/ContestantBE.csproj` trong Visual Studio
2. Chọn profile "http" hoặc "https" 
3. Nhấn F5 hoặc click nút "Run"
4. Backend sẽ chạy ở:
   - HTTP: `http://localhost:5069`
   - HTTPS: `https://localhost:7297`

### Cách 2: Sử dụng Command Line

```bash
# Di chuyển vào thư mục backend
cd ControlCenterAndChallengeHostingServer/ContestantBE

# Chạy backend
dotnet run --launch-profile http

# Hoặc với HTTPS
dotnet run --launch-profile https
```

### Cách 3: Sử dụng dotnet watch (Auto-reload khi code thay đổi)

```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet watch run --launch-profile http
```

## ✅ Kiểm Tra Backend Đã Chạy

### Cách 1: Mở browser
```
http://localhost:5069/swagger
```
Nếu thấy Swagger UI thì backend đã chạy thành công!

### Cách 2: Kiểm tra health endpoint
```
http://localhost:5069/api/health
```

### Cách 3: Sử dụng curl
```bash
curl http://localhost:5069/api/health
```

## 🔧 Nếu Backend Không Chạy

### Kiểm tra .NET SDK đã cài đặt chưa
```bash
dotnet --version
```
Cần .NET 6.0 hoặc cao hơn

### Kiểm tra dependencies
```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet restore
```

### Kiểm tra database connection
Đảm bảo connection string trong `appsettings.json` hoặc `.env` đúng

### Kiểm tra port có bị chiếm không
```bash
# Windows PowerShell
netstat -ano | findstr :5069

# Nếu port bị chiếm, kill process
taskkill /PID <process_id> /F
```

## 📝 Sau Khi Backend Chạy

1. Mở `test-admin-login.html` (đã cập nhật port 5069)
2. Login với tài khoản admin
3. Test các màn hình admin

## 🌐 URLs Quan Trọng

- **Backend API**: http://localhost:5069
- **Swagger UI**: http://localhost:5069/swagger
- **Health Check**: http://localhost:5069/api/health
- **Login API**: http://localhost:5069/api/auth/login
- **Frontend**: http://localhost:5173 (khi chạy)

## 🔍 Debug Backend

### Xem logs trong console
Backend sẽ hiển thị logs khi có request

### Xem logs trong Visual Studio
Output window → Show output from: Debug

### Kiểm tra appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## ⚡ Quick Start (Tóm Tắt)

```bash
# 1. Khởi động backend
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run --launch-profile http

# 2. Kiểm tra backend (trong terminal khác hoặc browser)
curl http://localhost:5069/api/health

# 3. Mở test-admin-login.html trong browser
# 4. Login và test!
```
