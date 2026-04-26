# 🎯 Hướng Dẫn Login Admin ĐÚNG

## ✅ QUAN TRỌNG: Có 2 Hệ Thống Riêng Biệt!

### 1. **FCTF-ManagementPlatform** (Python/Flask/CTFd)
- **Mục đích:** Dành cho Admin, Teacher, Jury
- **Port:** 8000 (hoặc 4000)
- **Login URL:** `http://localhost:8000/login`
- **Cho phép:** admin, teacher, jury
- **KHÔNG cho phép:** user (contestants)

### 2. **ContestantBE** (C#/.NET)
- **Mục đích:** Dành cho Contestants (user)
- **Port:** 5069
- **Login API:** `http://localhost:5069/api/Auth/login-contestant`
- **Cho phép:** user
- **KHÔNG cho phép:** admin, teacher (ban đầu)

---

## 🚀 Cách Login Admin ĐÚNG

### **Option 1: Sử dụng ManagementPlatform (Khuyến nghị cho Admin)**

#### Bước 1: Khởi động ManagementPlatform

```bash
cd FCTF-ManagementPlatform
python serve.py
```

Hoặc:

```bash
cd FCTF-ManagementPlatform
python wsgi.py
```

#### Bước 2: Truy cập trang login

Mở browser và truy cập:
```
http://localhost:8000/login
```

Hoặc:
```
http://localhost:4000/login
```

#### Bước 3: Login

- **Username:** `admin` (hoặc email)
- **Password:** Password của bạn trong database

#### Bước 4: Sau khi login thành công

Bạn sẽ được redirect đến:
```
http://localhost:8000/admin/challenges
```

---

### **Option 2: Sử dụng ContestantBE (Nếu muốn test với frontend React)**

Nếu bạn muốn admin login vào ContestantPortal (React frontend), cần:

#### Bước 1: Đã sửa code (đã làm rồi)

File `AuthService.cs` đã được sửa để cho phép admin login.

#### Bước 2: Khởi động ContestantBE

```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run --launch-profile http
```

#### Bước 3: Test login

Mở `test-admin-login.html` và login với:
- **Username:** `admin`
- **Password:** `Admin@123`

---

## 📋 So Sánh 2 Hệ Thống

| Tính năng | ManagementPlatform | ContestantBE |
|-----------|-------------------|--------------|
| **Ngôn ngữ** | Python/Flask | C#/.NET |
| **Port** | 8000 hoặc 4000 | 5069 |
| **Frontend** | Jinja2 templates | React (ContestantPortal) |
| **Cho phép login** | admin, teacher, jury | user (và admin sau khi sửa) |
| **Mục đích** | Quản lý challenges, contests | Portal cho contestants |
| **Database** | Chung | Chung |

---

## 🔍 Kiểm Tra Hệ Thống Nào Đang Chạy

### Kiểm tra ManagementPlatform:
```bash
curl http://localhost:8000/login
```

Hoặc:
```bash
curl http://localhost:4000/login
```

### Kiểm tra ContestantBE:
```bash
curl http://localhost:5069/api/Auth/login-contestant
```

---

## 💡 Khuyến Nghị

### Nếu bạn là Admin và muốn:

1. **Quản lý challenges, contests, users**
   → Dùng **ManagementPlatform** (port 8000)

2. **Test contestant portal với quyền admin**
   → Dùng **ContestantBE** (port 5069) + React frontend

3. **Tạo challenges, deploy instances**
   → Dùng **ManagementPlatform** (port 8000)

---

## 🚀 Quick Start - Login Admin Ngay

### Cách 1: ManagementPlatform (Dễ nhất)

```bash
# Terminal 1: Khởi động ManagementPlatform
cd FCTF-ManagementPlatform
python serve.py

# Browser: Mở
http://localhost:8000/login

# Login với username: admin, password: your_password
```

### Cách 2: ContestantBE + React Frontend

```bash
# Terminal 1: Khởi động ContestantBE
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run --launch-profile http

# Terminal 2: Khởi động React Frontend
cd ContestantPortal
npm run dev

# Browser: Mở
http://localhost:5173/login

# Login với username: admin, password: Admin@123
```

---

## ⚠️ Lưu Ý

1. **ManagementPlatform** và **ContestantBE** là 2 hệ thống độc lập
2. Cả 2 đều dùng chung database
3. Admin nên dùng **ManagementPlatform** để quản lý
4. Contestants dùng **ContestantBE** + React frontend
5. Password phải đúng format hash trong database

---

## 🔐 Thông Tin Login

Từ database của bạn:
- **Username:** `admin`
- **Email:** `admin@test.com`
- **Type:** `admin`
- **Password:** Cần set lại với hash đúng

Để set password mới:
```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run Admin@123
```

Sau đó update vào database:
```sql
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$...'
WHERE Username = 'admin';
```

---

**Bạn muốn login vào hệ thống nào?** 🎉
