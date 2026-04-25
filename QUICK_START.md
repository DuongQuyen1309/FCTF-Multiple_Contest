# 🚀 Quick Start Guide - Local Testing

## ⚡ TL;DR - Fastest Way to Test

```bash
# 1. Generate password hash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy the hash output

# 2. Setup database
mysql -u root -p
CREATE DATABASE fctf_multiple_contest;
USE fctf_multiple_contest;
# Import your schema
source path/to/schema.sql;
# Edit test-data.sql and replace REPLACE_WITH_HASH with the hash from step 1
source path/to/test-data.sql;
exit;

# 3. Start Redis
redis-server

# 4. Configure backend
cd ControlCenterAndChallengeHostingServer/ContestantBE
# Create .env file (see below)
dotnet run

# 5. Configure frontend (in new terminal)
cd ContestantPortal
npm install
# Create .env.local or update public/env.template.js
npm run dev

# 6. Test
# Open http://localhost:5173
# Login: student1 / password123
```

---

## 📝 Backend .env File

Create `ControlCenterAndChallengeHostingServer/ContestantBE/.env`:

```env
DB_CONNECTION=Server=localhost;Port=3306;Database=fctf_multiple_contest;User=root;Password=YOUR_MYSQL_PASSWORD;
REDIS_CONNECTION_STRING=localhost:6379
REDIS_TLS_INSECURE_SKIP_VERIFY=true
PRIVATE_KEY=test-secret-key-change-in-production
CLOUDFLARE_TURNSTILE_SECRET_KEY=
CLOUDFLARE_TURNSTILE_ENABLED=false
CTF_NAME=FCTF Test
```

---

## 📝 Frontend Configuration

**Option 1:** Create `ContestantPortal/.env.local`:
```env
VITE_API_BASE_URL=http://localhost:5000
VITE_CLOUDFLARE_TURNSTILE_SITE_KEY=
```

**Option 2:** Edit `ContestantPortal/public/env.template.js`:
```javascript
window.ENV = {
  API_BASE_URL: 'http://localhost:5000',
  CLOUDFLARE_TURNSTILE_SITE_KEY: '',
  BASE_GATEWAY: '',
  HTTP_PORT: '',
  TCP_PORT: ''
};
```

---

## 🧪 Test Accounts

After running `test-data.sql`:

| Username | Password | Role | Email |
|----------|----------|------|-------|
| admin | password123 | Admin | admin@test.com |
| teacher1 | password123 | Teacher | teacher1@test.com |
| student1 | password123 | Student | student1@test.com |
| student2 | password123 | Student | student2@test.com |

---

## 🎯 Test Scenarios

### 1. **Student Login & Contest Selection**
```
1. Go to http://localhost:5173/login
2. Login: student1 / password123
3. Should redirect to /contests
4. Click on "Test Contest 1"
5. Should redirect to /contest/1/challenges
6. View challenges
```

### 2. **Admin Create Contest**
```
1. Login as admin
2. Click "Create Contest"
3. Fill form and submit
4. Contest created successfully
```

### 3. **Admin Pull Challenges**
```
1. Login as admin
2. From contest list, click "Pull Challenges"
3. Select challenges
4. Configure settings
5. Click "Pull X Challenge(s)"
6. Challenges added to contest
```

### 4. **Admin Import Participants**
```
1. Login as admin
2. From contest list, click "Import Users"
3. Enter emails (one per line):
   student3@test.com
   student4@test.com
   newuser@test.com
4. Click "Import Participants"
5. Users added to contest
```

### 5. **Submit Flag**
```
1. Login as student1
2. Select contest
3. Click on a challenge
4. Enter flag: flag{test_flag}
5. Submit
6. Check if correct/incorrect
```

---

## 🔍 Verify Everything Works

### Check Backend:
```bash
# Health check
curl http://localhost:5000/healthcheck

# Swagger UI
open http://localhost:5000/swagger
```

### Check Database:
```sql
-- Check users
SELECT id, name, email, type FROM users;

-- Check contests
SELECT id, name, slug, state FROM contests;

-- Check participants
SELECT cp.*, u.name, c.name as contest_name
FROM contest_participants cp
JOIN users u ON cp.user_id = u.id
JOIN contests c ON cp.contest_id = c.id;

-- Check challenges in contests
SELECT cc.id, c.name as contest, cc.name as challenge, cc.value
FROM contests_challenges cc
JOIN contests c ON cc.contest_id = c.id;
```

### Check Redis:
```bash
redis-cli
127.0.0.1:6379> KEYS *
127.0.0.1:6379> KEYS contest:*
```

---

## 🐛 Common Issues

### Issue 1: Backend won't start
```bash
# Check MySQL is running
sudo service mysql status

# Check connection
mysql -u root -p -e "USE fctf_multiple_contest; SHOW TABLES;"
```

### Issue 2: Frontend can't connect
```bash
# Check backend is running
curl http://localhost:5000/healthcheck

# Check CORS in backend Program.cs
# Should have: app.UseCors("AllowAll");
```

### Issue 3: Login fails
```bash
# Check password hash is correct
# Use GeneratePasswordHash tool to generate hash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123

# Update database with correct hash
mysql -u root -p
USE fctf_multiple_contest;
UPDATE users SET password = 'YOUR_HASH' WHERE email = 'student1@test.com';
```

### Issue 4: No contests showing
```sql
-- Check user is participant
SELECT * FROM contest_participants WHERE user_id = (SELECT id FROM users WHERE email = 'student1@test.com');

-- Add user to contest
INSERT INTO contest_participants (contest_id, user_id, role, score, joined_at)
VALUES (1, (SELECT id FROM users WHERE email = 'student1@test.com'), 'contestant', 0, NOW());
```

---

## 📊 Test API with Script

```bash
# Make script executable
chmod +x test-api.sh

# Run tests
./test-api.sh
```

This will test:
- ✅ Health check
- ✅ Login (student)
- ✅ Get contests
- ✅ Select contest
- ✅ Get challenges
- ✅ Access control
- ✅ Admin login
- ✅ Admin features

---

## 🎉 Success Checklist

- [ ] MySQL running and database created
- [ ] Redis running
- [ ] Backend running on port 5000
- [ ] Frontend running on port 5173
- [ ] Can login as student
- [ ] Can see contest list
- [ ] Can select contest
- [ ] Can view challenges
- [ ] Can login as admin
- [ ] Can create contest
- [ ] Can pull challenges
- [ ] Can import participants

---

## 📚 Next Steps

1. ✅ Test basic flow (login → select contest → view challenges)
2. ✅ Test admin features (create, pull, import)
3. ✅ Test flag submission (non-deploy challenges)
4. ✅ Test scoreboard
5. ✅ Test contest switching
6. ⏭️ Deploy to staging for K8s testing

---

## 💡 Tips

- Use **Swagger UI** (`http://localhost:5000/swagger`) to test APIs directly
- Use **Browser DevTools** to inspect network requests and localStorage
- Use **Redis CLI** to monitor cache keys
- Use **MySQL Workbench** for database inspection
- Check **backend console** for logs and errors

---

## 🆘 Need Help?

1. Check `LOCAL_TESTING_GUIDE.md` for detailed instructions
2. Check `MULTIPLE_CONTEST_FLOW.md` for architecture details
3. Check backend logs for errors
4. Check browser console for frontend errors
5. Verify database has test data

---

**Happy Testing! 🚀**
