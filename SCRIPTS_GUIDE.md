# 📜 Scripts Guide - FCTF Multiple Contest

## 📋 Available Scripts

This guide explains how to use all the scripts provided for testing the FCTF Multiple Contest system.

---

## 🐳 Docker & Database Scripts

### **1. setup-database.sh**

**Purpose:** Setup database in Docker MariaDB

**What it does:**
- Waits for MariaDB to be ready
- Creates `fctf_multiple_contest` database
- Grants privileges to `fctf_user`
- Imports schema if `schema.sql` exists

**Usage:**
```bash
# Make executable (first time only)
chmod +x setup-database.sh

# Run
./setup-database.sh
```

**Prerequisites:**
- Docker running
- MariaDB container running (`docker compose -f docker-compose.dev.yml up -d`)

**Output:**
```
=== FCTF Database Setup ===
Waiting for MariaDB to be ready...
MariaDB is ready!
Creating database...
Database created: fctf_multiple_contest
```

---

### **2. import-test-data.sh**

**Purpose:** Import test data into database

**What it does:**
- Checks if `test-data.sql` exists
- Verifies password hash has been updated
- Imports test data
- Shows verification counts

**Usage:**
```bash
# Make executable (first time only)
chmod +x import-test-data.sh

# Run
./import-test-data.sh
```

**Prerequisites:**
- Database created (`setup-database.sh` completed)
- `test-data.sql` exists
- Password hash updated in `test-data.sql`

**Output:**
```
=== Importing test data ===
Importing test data into database...
Test data imported successfully!

=== Verification ===
Info          Count
Users:        7
Contests:     3
Participants: 5
Challenges:   10
Contest Challenges: 9
```

---

### **3. test-with-docker.sh**

**Purpose:** Complete automated setup

**What it does:**
- Starts Docker services
- Waits for services to be healthy
- Runs `setup-database.sh`
- Runs `import-test-data.sh` (if ready)
- Verifies all services
- Shows next steps

**Usage:**
```bash
# Make executable (first time only)
chmod +x test-with-docker.sh

# Run
./test-with-docker.sh
```

**Prerequisites:**
- Docker installed and running
- `docker-compose.dev.yml` exists

**Output:**
```
=== FCTF Multiple Contest - Docker Test ===
Step 1: Starting infrastructure...
Step 2: Waiting for services to be healthy...
Step 3: Checking service health...
Step 4: Setting up database...
Step 5: Test data import...
Step 6: Verifying setup...
✓ MariaDB OK
✓ Redis OK
✓ RabbitMQ OK

=== Setup Complete! ===
```

---

## 🧪 Testing Scripts

### **4. test-api.sh**

**Purpose:** Automated API testing

**What it does:**
- Tests health check
- Tests login flow
- Tests get contests
- Tests select contest
- Tests get challenges
- Tests access control
- Tests admin features
- Shows test summary

**Usage:**
```bash
# Make executable (first time only)
chmod +x test-api.sh

# Run
./test-api.sh
```

**Prerequisites:**
- Backend running on `http://localhost:5000`
- Test data imported
- Users created with password `password123`

**Output:**
```
=== Test 1: Health Check ===
✓ Health check passed

=== Test 2: Login as Student ===
✓ Login successful
✓ Temporary token has contestId=0

=== Test 3: Get Contests ===
✓ Get contests successful
ℹ Found 2 contests

=== Test 4: Select Contest ===
✓ Select contest successful
✓ New token has contestId=1

=== Test 5: Get Challenges ===
✓ Get challenges successful
ℹ Found 5 challenges

=== Test 6: Access Control ===
✓ Access control working - temporary token blocked

=== Test 7: Admin Login ===
✓ Admin login successful
✓ User type is admin

=== Test 8: Get Challenge Bank ===
✓ Get challenge bank successful
ℹ Found 10 challenges in bank

=== Test Summary ===
Passed: 12
Failed: 0
Total: 12
All tests passed! ✓
```

---

## 📊 Data Scripts

### **5. test-data.sql**

**Purpose:** SQL script with test data

**What it contains:**
- 7 test users (admin, teachers, students)
- 3 test contests
- Contest participants
- 10 test challenges (bank)
- Flags for challenges
- Contest challenges (pulled from bank)
- Verification queries

**Usage:**
```bash
# Method 1: Using import-test-data.sh (recommended)
./import-test-data.sh

# Method 2: Direct import
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest < test-data.sql

# Method 3: Manual import
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest
source /path/to/test-data.sql;
```

**IMPORTANT:** Before importing, you must:
1. Generate password hash
2. Replace `REPLACE_WITH_HASH` in the file

---

## 🔐 Password Hash Generation

### **Generate Password Hash:**

```bash
# Navigate to tool directory
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash

# Generate hash for "password123"
dotnet run password123

# Output example:
# Password: password123
# Hash: ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f
```

### **Update test-data.sql:**

1. Open `test-data.sql`
2. Find all occurrences of `REPLACE_WITH_HASH`
3. Replace with the generated hash
4. Save the file

**Example:**
```sql
-- Before
INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', 'REPLACE_WITH_HASH', 'admin', 1, 0, 0, NOW()),

-- After
INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'admin', 1, 0, 0, NOW()),
```

---

## 🚀 Complete Workflow

### **First Time Setup:**

```bash
# 1. Start Docker services
docker compose -f docker-compose.dev.yml up -d

# 2. Generate password hash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy the hash

# 3. Update test-data.sql
# Replace REPLACE_WITH_HASH with the hash

# 4. Run automated setup
cd ../..  # Back to root
./test-with-docker.sh

# 5. Start backend (new terminal)
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run

# 6. Start frontend (new terminal)
cd ContestantPortal
npm run dev

# 7. Test APIs
./test-api.sh

# 8. Test in browser
# Open http://localhost:5173
# Login: student1 / password123
```

---

## 🔄 Reset & Restart

### **Reset Database:**

```bash
# Stop services
docker compose -f docker-compose.dev.yml down

# Remove volumes (deletes all data)
docker compose -f docker-compose.dev.yml down -v

# Start fresh
./test-with-docker.sh
```

### **Reset Only Data:**

```bash
# Keep containers running
# Just re-import test data
./import-test-data.sh
```

---

## 🐛 Troubleshooting

### **Script Permission Denied:**

```bash
# Make all scripts executable
chmod +x setup-database.sh
chmod +x import-test-data.sh
chmod +x test-with-docker.sh
chmod +x test-api.sh
```

### **MariaDB Not Ready:**

```bash
# Check if container is running
docker ps | grep mariadb

# Check logs
docker logs fctf-mariadb

# Restart container
docker restart fctf-mariadb
```

### **Password Hash Not Updated:**

```bash
# Error message:
# "Warning: test-data.sql still contains REPLACE_WITH_HASH"

# Solution:
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy hash and update test-data.sql
```

### **Test API Fails:**

```bash
# Check if backend is running
curl http://localhost:5000/healthcheck

# Check if test data is imported
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "SELECT COUNT(*) FROM users;"
```

---

## 📝 Script Locations

```
.
├── setup-database.sh           # Setup database
├── import-test-data.sh         # Import test data
├── test-with-docker.sh         # Complete automation
├── test-api.sh                 # API testing
├── test-data.sql               # Test data SQL
└── docker-compose.dev.yml      # Docker services
```

---

## ✅ Verification Commands

### **Check Docker Services:**

```bash
docker compose -f docker-compose.dev.yml ps
```

### **Check Database:**

```bash
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "
SELECT 'Users' as Table_Name, COUNT(*) as Count FROM users
UNION ALL SELECT 'Contests', COUNT(*) FROM contests
UNION ALL SELECT 'Participants', COUNT(*) FROM contest_participants
UNION ALL SELECT 'Challenges', COUNT(*) FROM challenges
UNION ALL SELECT 'Contest Challenges', COUNT(*) FROM contests_challenges;
"
```

### **Check Redis:**

```bash
docker exec -it fctf-redis redis-cli -a redis_password KEYS '*'
```

### **Check RabbitMQ:**

```bash
# Management UI
open http://localhost:15672
# Username: admin
# Password: rabbitmq_password
```

---

## 🎯 Quick Reference

| Script | Purpose | Prerequisites |
|--------|---------|---------------|
| `setup-database.sh` | Create database | Docker running |
| `import-test-data.sh` | Import test data | Database created, hash updated |
| `test-with-docker.sh` | Complete setup | Docker installed |
| `test-api.sh` | Test APIs | Backend running, data imported |

---

## 💡 Tips

1. **Use `test-with-docker.sh` for first-time setup** - It automates everything
2. **Run `test-api.sh` after starting backend** - Quick verification
3. **Check script output** - All scripts provide detailed feedback
4. **Use verification commands** - Confirm data is imported correctly
5. **Keep scripts executable** - Run `chmod +x` once

---

**Happy Testing! 🚀**

