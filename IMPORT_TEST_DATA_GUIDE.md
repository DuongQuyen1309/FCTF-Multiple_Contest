# 📊 Import Test Data Guide

## 📋 Database Information

**Your Database Configuration:**
- **Database Name:** `ctfd`
- **User:** `fctf_user`
- **Password:** `fctf_password`
- **Root Password:** `root_password`

---

## 🚀 Quick Import (3 Steps)

### **Step 1: Generate Password Hash**

```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
```

**Output example:**
```
Password: password123
Hash: ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f
```

**Copy the hash!**

---

### **Step 2: Update test-data.sql**

Open `test-data.sql` and replace **ALL** occurrences of `REPLACE_WITH_HASH` with your hash:

**Before:**
```sql
INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', 'REPLACE_WITH_HASH', 'admin', 1, 0, 0, NOW()),
```

**After:**
```sql
INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'admin', 1, 0, 0, NOW()),
```

**Tip:** Use Find & Replace in your editor:
- Find: `REPLACE_WITH_HASH`
- Replace: `your_hash_here`

---

### **Step 3: Import Test Data**

**Option 1: Using Script (Recommended)**

```bash
./import-test-data.sh
```

**Option 2: Manual Import**

```bash
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password ctfd < test-data.sql
```

**Option 3: Interactive Import**

```bash
# Connect to database
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd

# In MySQL prompt
source /path/to/test-data.sql;

# Or if you're in the project directory
# Copy file to container first
docker cp test-data.sql fctf-mariadb:/tmp/test-data.sql

# Then import
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "source /tmp/test-data.sql"
```

---

## ✅ Verification

### **Check if data imported:**

```bash
# Check users
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT id, name, email, type FROM users;"

# Check contests
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT id, name, slug, state FROM contests;"

# Check counts
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "
SELECT 'Users' as Table_Name, COUNT(*) as Count FROM users
UNION ALL SELECT 'Contests', COUNT(*) FROM contests
UNION ALL SELECT 'Participants', COUNT(*) FROM contest_participants
UNION ALL SELECT 'Challenges', COUNT(*) FROM challenges
UNION ALL SELECT 'Contest Challenges', COUNT(*) FROM contests_challenges;
"
```

**Expected output:**
```
Table_Name          Count
Users               7
Contests            3
Participants        5
Challenges          10
Contest Challenges  9
```

---

## 🔍 What Test Data Contains

### **Users (7):**
| Username | Email | Password | Role |
|----------|-------|----------|------|
| admin | admin@test.com | password123 | Admin |
| teacher1 | teacher1@test.com | password123 | Teacher |
| teacher2 | teacher2@test.com | password123 | Teacher |
| student1 | student1@test.com | password123 | Student |
| student2 | student2@test.com | password123 | Student |
| student3 | student3@test.com | password123 | Student |
| student4 | student4@test.com | password123 | Student |

### **Contests (3):**
| Name | Slug | Owner | State |
|------|------|-------|-------|
| Test Contest 1 | test-contest-1 | admin | visible |
| Test Contest 2 | test-contest-2 | teacher1 | visible |
| Test Contest 3 | test-contest-3 | teacher2 | draft |

### **Challenges (10 in bank):**
- Web: SQL Injection, XSS, CSRF
- Crypto: Caesar Cipher, RSA
- Pwn: Buffer Overflow, Format String
- Reverse: Reverse Me
- Forensics: Hidden in Image
- Misc: OSINT Challenge

### **Contest Challenges:**
- Contest 1: 5 challenges (Web, Crypto, Forensics)
- Contest 2: 4 challenges (Pwn, Reverse, Misc)
- Contest 3: 0 challenges (empty)

---

## 🐛 Troubleshooting

### **Problem 1: REPLACE_WITH_HASH still in file**

**Error:**
```
Warning: test-data.sql still contains REPLACE_WITH_HASH
```

**Solution:**
```bash
# Generate hash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123

# Update test-data.sql with the hash
# Use Find & Replace in your editor
```

### **Problem 2: Database not found**

**Error:**
```
ERROR 1049 (42000): Unknown database 'ctfd'
```

**Solution:**
```bash
# Check if database exists
docker exec -it fctf-mariadb mysql -u root -proot_password -e "SHOW DATABASES;"

# If not exists, create it
docker exec -it fctf-mariadb mysql -u root -proot_password -e "CREATE DATABASE ctfd;"

# Or restart Docker services
docker compose -f docker-compose.dev.yml restart mariadb
```

### **Problem 3: Access denied**

**Error:**
```
ERROR 1045 (28000): Access denied for user 'fctf_user'@'localhost'
```

**Solution:**
```bash
# Check credentials in docker-compose.dev.yml
cat docker-compose.dev.yml | grep MARIADB

# Grant privileges
docker exec -it fctf-mariadb mysql -u root -proot_password -e "
GRANT ALL PRIVILEGES ON ctfd.* TO 'fctf_user'@'%';
FLUSH PRIVILEGES;
"
```

### **Problem 4: Duplicate entry**

**Error:**
```
ERROR 1062 (23000): Duplicate entry 'admin' for key 'users.name'
```

**Solution:** Data already exists. You can:

**Option A: Skip duplicates (recommended)**
```bash
# The test-data.sql already uses ON DUPLICATE KEY UPDATE
# Just re-run the import
./import-test-data.sh
```

**Option B: Clear existing data**
```bash
# WARNING: This will delete all data!
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE submissions;
TRUNCATE TABLE solves;
TRUNCATE TABLE contests_challenges;
TRUNCATE TABLE contest_participants;
TRUNCATE TABLE flags;
TRUNCATE TABLE challenges;
TRUNCATE TABLE contests;
TRUNCATE TABLE users;
SET FOREIGN_KEY_CHECKS = 1;
"

# Then import again
./import-test-data.sh
```

### **Problem 5: Table doesn't exist**

**Error:**
```
ERROR 1146 (42S02): Table 'ctfd.users' doesn't exist
```

**Solution:** Run migrations first!
```bash
# Run Alembic migrations
./run-migrations.sh

# Or manually
cd FCTF-ManagementPlatform
source .venv/bin/activate
alembic upgrade head
cd ..

# Then import test data
./import-test-data.sh
```

---

## 📝 Manual Import Steps (Detailed)

### **Method 1: Using Docker Exec (Recommended)**

```bash
# 1. Make sure Docker is running
docker ps | grep fctf-mariadb

# 2. Import test data
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password ctfd < test-data.sql

# 3. Verify
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT COUNT(*) FROM users;"
```

### **Method 2: Copy File to Container**

```bash
# 1. Copy file to container
docker cp test-data.sql fctf-mariadb:/tmp/test-data.sql

# 2. Import from inside container
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "source /tmp/test-data.sql"

# 3. Clean up
docker exec -it fctf-mariadb rm /tmp/test-data.sql
```

### **Method 3: Interactive MySQL Session**

```bash
# 1. Connect to MySQL
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd

# 2. In MySQL prompt, paste SQL commands manually
# Or use source command if file is accessible

# 3. Exit
exit;
```

---

## 🔄 Re-import Test Data

If you need to re-import test data:

```bash
# Option 1: Just re-run (uses ON DUPLICATE KEY UPDATE)
./import-test-data.sh

# Option 2: Clear and re-import
# WARNING: Deletes all data!
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE submissions;
TRUNCATE TABLE solves;
TRUNCATE TABLE contests_challenges;
TRUNCATE TABLE contest_participants;
TRUNCATE TABLE flags;
TRUNCATE TABLE challenges;
TRUNCATE TABLE contests;
TRUNCATE TABLE users;
SET FOREIGN_KEY_CHECKS = 1;
"

./import-test-data.sh
```

---

## ✅ Success Checklist

After importing test data, verify:

- [ ] 7 users created
- [ ] 3 contests created
- [ ] 5 contest participants
- [ ] 10 challenges in bank
- [ ] 9 contest challenges
- [ ] Can login with test accounts
- [ ] Can select contests
- [ ] Can view challenges

---

## 🎯 Quick Commands

```bash
# Import test data
./import-test-data.sh

# Check users
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT name, email, type FROM users;"

# Check contests
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT name, slug, state FROM contests;"

# Check all counts
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "
SELECT 'Users' as Info, COUNT(*) FROM users
UNION ALL SELECT 'Contests', COUNT(*) FROM contests
UNION ALL SELECT 'Challenges', COUNT(*) FROM challenges;
"

# Clear all data (WARNING!)
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE users;
TRUNCATE TABLE contests;
TRUNCATE TABLE challenges;
SET FOREIGN_KEY_CHECKS = 1;
"
```

---

**Happy Testing! 🚀**

*Database: ctfd*
*User: fctf_user*
*Password: fctf_password*

