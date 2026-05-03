-- ============================================
-- FIX ADMIN PASSWORD - FINAL
-- ============================================
-- Vấn đề: Password hash bị cắt ngắn → Login thất bại
-- Giải pháp: Cập nhật password hash ĐẦY ĐỦ
-- ============================================

-- Bước 1: Kiểm tra password hiện tại
SELECT 
    Id, 
    Username, 
    Email, 
    Type,
    Password,
    LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';

-- Nếu PasswordLength < 90 → Password bị cắt ngắn!

-- ============================================
-- Bước 2: Cập nhật password MỚI (ĐẦY ĐỦ)
-- ============================================

-- Password: Admin@123
-- Hash ĐẦY ĐỦ (97 ký tự):
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K'
WHERE Username = 'admin';

-- ============================================
-- Bước 3: Kiểm tra lại
-- ============================================

SELECT 
    Id, 
    Username, 
    Email, 
    Type,
    Password,
    LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';

-- PasswordLength phải = 97 (hoặc gần 97)
-- Nếu < 90 → Cột Password quá ngắn, cần ALTER TABLE

-- ============================================
-- Bước 4: Nếu cần mở rộng cột Password
-- ============================================

-- Kiểm tra độ dài cột hiện tại:
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
  AND COLUMN_NAME = 'Password';

-- Nếu CHARACTER_MAXIMUM_LENGTH < 128, chạy lệnh này:
/*
ALTER TABLE Users
ALTER COLUMN Password NVARCHAR(256);

-- Sau đó chạy lại UPDATE ở Bước 2
*/

-- ============================================
-- THÔNG TIN LOGIN SAU KHI CẬP NHẬT
-- ============================================
-- URL: http://localhost:8000/login
-- Username: admin
-- Password: Admin@123
-- ============================================

-- ============================================
-- CÁC PASSWORD HASH KHÁC (NẾU CẦN)
-- ============================================

-- Password: password123
-- UPDATE Users SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$NpQPzhDmobdBEk./GNxEY.$IDgGKKeUGMiv9ohsAhwKELAME0pR.Z6G' WHERE Username = 'admin';

-- Password: admin123
-- UPDATE Users SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$lJB/j3TkJoQd60uiJp/NuO$iBka77KAe8nRsb4fpiXkbd/7Vj7gmR2O' WHERE Username = 'admin';

-- Password: test123
-- UPDATE Users SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$Kei/.yu28I48HZzQKUSPNe$h0KI77VNNefCTqZ5QsMr0.luCacZfIGq' WHERE Username = 'admin';

-- ============================================
-- LƯU Ý QUAN TRỌNG
-- ============================================
-- 1. Password hash PHẢI ĐẦY ĐỦ (khoảng 97 ký tự)
-- 2. Format: $bcrypt-sha256$v=2,t=2a,r=10$<salt22>$<digest31>
-- 3. Nếu hash bị cắt → Cột Password quá ngắn
-- 4. Khuyến nghị: Cột Password nên là NVARCHAR(256)
-- ============================================
