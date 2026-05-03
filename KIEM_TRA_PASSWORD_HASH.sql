-- ============================================
-- KIỂM TRA PASSWORD HASH TRONG DATABASE
-- ============================================

-- Kiểm tra password hiện tại của admin
SELECT 
    Id, 
    Username, 
    Email, 
    Type,
    Password,
    LEN(Password) as PasswordLength,
    CASE 
        WHEN LEN(Password) >= 90 THEN 'OK - Đủ dài'
        WHEN LEN(Password) < 90 AND LEN(Password) > 0 THEN 'LỖI - Bị cắt ngắn!'
        ELSE 'LỖI - Không có password!'
    END as Status
FROM Users
WHERE Username = 'admin';

-- ============================================
-- PHÂN TÍCH KẾT QUẢ:
-- ============================================
-- 
-- PasswordLength = 97-100 → OK, password hash đầy đủ
-- PasswordLength < 90 → LỖI, password bị cắt ngắn
-- PasswordLength = 0 hoặc NULL → LỖI, không có password
--
-- ============================================

-- Kiểm tra độ dài cột Password
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    CASE 
        WHEN CHARACTER_MAXIMUM_LENGTH >= 256 THEN 'OK - Đủ lớn'
        WHEN CHARACTER_MAXIMUM_LENGTH >= 128 THEN 'Tạm OK - Nên mở rộng'
        ELSE 'LỖI - Quá nhỏ, cần ALTER TABLE!'
    END as Status
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
  AND COLUMN_NAME = 'Password';

-- ============================================
-- NẾU CỘT PASSWORD QUÁ NHỎ:
-- ============================================

-- Mở rộng cột Password lên 256 ký tự:
/*
ALTER TABLE Users
ALTER COLUMN Password NVARCHAR(256);
*/

-- ============================================
-- SAU KHI MỞ RỘNG, CẬP NHẬT PASSWORD:
-- ============================================

-- Password: Admin@123
/*
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K'
WHERE Username = 'admin';
*/

-- Kiểm tra lại sau khi update:
/*
SELECT 
    Id, Username, Email, Type,
    Password,
    LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';
*/
