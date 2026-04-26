-- ============================================
-- KIỂM TRA USERS TRONG DATABASE
-- ============================================

-- 1. Xem TẤT CẢ users
SELECT Id, Username, Email, Type, CreatedAt 
FROM Users
ORDER BY CreatedAt DESC;

-- 2. Xem CHỈ admin users
SELECT Id, Username, Email, Type, CreatedAt 
FROM Users
WHERE Type = 'admin';

-- 3. Xem user cụ thể (thay 'admin' bằng username bạn muốn tìm)
SELECT Id, Username, Email, Type, CreatedAt 
FROM Users
WHERE Username = 'admin';

-- 4. Đếm số lượng users theo type
SELECT Type, COUNT(*) as Count
FROM Users
GROUP BY Type;

-- ============================================
-- CẬP NHẬT USER THÀNH ADMIN
-- ============================================

-- Cách 1: Cập nhật theo Username
UPDATE Users 
SET Type = 'admin' 
WHERE Username = 'your_username_here';

-- Cách 2: Cập nhật theo Email
UPDATE Users 
SET Type = 'admin' 
WHERE Email = 'your_email@example.com';

-- Cách 3: Cập nhật theo Id
UPDATE Users 
SET Type = 'admin' 
WHERE Id = 1;

-- ============================================
-- KIỂM TRA SAU KHI CẬP NHẬT
-- ============================================

-- Xem lại user vừa cập nhật
SELECT Id, Username, Email, Type 
FROM Users
WHERE Type = 'admin';

-- ============================================
-- HƯỚNG DẪN SỬ DỤNG
-- ============================================
-- 
-- BƯỚC 1: Chạy query #1 để xem tất cả users
-- BƯỚC 2: Tìm username bạn muốn làm admin
-- BƯỚC 3: Chạy query UPDATE phù hợp (thay username/email/id)
-- BƯỚC 4: Chạy query kiểm tra để xác nhận
-- BƯỚC 5: Thử login với username và password của user đó
--
-- ============================================
