-- ============================================
-- DEBUG PASSWORD HASH CHI TIẾT
-- ============================================

-- 1. Xem password CHÍNH XÁC trong database
SELECT 
    Id,
    Username,
    Email,
    Type,
    Password,
    LEN(Password) as PasswordLength,
    -- Xem từng phần của hash
    SUBSTRING(Password, 1, 16) as Part1_Algorithm,
    SUBSTRING(Password, 17, 15) as Part2_Params,
    SUBSTRING(Password, 32, 22) as Part3_Salt,
    SUBSTRING(Password, 55, 50) as Part4_Digest,
    -- Kiểm tra ký tự đặc biệt
    CHARINDEX('$', Password) as FirstDollar,
    CHARINDEX('$', Password, 2) as SecondDollar,
    CHARINDEX('$', Password, 18) as ThirdDollar,
    CHARINDEX('$', Password, 33) as FourthDollar
FROM Users
WHERE Username = 'admin';

-- ============================================
-- 2. So sánh với hash ĐÚNG
-- ============================================

-- Hash ĐÚNG phải có format:
-- $bcrypt-sha256$v=2,t=2a,r=10$<22chars>$<31chars>
-- 
-- Part1: $bcrypt-sha256$  (16 ký tự)
-- Part2: v=2,t=2a,r=10$   (15 ký tự)
-- Part3: <salt>           (22 ký tự)
-- Part4: $<digest>        (32 ký tự: $ + 31 chars)
--
-- Tổng: ~97 ký tự

-- ============================================
-- 3. Kiểm tra encoding
-- ============================================

SELECT 
    Username,
    Password,
    -- Kiểm tra có ký tự lạ không
    CAST(Password AS VARBINARY(256)) as PasswordBinary,
    -- Đếm số dấu $
    LEN(Password) - LEN(REPLACE(Password, '$', '')) as DollarCount
FROM Users
WHERE Username = 'admin';

-- DollarCount phải = 4 (4 dấu $)

-- ============================================
-- 4. Test với password hash MỚI
-- ============================================

-- Thử update với password hash MỚI (chắc chắn đúng):
/*
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K'
WHERE Username = 'admin';
*/

-- Sau đó kiểm tra lại:
/*
SELECT 
    Username,
    Password,
    LEN(Password) as PasswordLength,
    LEN(Password) - LEN(REPLACE(Password, '$', '')) as DollarCount
FROM Users
WHERE Username = 'admin';
*/

-- ============================================
-- 5. Nếu vẫn lỗi, thử password hash KHÁC
-- ============================================

-- Password: test123 (đơn giản hơn)
/*
UPDATE Users 
SET Password = '$bcrypt-sha256$v=2,t=2a,r=10$Kei/.yu28I48HZzQKUSPNe$h0KI77VNNefCTqZ5QsMr0.luCacZfIGq'
WHERE Username = 'admin';
*/

-- Login với: test123

-- ============================================
-- PHÂN TÍCH KẾT QUẢ
-- ============================================
-- 
-- Nếu PasswordLength = 97 VÀ DollarCount = 4 → Hash đúng format
-- Nếu PasswordLength < 97 → Hash bị cắt
-- Nếu DollarCount != 4 → Hash sai format
-- Nếu có ký tự lạ trong PasswordBinary → Encoding sai
--
-- ============================================
