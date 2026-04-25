# ⚡ Quick Restart Guide - Khi Database Đã Setup

## 📋 Khi Nào Dùng Guide Này?

Dùng guide này khi:
- ✅ Bạn đã chạy migrations rồi (`alembic upgrade head`)
- ✅ Database đã có schema
- ✅ Test data đã import (hoặc không cần test data)
- ✅ Chỉ cần restart backend/frontend để test

---

## 🚀 Quick Restart (3 Bước)

### **Bước 1: Đảm Bảo Docker Services Đang Chạy**

```bash
# Check services
docker compose -f docker-compose.dev.yml ps

# Nếu chưa chạy, start services
docker compose -f docker-compose.dev.yml up -d
```

**Expected output:**
```
NAME              STATUS         PORTS
fctf-mariadb      Up (healthy)   0.0.0.0:3306->3306/tcp
fctf-redis        Up (healthy)   0.0.0.0:6379->6379/tcp
fctf-rabbitmq     Up (healthy)   0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
```

### **Bước 2: Start Backend** (Terminal 1)

```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

### **Bước 3: Start Frontend** (Terminal 2)

```bash
cd ContestantPortal
npm run dev
```

**Expected output:**
```
  VITE v5.x.x  ready in xxx ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
```

### **Bước 4: Test!**

```
Open: http://localhost:5173
Login: student1 / password123
```

---

## 🔍 Verification (Optional)

### **Check Database:**

```bash
# Check if database exists
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password -e "SHOW DATABASES LIKE 'fctf_multiple_contest';"

# Check tables
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "SHOW TABLES;"

# Check users
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "SELECT COUNT(*) FROM users;"
```

### **Check Redis:**

```bash
docker exec -it fctf-redis redis-cli -a redis_password ping
```

### **Check Backend:**

```bash
curl http://localhost:5000/healthcheck
```

---

## 📊 Khi Nào Cần Import Test Data?

### **Check nếu đã có test data:**

```bash
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "
SELECT 'Users' as Table_Name, COUNT(*) as Count FROM users
UNION ALL SELECT 'Contests', COUNT(*) FROM contests
UNION ALL SELECT 'Challenges', COUNT(*) FROM challenges;
"
```

### **Nếu chưa có data (Count = 0):**

```bash
# 1. Generate password hash (nếu chưa)
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy hash
cd ../..

# 2. Update test-data.sql
# Replace REPLACE_WITH_HASH with hash

# 3. Import test data
./import-test-data.sh
```

---

## 🔄 Complete Workflow Comparison

### **First Time Setup (Full):**
```bash
./test-with-docker.sh  # Làm tất cả
```

### **Already Setup (Quick Restart):**
```bash
# 1. Check Docker
docker compose -f docker-compose.dev.yml ps

# 2. Start backend
cd ControlCenterAndChallengeHostingServer/ContestantBE && dotnet run

# 3. Start frontend (new terminal)
cd ContestantPortal && npm run dev
```

---

## 🛑 Stop Services

### **Stop Backend/Frontend:**
```
Ctrl+C in each terminal
```

### **Stop Docker Services:**
```bash
# Stop (keep data)
docker compose -f docker-compose.dev.yml stop

# Stop and remove containers (keep data)
docker compose -f docker-compose.dev.yml down

# Stop and remove everything including data
docker compose -f docker-compose.dev.yml down -v
```

---

## 🔄 Restart Docker Services Only

```bash
# Restart all services
docker compose -f docker-compose.dev.yml restart

# Restart specific service
docker compose -f docker-compose.dev.yml restart mariadb
docker compose -f docker-compose.dev.yml restart redis
docker compose -f docker-compose.dev.yml restart rabbitmq
```

---

## 🐛 Troubleshooting

### **Problem: Backend can't connect to database**

```bash
# Check if MariaDB is running
docker ps | grep mariadb

# Check connection
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "SELECT 1;"

# Restart MariaDB
docker compose -f docker-compose.dev.yml restart mariadb
```

### **Problem: Frontend can't connect to backend**

```bash
# Check if backend is running
curl http://localhost:5000/healthcheck

# Check backend .env file
cat ControlCenterAndChallengeHostingServer/ContestantBE/.env

# Check frontend .env.local
cat ContestantPortal/.env.local
```

### **Problem: Redis connection error**

```bash
# Check if Redis is running
docker exec -it fctf-redis redis-cli -a redis_password ping

# Restart Redis
docker compose -f docker-compose.dev.yml restart redis
```

---

## 📝 Daily Development Workflow

### **Morning (Start Work):**
```bash
# 1. Start Docker services
docker compose -f docker-compose.dev.yml up -d

# 2. Start backend (terminal 1)
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run

# 3. Start frontend (terminal 2)
cd ContestantPortal
npm run dev

# 4. Start coding!
```

### **Evening (End Work):**
```bash
# 1. Stop backend/frontend (Ctrl+C in terminals)

# 2. Stop Docker services (optional - can keep running)
docker compose -f docker-compose.dev.yml stop
```

---

## 🎯 Quick Commands

```bash
# Check Docker services
docker compose -f docker-compose.dev.yml ps

# Start Docker services
docker compose -f docker-compose.dev.yml up -d

# Stop Docker services
docker compose -f docker-compose.dev.yml stop

# Restart Docker services
docker compose -f docker-compose.dev.yml restart

# View logs
docker compose -f docker-compose.dev.yml logs -f

# Start backend
cd ControlCenterAndChallengeHostingServer/ContestantBE && dotnet run

# Start frontend
cd ContestantPortal && npm run dev

# Test API
./test-api.sh
```

---

## ✅ Checklist

### **Before Starting:**
- [ ] Docker services running
- [ ] Database has schema (migrations done)
- [ ] Test data imported (optional)
- [ ] Backend .env configured
- [ ] Frontend .env.local configured

### **After Starting:**
- [ ] Backend running on port 5000
- [ ] Frontend running on port 5173
- [ ] Can access http://localhost:5173
- [ ] Can login with test account

---

## 💡 Tips

1. **Keep Docker running** - Không cần stop Docker services mỗi lần
2. **Use separate terminals** - Dễ xem logs của backend và frontend
3. **Check logs** - Nếu có lỗi, xem logs trong terminal
4. **Use test-api.sh** - Quick verification sau khi start
5. **Bookmark http://localhost:5173** - Dễ access

---

## 🎉 Summary

**Nếu database đã setup:**
```bash
# Chỉ cần 3 bước:
1. docker compose -f docker-compose.dev.yml up -d
2. cd ControlCenterAndChallengeHostingServer/ContestantBE && dotnet run
3. cd ContestantPortal && npm run dev  # (terminal mới)
```

**Không cần:**
- ❌ Chạy migrations lại
- ❌ Import test data lại (nếu đã có)
- ❌ Generate password hash lại
- ❌ Update test-data.sql lại

**Chỉ cần:**
- ✅ Start Docker services
- ✅ Start backend
- ✅ Start frontend
- ✅ Test!

---

**Happy Coding! 🚀**

*Last Updated: 2024*
*Version: 1.0*

