# 🎯 FCTF Multiple Contest System

## ✅ Status: Implementation Complete - Ready for Testing

This repository contains the **FCTF Multiple Contest System** - a CTF platform that supports multiple independent contests with isolated data and access control.

---

## 🚀 Quick Start (5 Minutes)

```bash
# 1. Make scripts executable (first time only)
chmod +x *.sh

# 2. Generate password hash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy the hash, then go back to root
cd ../..

# 3. Update test-data.sql
# Replace all REPLACE_WITH_HASH with the hash from step 2

# 4. Run automated setup
./test-with-docker.sh

# 5. Start backend (new terminal)
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run

# 6. Start frontend (new terminal)
cd ContestantPortal
npm run dev

# 7. Test in browser
# Open http://localhost:5173
# Login: student1 / password123
```

---

## 📚 Documentation

### **Start Here:**
- 📖 **[START_HERE.md](START_HERE.md)** ⭐ - **Read this first!**
- 🇻🇳 **[TOM_TAT_TIENG_VIET.md](TOM_TAT_TIENG_VIET.md)** - Vietnamese summary
- 📜 **[SCRIPTS_GUIDE.md](SCRIPTS_GUIDE.md)** ⭐ - How to use scripts

### **Understanding:**
- 📊 [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - What was implemented
- 🏗️ [ARCHITECTURE_DIAGRAM.md](ARCHITECTURE_DIAGRAM.md) - System architecture
- 🔄 [MULTIPLE_CONTEST_FLOW.md](MULTIPLE_CONTEST_FLOW.md) - Detailed flow

### **Testing:**
- 🐳 [DOCKER_TESTING_GUIDE.md](DOCKER_TESTING_GUIDE.md) - Docker-based testing
- ⚡ [QUICK_START.md](QUICK_START.md) - Quick start guide
- 📋 [LOCAL_TESTING_GUIDE.md](LOCAL_TESTING_GUIDE.md) - Detailed guide
- ✅ [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md) - 38 test cases
- 📝 [TESTING_PROGRESS.md](TESTING_PROGRESS.md) - Track progress

### **Reference:**
- 📑 [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md) - Complete index
- 📁 [FILES_CREATED.md](FILES_CREATED.md) - List of all files
- 📚 [README_TESTING.md](README_TESTING.md) - Testing overview

---

## 🔧 Available Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `setup-database.sh` | Setup database | `./setup-database.sh` |
| `import-test-data.sh` | Import test data | `./import-test-data.sh` |
| `test-with-docker.sh` | Complete automation | `./test-with-docker.sh` |
| `test-api.sh` | Test APIs | `./test-api.sh` |

**See [SCRIPTS_GUIDE.md](SCRIPTS_GUIDE.md) for detailed usage.**

---

## 🧪 Test Accounts

| Username | Password | Role | Access |
|----------|----------|------|--------|
| admin | password123 | Admin | All contests |
| teacher1 | password123 | Teacher | Own + participated |
| student1 | password123 | Student | Contest 1, 2 |
| student2 | password123 | Student | Contest 1 |

---

## 🎯 Key Features

### **Multiple Contest Support:**
- ✅ Users can participate in multiple contests
- ✅ Each contest has isolated data
- ✅ Independent scoreboards per contest
- ✅ Contest-specific challenges

### **New Authentication Flow:**
```
Login (no contestId) → Select Contest → Access Resources
```

### **Role-Based Access:**
- **Admin:** Manage all contests
- **Teacher:** Create and manage own contests
- **Student:** Participate in assigned contests

### **Data Isolation:**
- JWT tokens include contestId
- Redis keys prefixed: `contest:{contestId}:`
- Database queries scoped by contestId
- Token from Contest A cannot access Contest B

---

## 🏗️ Architecture

```
Frontend (React/TypeScript)
    ↓
Backend (.NET/C#)
    ↓
┌─────────┬─────────┬──────────┐
│ MariaDB │  Redis  │ RabbitMQ │
└─────────┴─────────┴──────────┘
```

**See [ARCHITECTURE_DIAGRAM.md](ARCHITECTURE_DIAGRAM.md) for detailed diagrams.**

---

## 📊 What Can Be Tested

✅ Login without contestId  
✅ Contest selection  
✅ Contest management (create, list, view)  
✅ Challenge management (pull, configure)  
✅ Participant import  
✅ Flag submission (non-deploy)  
✅ Scoreboard  
✅ Access control  
✅ Data isolation  

❌ Cannot test locally: K8s features (challenge deployment, pods)

---

## 🐳 Docker Services

| Service | Port | Credentials |
|---------|------|-------------|
| MariaDB | 3306 | fctf_user / fctf_password |
| Redis | 6379 | redis_password |
| RabbitMQ | 5672, 15672 | admin / rabbitmq_password |

**Management UI:** http://localhost:15672

---

## 🔍 Verification

### **Check Services:**
```bash
docker compose -f docker-compose.dev.yml ps
```

### **Check Database:**
```bash
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "SELECT COUNT(*) FROM users;"
```

### **Check Redis:**
```bash
docker exec -it fctf-redis redis-cli -a redis_password KEYS '*'
```

### **Check Backend:**
```bash
curl http://localhost:5000/healthcheck
```

### **Test APIs:**
```bash
./test-api.sh
```

---

## 🐛 Troubleshooting

### **Scripts not executable:**
```bash
chmod +x *.sh
```

### **Password hash not updated:**
```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Update test-data.sql with the hash
```

### **Backend won't start:**
```bash
# Check MySQL
docker ps | grep mariadb

# Check connection
mysql -u fctf_user -pfctf_password -h localhost fctf_multiple_contest -e "SHOW TABLES;"
```

### **Frontend can't connect:**
```bash
# Check backend
curl http://localhost:5000/healthcheck

# Check .env.local
cat ContestantPortal/.env.local
```

**See troubleshooting sections in documentation for more details.**

---

## 📁 Project Structure

```
.
├── Documentation/          # All documentation files
│   ├── START_HERE.md      # Entry point ⭐
│   ├── SCRIPTS_GUIDE.md   # Scripts usage ⭐
│   └── ...
│
├── Scripts/               # Setup and test scripts
│   ├── setup-database.sh
│   ├── import-test-data.sh
│   ├── test-with-docker.sh
│   └── test-api.sh
│
├── Data/
│   └── test-data.sql      # Test data
│
├── ControlCenterAndChallengeHostingServer/
│   └── ContestantBE/      # Backend (.NET)
│
├── ContestantPortal/      # Frontend (React)
│
└── docker-compose.dev.yml # Docker services
```

---

## 🎯 Next Steps

1. ✅ Read [START_HERE.md](START_HERE.md)
2. ✅ Read [SCRIPTS_GUIDE.md](SCRIPTS_GUIDE.md)
3. ✅ Follow Quick Start above
4. ✅ Run `./test-api.sh`
5. ✅ Test in browser
6. ✅ Follow [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md)
7. ⏭️ Deploy to staging

---

## 📞 Support

- **Quick start:** [START_HERE.md](START_HERE.md)
- **Scripts help:** [SCRIPTS_GUIDE.md](SCRIPTS_GUIDE.md)
- **Vietnamese:** [TOM_TAT_TIENG_VIET.md](TOM_TAT_TIENG_VIET.md)
- **Docker setup:** [DOCKER_TESTING_GUIDE.md](DOCKER_TESTING_GUIDE.md)
- **Testing:** [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md)

---

## 📊 Statistics

- **Total Documentation:** 14 files (~180 KB)
- **Scripts:** 4 files
- **Test Cases:** 38
- **Test Accounts:** 7
- **Test Contests:** 3
- **Test Challenges:** 10

---

## ✅ Implementation Status

- ✅ Backend APIs implemented
- ✅ Frontend pages implemented
- ✅ Authentication flow updated
- ✅ Access control implemented
- ✅ Data isolation implemented
- ✅ Testing documentation complete
- ✅ Test data ready
- ✅ Scripts ready

**Status: Ready for Testing! 🚀**

---

## 🏆 Key Changes

### **Authentication:**
- Login no longer requires contestId
- New contest selection step
- JWT token updated after selection

### **Contest Management:**
- Create multiple contests
- Pull challenges from bank
- Import participants via email
- Configure challenge properties

### **Data Isolation:**
- JWT includes contestId
- Redis keys prefixed
- Database queries scoped
- Token validation enforced

---

## 📝 License

[Your License Here]

---

## 👥 Contributors

[Your Team Here]

---

**Ready to start testing? Read [START_HERE.md](START_HERE.md)! 🚀**

*Last Updated: 2024*
*Version: 1.0*
*Status: Complete ✅*

