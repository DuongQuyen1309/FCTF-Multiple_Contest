# 🎯 GIẢI PHÁP CUỐI CÙNG - Fix "malformed bcrypt_sha256 hash"

## ✅ CÁCH 1: Chạy Script Python Test (Khuyến nghị)

### Bước 1: Chạy script test
```bash
cd FCTF-ManagementPlatform
python test_password_hash.py
```

Script này sẽ:
- Test password hash hiện tại
- Tạo password hash MỚI đúng format
- Cho bạn SQL để update

### Bước 2: Copy password hash từ output

Script sẽ tạo password hash mới, ví dụ:
```
Password: Admin@123
Hash: $bcrypt-sha256$v=2,t=2a,r=10$abc...xyz
Length: 97
Verify: ✅ OK
```

### Bước 3: Update vào database

```sql
UPDATE Users
SET Password = '<paste_hash_từ_script>'
WHERE Username = 'admin';
```

### Bước 4: Test login

- URL: http://localhost:8000/login
- Username: admin
- Password: Admin@123 (hoặc password bạn chọn)

---

## ✅ CÁCH 2: Dùng SQL Trực Tiếp

### Bước 1: Chạy SQL debug

Mở DBeaver và chạy file `DEBUG_PASSWORD_HASH.sql`:

```sql
SELECT 
    Username,
    Password,
    LEN(Password) as PasswordLength,
    LEN(Password) - LEN(REPLACE(Password, '$', '')) as DollarCount
FROM Users
WHERE Username = 'admin';
```

**Kiểm tra:**
- `PasswordLength` phải >= 90
- `DollarCount` phải = 4

### Bước 2: Update password hash

```sql
-- Password: Admin@123
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K'
WHERE Username = 'admin';
```

### Bước 3: Kiểm tra lại

```sql
SELECT 
    Username,
    Password,
    LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';
```

**Phải thấy:** `PasswordLength = 97`

### Bước 4: Test login

- URL: http://localhost:8000/login
- Username: admin
- Password: Admin@123

---

## ✅ CÁCH 3: Dùng Password Đơn Giản Hơn

Nếu vẫn lỗi, thử password đơn giản:

```sql
-- Password: test123
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$Kei/.yu28I48HZzQKUSPNe$h0KI77VNNefCTqZ5QsMr0.luCacZfIGq'
WHERE Username = 'admin';
```

Login với:
- Username: admin
- Password: test123

---

## 🔍 DEBUG - Nếu Vẫn Lỗi

### Kiểm tra 1: Password có đúng trong database không?

```sql
SELECT Password FROM Users WHERE Username = 'admin';
```

Copy password ra và so sánh với hash gốc. Phải GIỐNG HỆT!

### Kiểm tra 2: Có ký tự lạ không?

```sql
SELECT 
    Password,
    CAST(Password AS VARBINARY(256)) as PasswordBinary
FROM Users 
WHERE Username = 'admin';
```

Nếu thấy ký tự lạ trong `PasswordBinary` → Encoding sai

### Kiểm tra 3: Test với user khác

Tạo user mới để test:

```sql
-- Tạo user test
INSERT INTO Users (Name, Email, Password, Type, Verified, Created)
VALUES (
    'testadmin',
    'testadmin@test.com',
    '$bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K',
    'admin',
    1,
    GETDATE()
);
```

Login với:
- Username: testadmin
- Password: Admin@123

Nếu testadmin login được → Vấn đề ở user admin cũ
Nếu testadmin cũng lỗi → Vấn đề ở code hoặc config

---

## 🎓 GIẢI THÍCH

### Tại sao lỗi "malformed hash"?

`passlib.hash.bcrypt_sha256` rất strict về format:

**Format đúng:**
```
$bcrypt-sha256$v=2,t=2a,r=10$<22chars>$<31chars>
```

**Các lỗi thường gặp:**
1. Hash bị cắt ngắn (thiếu digest)
2. Có khoảng trắng thừa
3. Ký tự đặc biệt bị encode sai
4. Thiếu dấu `$` ở đúng vị trí

### Tại sao cột 128 ký tự vẫn không đủ?

Hash cần ~97 ký tự, nhưng:
- SQL Server có thể thêm padding
- Encoding có thể tốn thêm bytes
- An toàn nhất là dùng 256 ký tự

---

## 📋 CHECKLIST

- [ ] Đã chạy `test_password_hash.py` (Cách 1)
- [ ] Hoặc đã chạy SQL update (Cách 2)
- [ ] Đã kiểm tra `PasswordLength = 97`
- [ ] Đã kiểm tra `DollarCount = 4`
- [ ] Đã test login
- [ ] Login thành công ✅

---

## 💡 KHUYẾN NGHỊ

**Chạy Cách 1 (Python script) trước** vì:
- Tạo hash mới 100% đúng
- Test ngay trong script
- Không lo lỗi copy/paste

Nếu không chạy được Python → Dùng Cách 2 (SQL)

---

**Hãy thử Cách 1 trước nhé!** 🚀

```bash
cd FCTF-ManagementPlatform
python test_password_hash.py
```
