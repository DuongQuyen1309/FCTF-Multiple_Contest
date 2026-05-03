# 🔐 Danh Sách Password Hash

## ✅ Password Hash Đã Tạo

### 1. **Admin@123**
```
Password: Admin@123
Hash: $bcrypt-sha256$v=2,t=2a,r=10$YdJMyFq/u92ccdvJht2rpfO$MyCQepJSAQQMODf8SlkZAJNq9CATet.
```

### 2. **password123**
```
Password: password123
Hash: $bcrypt-sha256$v=2,t=2a,r=10$Si9gLKw7tTwM4tmNsTEr0e$Hbhe57e.sJ49hunDBSLDzrooTqAfkj1K
```

### 3. **admin123**
```
Password: admin123
Hash: $bcrypt-sha256$v=2,t=2a,r=10$OuYPaycuYox3hwV2.VkAP.$tM4mzLVVFmzRE/iHxEJ6Tc003IoKHx6i
```

### 4. **test123**
```
Password: test123
Hash: $bcrypt-sha256$v=2,t=2a,r=10$xUq.f7hcWQyFiZWyJRFG8e$vHaYgRJPnhqrlY5mYA6rM/TTqi9Xpzfi
```

---

## 📋 SQL Để Cập Nhật

### Cập nhật với Admin@123 (Khuyến nghị)
```sql
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$YdJMyFq/u92ccdvJht2rpfO$MyCQepJSAQQMODf8SlkZAJNq9CATet.'
WHERE Username = 'admin';
```

### Cập nhật với admin123
```sql
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$OuYPaycuYox3hwV2.VkAP.$tM4mzLVVFmzRE/iHxEJ6Tc003IoKHx6i'
WHERE Username = 'admin';
```

### Cập nhật với test123
```sql
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$xUq.f7hcWQyFiZWyJRFG8e$vHaYgRJPnhqrlY5mYA6rM/TTqi9Xpzfi'
WHERE Username = 'admin';
```

---

## ⚠️ VẤN ĐỀ QUAN TRỌNG

### Vấn đề hiện tại:
Password hash trong database của bạn bị **CẮT NGẮN**:
```
Trong DB: $bcrypt-sha256$v=2,t=2a,r=10$ThT1wMMFTDO3h/Lq9Ai6q.$lANTGUUYOs8N/oAtH.oJDRtCb0fyPHi
Độ dài:   ~85 ký tự (BỊ CẮT!)

Đúng:     $bcrypt-sha256$v=2,t=2a,r=10$YdJMyFq/u92ccdvJht2rpfO$MyCQepJSAQQMODf8SlkZAJNq9CATet.
Độ dài:   ~97 ký tự (ĐẦY ĐỦ)
```

### Nguyên nhân:
Cột `Password` trong bảng `Users` có độ dài quá ngắn (có thể là VARCHAR(80) hoặc VARCHAR(100))

### Giải pháp:

#### Bước 1: Kiểm tra độ dài cột
```sql
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
  AND COLUMN_NAME = 'Password';
```

#### Bước 2: Nếu CHARACTER_MAXIMUM_LENGTH < 128, mở rộng cột
```sql
ALTER TABLE Users
ALTER COLUMN Password NVARCHAR(256);
```

#### Bước 3: Cập nhật password hash đầy đủ
```sql
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$YdJMyFq/u92ccdvJht2rpfO$MyCQepJSAQQMODf8SlkZAJNq9CATet.'
WHERE Username = 'admin';
```

#### Bước 4: Kiểm tra lại
```sql
SELECT Id, Username, Email, Type, 
       Password,
       LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';
```

**PasswordLength phải >= 90 ký tự**

---

## 🚀 Sau Khi Cập Nhật

### Thông tin đăng nhập:
- **Username:** `admin`
- **Password:** `Admin@123`

### Test login:
1. Mở `test-admin-login.html`
2. Nhập username: `admin`
3. Nhập password: `Admin@123`
4. Click "Login as Admin"

---

## 💡 Tạo Password Hash Mới

Nếu bạn muốn tạo password hash cho password khác:

```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run your_password_here
```

Ví dụ:
```bash
dotnet run MySecurePassword123!
```
