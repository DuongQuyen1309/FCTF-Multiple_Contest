-- ============================================
-- CẬP NHẬT PASSWORD CHO ADMIN
-- ============================================

-- Password: Admin@123
-- Hash đầy đủ (QUAN TRỌNG: Phải copy TOÀN BỘ hash)

UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$YdJMyFq/u92ccdvJht2rpfO$MyCQepJSAQQMODf8SlkZAJNq9CATet.'
WHERE Username = 'admin';

-- Kiểm tra lại
SELECT Id, Username, Email, Type, 
       LEFT(Password, 50) as PasswordPreview,
       LEN(Password) as PasswordLength
FROM Users
WHERE Username = 'admin';

-- ============================================
-- LƯU Ý QUAN TRỌNG:
-- ============================================
-- 
-- 1. Password hash PHẢI ĐẦY ĐỦ (khoảng 97-100 ký tự)
-- 2. Nếu PasswordLength < 90 thì hash bị cắt ngắn
-- 3. Cần kiểm tra độ dài cột Password trong database
-- 4. Nếu cột Password quá ngắn, cần ALTER TABLE
--
-- ============================================

-- Kiểm tra độ dài cột Password
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
*/

-- ============================================
-- SAU KHI CẬP NHẬT, THÔNG TIN ĐĂNG NHẬP:
-- ============================================
-- Username: admin
-- Password: Admin@123
-- ============================================
