# 🧪 Local Testing Guide - Multiple Contest System

## 📋 Mục lục
1. [Chuẩn bị môi trường](#1-chuẩn-bị-môi-trường)
2. [Setup Database](#2-setup-database)
3. [Setup Redis](#3-setup-redis)
4. [Chạy Backend](#4-chạy-backend)
5. [Chạy Frontend](#5-chạy-frontend)
6. [Test Scenarios](#6-test-scenarios)
7. [Troubleshooting](#7-troubleshooting)

---

## 1. Chuẩn bị môi trường

### **Yêu cầu:**
- ✅ .NET 6.0 SDK trở lên
- ✅ Node.js 18+ và npm
- ✅ MySQL/MariaDB 10.11+
- ✅ Redis 6.0+
- ✅ Git Bash hoặc PowerShell

### **Kiểm tra version:**
```bash
# Check .NET
dotnet --version

# Check Node.js
node --version
npm --version

# Check MySQL
mysql --version

# Check Redis
redis-cli --version
```

---

## 2. Setup Database

### **Bước 1: Tạo Database**
```sql
-- Kết nối MySQL
mysql -u root -p

-- Tạo database
CREATE DATABASE fctf_multiple_contest CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Tạo user (optional)
CREATE USER 'fctf_user'@'localhost' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON fctf_multiple_contest.* TO 'fctf_user'@'localhost';
FLUSH PRIVILEGES;

-- Kiểm tra
USE fctf_multiple_contest;
SHOW TABLES;
```

### **Bước 2: Import Schema**

Nếu bạn có file SQL schema:
```bash
mysql -u root -p fctf_multiple_contest < path/to/schema.sql
```

Hoặc chạy migrations nếu có:
```bash
cd ControlCenterAndChallengeHostingServer
dotnet ef database update
```

### **Bước 3: Seed Test Data**

Tạo file SQL để insert test data:

```sql
-- Insert test users
INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', '$2b$12$KIXxLVQdKwrB.zQx5qZ5qOYvJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5', 'admin', 1, 0, 0, NOW()),
('teacher1', 'teacher1@test.com', '$2b$12$KIXxLVQdKwrB.zQx5qZ5qOYvJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5', 'teacher', 1, 0, 0, NOW()),
('student1', 'student1@test.com', '$2b$12$KIXxLVQdKwrB.zQx5qZ5qOYvJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5', 'user', 1, 0, 0, NOW()),
('student2', 'student2@test.com', '$2b$12$KIXxLVQdKwrB.zQx5qZ5qOYvJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5', 'user', 1, 0, 0, NOW());

-- Insert test semester
INSERT INTO semesters (semester_name, start_date, end_date) VALUES
('Spring 2024', '2024-01-01', '2024-05-31');

-- Insert test contests
INSERT INTO contests (name, description, slug, owner_id, semester_name, state, user_mode, start_time, end_time, created_at) VALUES
('Test Contest 1', 'First test contest', 'test-contest-1', 1, 'Spring 2024', 'visible', 'users', '2024-01-01 00:00:00', '2024-12-31 23:59:59', NOW()),
('Test Contest 2', 'Second test contest', 'test-contest-2', 2, 'Spring 2024', 'visible', 'users', '2024-01-01 00:00:00', '2024-12-31 23:59:59', NOW());

-- Insert test teams
INSERT INTO teams (name, email, password, captain_id, created) VALUES
('Team Alpha', 'alpha@test.com', '$2b$12$KIXxLVQdKwrB.zQx5qZ5qOYvJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5', 3, NOW()),
('Team Beta', 'beta@test.com', '$2b$12$KIXxLVQdKwrB.zQx5qZ5qOYvJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5vJ5', 4, NOW());

-- Insert contest participants
INSERT INTO contest_participants (contest_id, user_id, team_id, role, score, joined_at) VALUES
(1, 3, 1, 'contestant', 0, NOW()),
(1, 4, 2, 'contestant', 0, NOW()),
(2, 3, 1, 'contestant', 0, NOW());

-- Insert test challenges (bank)
INSERT INTO challenges (name, description, category, value, type, state, max_attempts, require_deploy, connection_protocol) VALUES
('Web Challenge 1', 'Test web challenge', 'Web', 100, 'standard', 'visible', 0, 0, 'http'),
('Crypto Challenge 1', 'Test crypto challenge', 'Crypto', 150, 'standard', 'visible', 3, 0, 'http'),
('Pwn Challenge 1', 'Test pwn challenge', 'Pwn', 200, 'standard', 'visible', 0, 1, 'tcp');

-- Insert flags for challenges
INSERT INTO flags (challenge_id, type, content, data) VALUES
(1, 'static', 'flag{test_web_flag}', 'case_insensitive'),
(2, 'static', 'flag{test_crypto_flag}', 'case_insensitive'),
(3, 'static', 'flag{test_pwn_flag}', 'case_insensitive');

-- Verify data
SELECT * FROM users;
SELECT * FROM contests;
SELECT * FROM contest_participants;
SELECT * FROM challenges;
```

**Lưu ý:** Password hash trên là ví dụ. Bạn cần generate hash thật cho password của mình.

### **Generate Password Hash:**

Tạo file C# script để generate password:
```csharp
// GeneratePassword.cs
using System;
using System.Security.Cryptography;
using System.Text;

public class PasswordHasher
{
    public static void Main(string[] args)
    {
        string password = "password123"; // Change this
        string hash = HashPassword(password);
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
    }

    public static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var saltedPassword = password + "your_salt"; // Use your salt
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(bytes);
        }
    }
}
```

Hoặc sử dụng tool online: https://bcrypt-generator.com/

---

## 3. Setup Redis

### **Bước 1: Cài đặt Redis**

**Windows:**
```bash
# Download Redis for Windows
# https://github.com/microsoftarchive/redis/releases

# Hoặc dùng WSL
wsl --install
wsl
sudo apt update
sudo apt install redis-server
```

**Linux/Mac:**
```bash
# Ubuntu/Debian
sudo apt install redis-server

# Mac
brew install redis
```

### **Bước 2: Start Redis**

```bash
# Windows (Redis for Windows)
redis-server

# Linux/Mac
sudo service redis-server start

# Hoặc
redis-server --port 6379
```

### **Bước 3: Test Redis**

```bash
# Connect to Redis
redis-cli

# Test commands
127.0.0.1:6379> PING
PONG

127.0.0.1:6379> SET test "hello"
OK

127.0.0.1:6379> GET test
"hello"

127.0.0.1:6379> DEL test
(integer) 1

127.0.0.1:6379> exit
```

---

## 4. Chạy Backend

### **Bước 1: Cấu hình Environment Variables**

Tạo file `.env` trong `ControlCenterAndChallengeHostingServer/ContestantBE/`:

```env
# Database
DB_CONNECTION=Server=localhost;Port=3306;Database=fctf_multiple_contest;User=root;Password=your_password;

# Redis
REDIS_CONNECTION_STRING=localhost:6379
REDIS_TLS_INSECURE_SKIP_VERIFY=true

# JWT Secret
PRIVATE_KEY=your-super-secret-key-change-this-in-production

# Cloudflare Turnstile (Optional for testing)
CLOUDFLARE_TURNSTILE_SECRET_KEY=
CLOUDFLARE_TURNSTILE_ENABLED=false

# CTF Config
CTF_NAME=FCTF Test
CTF_DESCRIPTION=Test Environment
```

### **Bước 2: Restore Dependencies**

```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet restore
```

### **Bước 3: Build Project**

```bash
dotnet build
```

### **Bước 4: Run Backend**

```bash
dotnet run

# Hoặc với watch mode (auto-reload)
dotnet watch run
```

Backend sẽ chạy tại: `http://localhost:5000` hoặc `https://localhost:5001`

### **Bước 5: Verify Backend**

Mở browser hoặc Postman:
```
GET http://localhost:5000/healthcheck
Response: Healthy

GET http://localhost:5000/swagger
Response: Swagger UI
```

---

## 5. Chạy Frontend

### **Bước 1: Install Dependencies**

```bash
cd ContestantPortal
npm install
```

### **Bước 2: Cấu hình Environment**

Tạo file `.env.local` trong `ContestantPortal/`:

```env
VITE_API_BASE_URL=http://localhost:5000
VITE_CLOUDFLARE_TURNSTILE_SITE_KEY=
```

Hoặc cập nhật `public/env.template.js`:

```javascript
window.ENV = {
  API_BASE_URL: 'http://localhost:5000',
  CLOUDFLARE_TURNSTILE_SITE_KEY: '',
  BASE_GATEWAY: '',
  HTTP_PORT: '',
  TCP_PORT: ''
};
```

### **Bước 3: Run Frontend**

```bash
npm run dev
```

Frontend sẽ chạy tại: `http://localhost:5173`

### **Bước 4: Verify Frontend**

Mở browser: `http://localhost:5173`

Bạn sẽ thấy trang login.

---

## 6. Test Scenarios

### **Test 1: Login Flow (Không cần contestId)**

#### **Bước 1: Login**
1. Mở `http://localhost:5173/login`
2. Nhập credentials:
   - Username: `student1`
   - Password: `password123`
3. Click "Login"

#### **Expected Result:**
- ✅ Login thành công
- ✅ Redirect tới `/contests`
- ✅ Thấy danh sách contests mà student1 tham gia
- ✅ Token trong localStorage có `contestId = 0`

#### **Verify Token:**
```javascript
// Mở DevTools Console
const token = localStorage.getItem('auth_token');
console.log(token);

// Decode JWT (dùng jwt.io)
// Payload should have: { userId: 3, teamId: 0, contestId: 0 }
```

---

### **Test 2: Contest Selection**

#### **Bước 1: Click vào Contest**
1. Ở trang `/contests`, click vào "Test Contest 1"

#### **Expected Result:**
- ✅ Call API `POST /auth/select-contest` với `{ contestId: 1 }`
- ✅ Nhận token mới với `contestId = 1`
- ✅ Redirect tới `/contest/1/challenges`
- ✅ Token mới có `{ userId: 3, teamId: 1, contestId: 1 }`

#### **Verify:**
```javascript
// Check new token
const token = localStorage.getItem('auth_token');
console.log(token);

// Check user info
const user = JSON.parse(localStorage.getItem('user_info'));
console.log(user);
// Should have team info now
```

---

### **Test 3: Access Contest Resources**

#### **Bước 1: View Challenges**
1. Ở trang `/contest/1/challenges`
2. Xem danh sách challenges

#### **Expected Result:**
- ✅ Thấy challenges của Contest 1
- ✅ API call: `GET /challenge/by-topic` với JWT có `contestId = 1`
- ✅ Backend query `contests_challenges` WHERE `contest_id = 1`

#### **Verify Backend Logs:**
```
[Info] User 3 : Team 1 : Contest 1 - VIEW_All_TOPIC
```

---

### **Test 4: Try Access Without Contest Selection**

#### **Bước 1: Clear Token**
```javascript
// DevTools Console
localStorage.setItem('auth_token', 'old-token-with-contestId-0');
```

#### **Bước 2: Try Access Challenge**
1. Navigate to `/contest/1/challenges`

#### **Expected Result:**
- ✅ API call fails with error
- ✅ Response: `{ error: "No contest selected. Please select a contest first." }`
- ✅ Frontend shows error message

---

### **Test 5: Admin - Create Contest**

#### **Bước 1: Login as Admin**
1. Logout
2. Login với:
   - Username: `admin`
   - Password: `password123`

#### **Bước 2: Create Contest**
1. Click "Create Contest"
2. Fill form:
   - Name: "Test Contest 3"
   - Slug: "test-contest-3"
   - Description: "Third test contest"
   - Semester: "Spring 2024"
   - Start Time: (chọn ngày)
   - End Time: (chọn ngày)
3. Click "Create Contest"

#### **Expected Result:**
- ✅ Contest created successfully
- ✅ Redirect to contest challenges page
- ✅ New contest appears in database

#### **Verify Database:**
```sql
SELECT * FROM contests WHERE slug = 'test-contest-3';
```

---

### **Test 6: Admin - Pull Challenges**

#### **Bước 1: Navigate to Pull Challenges**
1. From contest list, click "Pull Challenges" on a contest

#### **Bước 2: Select Challenges**
1. Check "Web Challenge 1"
2. Check "Crypto Challenge 1"
3. Click "Configure" on "Web Challenge 1"
4. Change:
   - Points: 150 (instead of 100)
   - Max Attempts: 5
   - State: visible
5. Click "Save Configuration"
6. Click "Pull 2 Challenge(s)"

#### **Expected Result:**
- ✅ Challenges pulled successfully
- ✅ `contests_challenges` table has 2 new records
- ✅ Web Challenge 1 has value = 150 (overridden)
- ✅ Crypto Challenge 1 has value = 150 (from bank)

#### **Verify Database:**
```sql
SELECT cc.id, cc.contest_id, cc.bank_id, cc.name, cc.value, c.value as bank_value
FROM contests_challenges cc
JOIN challenges c ON cc.bank_id = c.id
WHERE cc.contest_id = 1;
```

---

### **Test 7: Admin - Import Participants**

#### **Bước 1: Navigate to Import Participants**
1. From contest list, click "Import Users" on a contest

#### **Bước 2: Import Users**
1. Enter emails (one per line):
```
student3@test.com
student4@test.com
newuser@test.com
```
2. Select Role: "Contestant"
3. Click "Import Participants"

#### **Expected Result:**
- ✅ Import completed
- ✅ Summary shows:
  - New Users Created: 1 (newuser@test.com)
  - Existing Users Added: 2 (student3, student4)
- ✅ All 3 users added to `contest_participants`

#### **Verify Database:**
```sql
-- Check new user
SELECT * FROM users WHERE email = 'newuser@test.com';

-- Check participants
SELECT cp.*, u.email 
FROM contest_participants cp
JOIN users u ON cp.user_id = u.id
WHERE cp.contest_id = 1;
```

---

### **Test 8: Contest Switching**

#### **Bước 1: Select Contest 1**
1. Login as student1
2. Click on "Test Contest 1"
3. View challenges

#### **Bước 2: Switch to Contest 2**
1. Click "Back to Contests" (arrow icon in header)
2. Click on "Test Contest 2"

#### **Expected Result:**
- ✅ New token generated with `contestId = 2`
- ✅ Challenges page shows Contest 2's challenges
- ✅ Old token (Contest 1) no longer valid

#### **Verify:**
```javascript
// Check token changed
const token = localStorage.getItem('auth_token');
// Decode and verify contestId = 2
```

---

### **Test 9: Submit Flag (Without Deploy)**

#### **Bước 1: Select Contest with Challenges**
1. Login as student1
2. Select "Test Contest 1"
3. Click on "Web Challenge 1"

#### **Bước 2: Submit Flag**
1. Enter flag: `flag{test_web_flag}`
2. Click "Submit"

#### **Expected Result:**
- ✅ Flag accepted
- ✅ Success message: "Correct!"
- ✅ Points added to team score
- ✅ Submission recorded in database

#### **Verify Database:**
```sql
-- Check submission
SELECT * FROM submissions 
WHERE contest_challenge_id = (
  SELECT id FROM contests_challenges 
  WHERE contest_id = 1 AND bank_id = 1
)
AND team_id = 1;

-- Check solve
SELECT * FROM solves 
WHERE contest_challenge_id = (
  SELECT id FROM contests_challenges 
  WHERE contest_id = 1 AND bank_id = 1
)
AND team_id = 1;
```

---

### **Test 10: Scoreboard**

#### **Bước 1: View Scoreboard**
1. Navigate to `/contest/1/scoreboard`

#### **Expected Result:**
- ✅ Shows teams/users ranked by score
- ✅ Team Alpha has points from solved challenge
- ✅ Scoreboard scoped to Contest 1 only

---

## 7. Troubleshooting

### **Problem 1: Backend không start**

**Error:** `Unable to connect to database`

**Solution:**
```bash
# Check MySQL is running
sudo service mysql status

# Check connection string in .env
DB_CONNECTION=Server=localhost;Port=3306;Database=fctf_multiple_contest;User=root;Password=your_password;

# Test connection
mysql -u root -p -e "USE fctf_multiple_contest; SHOW TABLES;"
```

---

### **Problem 2: Redis connection failed**

**Error:** `Redis connection timeout`

**Solution:**
```bash
# Check Redis is running
redis-cli ping

# If not running, start it
redis-server

# Check connection string
REDIS_CONNECTION_STRING=localhost:6379
```

---

### **Problem 3: Frontend không connect được Backend**

**Error:** `Network Error` hoặc `CORS Error`

**Solution:**
```bash
# Check backend is running
curl http://localhost:5000/healthcheck

# Check CORS settings in Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod()
    );
});

# Check frontend API_BASE_URL
# In public/env.template.js or .env.local
VITE_API_BASE_URL=http://localhost:5000
```

---

### **Problem 4: JWT Token invalid**

**Error:** `Unauthorized` hoặc `Invalid token`

**Solution:**
```bash
# Check PRIVATE_KEY in backend .env
PRIVATE_KEY=your-super-secret-key-change-this-in-production

# Clear localStorage and login again
localStorage.clear();
```

---

### **Problem 5: Contest not showing**

**Error:** Contest list is empty

**Solution:**
```sql
-- Check contests exist
SELECT * FROM contests;

-- Check user is participant
SELECT * FROM contest_participants WHERE user_id = 3;

-- Add user to contest
INSERT INTO contest_participants (contest_id, user_id, role, score, joined_at)
VALUES (1, 3, 'contestant', 0, NOW());
```

---

### **Problem 6: Challenge không hiển thị**

**Error:** No challenges in contest

**Solution:**
```sql
-- Check contests_challenges
SELECT * FROM contests_challenges WHERE contest_id = 1;

-- If empty, pull challenges from bank
-- Use admin account and "Pull Challenges" feature

-- Or manually insert
INSERT INTO contests_challenges (contest_id, bank_id, name, value, state, require_deploy)
SELECT 1, id, name, value, state, require_deploy
FROM challenges
WHERE id IN (1, 2, 3);
```

---

## 8. Testing Checklist

### **Basic Flow:**
- [ ] Login without contestId works
- [ ] Redirect to /contests after login
- [ ] Contest list shows only user's contests
- [ ] Click contest calls select-contest API
- [ ] New JWT token has correct contestId
- [ ] Navigate to contest challenges works

### **Admin Features:**
- [ ] Create contest works
- [ ] Pull challenges works
- [ ] Configure challenge overrides works
- [ ] Import participants works
- [ ] Admin sees all contests

### **Contest Features:**
- [ ] View challenges in contest
- [ ] Submit flag works (non-deploy challenges)
- [ ] Scoreboard shows correct data
- [ ] Action logs recorded
- [ ] Contest switching works

### **Security:**
- [ ] Token without contestId cannot access challenges
- [ ] Token from Contest A cannot access Contest B
- [ ] Redis keys isolated by contest
- [ ] User cannot access contests they don't participate in

---

## 9. Quick Test Script

Tạo file `test-api.sh`:

```bash
#!/bin/bash

API_URL="http://localhost:5000"

echo "=== Testing FCTF Multiple Contest API ==="

# Test 1: Health Check
echo -e "\n1. Health Check"
curl -s "$API_URL/healthcheck"

# Test 2: Login
echo -e "\n\n2. Login"
TOKEN=$(curl -s -X POST "$API_URL/auth/login-contestant" \
  -H "Content-Type: application/json" \
  -d '{"username":"student1","password":"password123"}' \
  | jq -r '.generatedToken')
echo "Token: $TOKEN"

# Test 3: Get Contests
echo -e "\n\n3. Get Contests"
curl -s "$API_URL/api/Contest/list" \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.'

# Test 4: Select Contest
echo -e "\n\n4. Select Contest"
NEW_TOKEN=$(curl -s -X POST "$API_URL/auth/select-contest" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"contestId":1}' \
  | jq -r '.data.token')
echo "New Token: $NEW_TOKEN"

# Test 5: Get Challenges
echo -e "\n\n5. Get Challenges"
curl -s "$API_URL/challenge/by-topic" \
  -H "Authorization: Bearer $NEW_TOKEN" \
  | jq '.'

echo -e "\n\n=== Tests Complete ==="
```

Run:
```bash
chmod +x test-api.sh
./test-api.sh
```

---

## 10. Monitoring & Debugging

### **Backend Logs:**
```bash
# Watch logs in real-time
dotnet run | tee backend.log

# Filter specific logs
dotnet run | grep "SELECT_CONTEST"
```

### **Redis Monitoring:**
```bash
# Monitor Redis commands
redis-cli monitor

# Check keys
redis-cli
127.0.0.1:6379> KEYS contest:*
127.0.0.1:6379> GET contest:1:auth:user:3
```

### **Database Queries:**
```sql
-- Monitor queries (MySQL)
SET GLOBAL general_log = 'ON';
SET GLOBAL log_output = 'TABLE';
SELECT * FROM mysql.general_log ORDER BY event_time DESC LIMIT 20;
```

### **Frontend DevTools:**
```javascript
// Monitor API calls
// Open DevTools > Network tab
// Filter: XHR

// Check localStorage
console.log(localStorage.getItem('auth_token'));
console.log(localStorage.getItem('user_info'));

// Monitor state
// React DevTools > Components > AuthProvider
```

---

## 📝 Summary

Để test local:

1. ✅ Setup MySQL + seed data
2. ✅ Start Redis
3. ✅ Configure .env files
4. ✅ Run backend: `dotnet run`
5. ✅ Run frontend: `npm run dev`
6. ✅ Test login flow (no contestId)
7. ✅ Test contest selection
8. ✅ Test admin features
9. ✅ Test contest switching

**Lưu ý:** Các tính năng liên quan K8s (deploy challenges) sẽ không test được ở local. Chỉ test:
- ✅ Authentication flow
- ✅ Contest management
- ✅ Challenge listing
- ✅ Flag submission (non-deploy)
- ✅ Scoreboard
- ✅ User management

---

**Happy Testing! 🚀**
