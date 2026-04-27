-- Script tạo tài khoản Admin trong SQL Server
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

-- 1. Kiểm tra các user hiện có
SELECT Id, Username, Email, Type, CreatedAt 
FROM Users
ORDER BY CreatedAt DESC;

-- 2. Kiểm tra xem đã có admin chưa
SELECT Id, Username, Email, Type 
FROM Users 
WHERE Type = 'admin';

-- 3. Cập nhật user hiện có thành admin (Cách 1 - Khuyến nghị)
-- Thay 'your_username' bằng username thực tế
UPDATE Users 
SET Type = 'admin' 
WHERE Username = 'your_username';

-- Kiểm tra lại
SELECT Id, Username, Email, Type 
FROM Users 
WHERE Username = 'your_username';

-- 4. Hoặc tạo user admin mới (Cách 2)
-- Lưu ý: Password cần được hash bằng BCrypt
-- Ví dụ password hash cho "Admin@123": $2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

-- Nếu bạn muốn tạo user mới, cần hash password trước
-- Có thể dùng online tool: https://bcrypt-generator.com/
-- Hoặc dùng code C# để hash

-- Ví dụ tạo admin mới (cần thay password hash thực tế):
/*
INSERT INTO Users (Username, Email, PasswordHash, Type, CreatedAt)
VALUES (
    'admin',
    'admin@test.com',
    '$2a$11$YourHashedPasswordHere',  -- Thay bằng password hash thực tế
    'admin',
    GETDATE()
);
*/

-- 5. Kiểm tra lại tất cả admin users
SELECT Id, Username, Email, Type, CreatedAt 
FROM Users 
WHERE Type = 'admin';

-- 6. Nếu cần xóa type admin (rollback)
/*
UPDATE Users 
SET Type = 'user' 
WHERE Username = 'admin';
*/

-- ============================================
-- HƯỚNG DẪN SỬ DỤNG:
-- ============================================
-- 
-- CÁCH 1 (Khuyến nghị): Cập nhật user hiện có
-- 1. Chạy query #1 để xem danh sách users
-- 2. Chọn một username muốn làm admin
-- 3. Chạy query #3, thay 'your_username' bằng username thực tế
-- 4. Chạy query #5 để kiểm tra
--
-- CÁCH 2: Tạo user admin mới
-- 1. Hash password bằng BCrypt (dùng online tool hoặc code C#)
-- 2. Uncomment và chỉnh sửa query #4
-- 3. Chạy query #4
-- 4. Chạy query #5 để kiểm tra
--
-- ============================================

-- Ví dụ hash password trong C#:
/*
using BCrypt.Net;

string password = "Admin@123";
string hashedPassword = BCrypt.HashPassword(password);
Console.WriteLine(hashedPassword);
*/
