# Tóm tắt sửa lỗi Password Hash Format

## Vấn đề

Backend báo lỗi:
```
Not a passlib bcrypt_sha256 hash
```

**Nguyên nhân**: 
- Database có password hash format **SHA256 thuần** (64 ký tự hex)
- Backend mong đợi format **passlib bcrypt_sha256** (`$bcrypt-sha256$v=2,...`)

## Giải pháp đã áp dụng

### 1. Cập nhật tool GeneratePasswordHash
- Sửa `Program.cs` để dùng `SHA256Helper.HashPasswordPythonStyle()` từ ResourceShared
- Thêm project reference đến ResourceShared
- Tool bây giờ tạo password hash đúng format passlib

### 2. Generate password mới cho test123
```powershell
cd ControlCenterAndChallengeHostingServer\GeneratePasswordHash
dotnet run test123
```

**Kết quả**:
```
Password: test123
Hash: $bcrypt-sha256$v=2,t=2a,r=10$DWzS7GwBZGAJ9RWlr/BZ7.$JJEl0CXNf0y2rrOQi5CMKA.ySUaNfVBG
```

### 3. Cập nhật password trong database
```sql
UPDATE users 
SET password='$bcrypt-sha256$v=2,t=2a,r=10$DWzS7GwBZGAJ9RWlr/BZ7.$JJEl0CXNf0y2rrOQi5CMKA.ySUaNfVBG' 
WHERE name='student1';
```

## Bây giờ thử đăng nhập

1. Mở browser: `http://localhost:5173`
2. Username: `student1`
3. Password: `test123`

✅ **Kết quả mong đợi**: Đăng nhập thành công!

## Tạo password mới cho user khác

Sử dụng tool đã cập nhật:

```powershell
cd ControlCenterAndChallengeHostingServer\GeneratePasswordHash

# Generate hash cho password bất kỳ
dotnet run your_password_here

# Hoặc chạy interactive mode
dotnet run
```

Copy hash và cập nhật vào database:

```sql
UPDATE users SET password='<hash_từ_tool>' WHERE name='username';
```

## Lưu ý quan trọng

1. **Tất cả user mới** phải dùng tool này để tạo password hash
2. **Không dùng SHA256 thuần** nữa - backend không hỗ trợ
3. **Format passlib** an toàn hơn SHA256 thuần (có salt, bcrypt)

## Giải thích kỹ thuật

### Passlib bcrypt_sha256 format:

```
$bcrypt-sha256$v=2,t=2a,r=10$<salt22>$<digest31>
```

- `v=2`: Version 2 (HMAC-SHA256 prehash)
- `t=2a`: BCrypt type
- `r=10`: Rounds (cost factor = 10)
- `<salt22>`: 22-character salt
- `<digest31>`: 31-character digest

### Quy trình hash:

1. Prehash password với HMAC-SHA256 (key = salt)
2. Hash kết quả với BCrypt
3. Format thành passlib string

Điều này an toàn hơn SHA256 thuần vì:
- Có salt (mỗi password có salt khác nhau)
- BCrypt chậm (chống brute force)
- Prehash với SHA256 (hỗ trợ password dài)

## Files đã thay đổi

1. `ControlCenterAndChallengeHostingServer/GeneratePasswordHash/Program.cs`
   - Dùng `SHA256Helper.HashPasswordPythonStyle()`
   - Bỏ hàm `HashPasswordPythonStyle()` cũ

2. `ControlCenterAndChallengeHostingServer/GeneratePasswordHash/GeneratePasswordHash.csproj`
   - Thêm reference đến ResourceShared

3. Database: Cập nhật password của user `student1`
