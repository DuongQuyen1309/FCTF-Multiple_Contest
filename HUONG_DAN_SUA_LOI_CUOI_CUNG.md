# 🔧 Hướng Dẫn Sửa Lỗi "malformed bcrypt_sha256 hash" - CUỐI CÙNG

## ❌ Lỗi Hiện Tại:
```
ValueError: malformed bcrypt_sha256 hash
POST http://localhost:8000/login 500 (INTERNAL SERVER ERROR)
```

## 🎯 Nguyên Nhân:
Password hash trong database bị **CẮT NGẮN** hoặc **SAI FORMAT**

---

## ✅ GIẢI PHÁP - LÀM THEO TỪNG BƯỚC:

### **Bước 1: Mở DBeaver**

1. Mở DBeaver
2. Kết nối đến database của bạn
3. Mở SQL Editor (Ctrl+Enter hoặc SQL Editor button)

---

### **Bước 2: Kiểm Tra Password Hiện Tại**

Copy và chạy SQL này:

```sql
SELECT 
    Id, 
    Username, 
    Email, 
    Type,
    Password,
    LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';
```

**Xem kết quả:**
- Nếu `PasswordLength` < 90 → Password bị cắt ngắn ❌
- Nếu `PasswordLength` >= 90 → Password OK ✅

**Ví dụ kết quả SAI:**
```
PasswordLength = 85  ← BỊ CẮT NGẮN!
Password = $bcrypt-sha256$v=2,t=2a,r=10$ThT1wMMFTDO3h/Lq9Ai6q.$lANTGUUYOs8N/oAtH.oJDRtCb0fyPHi
```

---

### **Bước 3: Kiểm Tra Độ Dài Cột Password**

Copy và chạy SQL này:

```sql
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
  AND COLUMN_NAME = 'Password';
```

**Xem kết quả:**
- Nếu `CHARACTER_MAXIMUM_LENGTH` < 128 → Cột quá nhỏ ❌
- Nếu `CHARACTER_MAXIMUM_LENGTH` >= 256 → Cột OK ✅

**Ví dụ kết quả SAI:**
```
CHARACTER_MAXIMUM_LENGTH = 128  ← QUÁ NHỎ!
```

---

### **Bước 4: Mở Rộng Cột Password (Nếu Cần)**

**Nếu `CHARACTER_MAXIMUM_LENGTH` < 256, chạy SQL này:**

```sql
ALTER TABLE Users
ALTER COLUMN Password NVARCHAR(256);
```

**Sau đó kiểm tra lại:**

```sql
SELECT 
    COLUMN_NAME,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
  AND COLUMN_NAME = 'Password';
```

**Kết quả phải là:**
```
CHARACTER_MAXIMUM_LENGTH = 256  ← OK!
```

---

### **Bước 5: Cập Nhật Password Hash ĐẦY ĐỦ**

**Copy và chạy SQL này:**

```sql
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K'
WHERE Username = 'admin';
```

**Lưu ý:** Password hash này có **97 ký tự** - PHẢI ĐẦY ĐỦ!

---

### **Bước 6: Kiểm Tra Lại**

```sql
SELECT 
    Id, 
    Username, 
    Email, 
    Type,
    Password,
    LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';
```

**Kết quả ĐÚNG phải là:**
```
PasswordLength = 97  ← OK!
Password = $bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K
```

---

### **Bước 7: Test Login**

1. **Mở browser:** `http://localhost:8000/login`
2. **Nhập:**
   - Username: `admin`
   - Password: `Admin@123`
3. **Click Login**
4. **Kết quả:** ✅ Login thành công!

---

## 🔍 TROUBLESHOOTING

### Vấn đề 1: Password vẫn bị cắt sau khi UPDATE

**Nguyên nhân:** Cột Password vẫn quá nhỏ

**Giải pháp:**
1. Chạy lại Bước 4 (ALTER TABLE)
2. Chạy lại Bước 5 (UPDATE)
3. Kiểm tra lại Bước 6

---

### Vấn đề 2: Lỗi "Cannot convert value"

**Nguyên nhân:** SQL syntax sai hoặc database không hỗ trợ

**Giải pháp:**
```sql
-- Thử cách này (dùng VARCHAR thay vì NVARCHAR):
ALTER TABLE Users
ALTER COLUMN Password VARCHAR(256);
```

---

### Vấn đề 3: Vẫn lỗi "malformed hash" sau khi update

**Nguyên nhân:** Password hash không được lưu đúng

**Giải pháp:**
1. Kiểm tra lại password trong database (Bước 6)
2. Copy password hash từ database ra notepad
3. So sánh với password hash gốc:
   ```
   $bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K
   ```
4. Nếu khác → Chạy lại UPDATE

---

## 📋 CHECKLIST

- [ ] Đã mở DBeaver
- [ ] Đã chạy Bước 2 - Kiểm tra password hiện tại
- [ ] Đã chạy Bước 3 - Kiểm tra độ dài cột
- [ ] Đã chạy Bước 4 - Mở rộng cột (nếu cần)
- [ ] Đã chạy Bước 5 - Cập nhật password hash
- [ ] Đã chạy Bước 6 - Kiểm tra lại
- [ ] PasswordLength = 97 ✅
- [ ] Đã test login thành công ✅

---

## 🎓 GIẢI THÍCH

### Tại sao password bị cắt ngắn?

Cột `Password` trong database có độ dài giới hạn (ví dụ: 128 ký tự).
Khi bạn INSERT hoặc UPDATE password dài hơn, SQL Server tự động cắt bớt.

### Password hash đúng phải như thế nào?

Format: `$bcrypt-sha256$v=2,t=2a,r=10$<salt22>$<digest31>`

- `$bcrypt-sha256$` - Algorithm identifier (16 ký tự)
- `v=2,t=2a,r=10$` - Parameters (15 ký tự)
- `<salt22>` - Salt (22 ký tự)
- `<digest31>` - Digest (31 ký tự)

**Tổng:** ~97 ký tự

Nếu thiếu phần `<digest31>` → Hash không hợp lệ → Lỗi!

---

## 💡 TIP

Sau khi sửa xong, bạn có thể tạo thêm password hash khác:

```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run YourPassword123
```

Sau đó update vào database.

---

**Hãy làm theo từng bước và cho tôi biết kết quả nhé!** 🚀
