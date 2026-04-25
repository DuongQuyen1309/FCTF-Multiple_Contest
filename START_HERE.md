# 🚀 START HERE - FCTF Multiple Contest System

## 👋 Welcome!

This document is your starting point for understanding and testing the FCTF Multiple Contest System.

---

## 📚 What You Have

Your system has been successfully converted from **single contest** to **multiple contest** architecture. Everything is ready for testing!

### ✅ Implementation Status: **COMPLETE**

- ✅ Backend APIs implemented
- ✅ Frontend pages implemented
- ✅ Authentication flow updated
- ✅ Access control implemented
- ✅ Data isolation implemented
- ✅ Testing documentation created
- ✅ Test data and scripts ready

---

## 🎯 Quick Navigation

### **For Quick Start:**
1. 📖 Read this file (START_HERE.md) - **YOU ARE HERE**
2. 🇻🇳 Read [TOM_TAT_TIENG_VIET.md](TOM_TAT_TIENG_VIET.md) - Vietnamese summary
3. ⚡ Follow [QUICK_START.md](QUICK_START.md) - Fastest way to test
4. 🐳 Follow [DOCKER_TESTING_GUIDE.md](DOCKER_TESTING_GUIDE.md) - Docker setup

### **For Understanding:**
5. 📊 Read [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - What was done
6. 🏗️ Read [ARCHITECTURE_DIAGRAM.md](ARCHITECTURE_DIAGRAM.md) - System architecture
7. 🔄 Read [MULTIPLE_CONTEST_FLOW.md](MULTIPLE_CONTEST_FLOW.md) - Detailed flow

### **For Testing:**
8. ✅ Use [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md) - 38 test cases
9. 📝 Use [TESTING_PROGRESS.md](TESTING_PROGRESS.md) - Track your progress
10. 📋 Read [LOCAL_TESTING_GUIDE.md](LOCAL_TESTING_GUIDE.md) - Detailed guide

---

## 🚀 Get Started in 5 Minutes

### **First Time Setup:**

See full setup guide below.

### **Already Setup? Quick Restart:**

If you already ran migrations and setup database, see **[QUICK_RESTART.md](QUICK_RESTART.md)** ⚡

Just need:
```bash
# 1. Start Docker
docker compose -f docker-compose.dev.yml up -d

# 2. Start backend
cd ControlCenterAndChallengeHostingServer/ContestantBE && dotnet run

# 3. Start frontend (new terminal)
cd ContestantPortal && npm run dev
```

---

## 🔧 First Time Setup

### **Step 1: Make Scripts Executable** (first time only)
```bash
chmod +x setup-database.sh
chmod +x import-test-data.sh
chmod +x test-with-docker.sh
chmod +x test-api.sh
chmod +x run-migrations.sh
```

### **Step 2: Setup Python Environment** (for migrations)
```bash
cd FCTF-ManagementPlatform
python -m venv .venv
source .venv/bin/activate  # Linux/Mac
# or .venv\Scripts\activate  # Windows
pip install -r requirements.txt
cd ..
```

### **Step 3: Generate Password Hash**
```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy the hash output
cd ../..
```

### **Step 4: Update test-data.sql**
```bash
# Open test-data.sql and replace all REPLACE_WITH_HASH with the hash from step 3
```

### **Step 5: Run Automated Setup**
```bash
./test-with-docker.sh
# This will:
# - Start Docker services
# - Create database
# - Run Alembic migrations
# - Prompt for test data import
```

### **Step 5: Start Backend** (new terminal)
```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run
```

### **Step 6: Start Frontend** (new terminal)
```bash
cd ContestantPortal
npm run dev
```

### **Step 7: Test APIs** (optional)
```bash
./test-api.sh
```

### **Step 8: Test in Browser**
```
Open: http://localhost:5173
Login: student1 / password123
```

---

## 🔄 Database Migrations

**Important:** This system uses **Alembic** (Python) for database migrations.

See [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) for detailed migration instructions.

---

## 📖 Documentation Structure

```
START_HERE.md (YOU ARE HERE)
│
├── TOM_TAT_TIENG_VIET.md          # Vietnamese summary
├── IMPLEMENTATION_SUMMARY.md       # What was implemented
├── ARCHITECTURE_DIAGRAM.md         # System architecture
│
├── QUICK_START.md                  # Quick start guide
├── DOCKER_TESTING_GUIDE.md         # Docker-based testing
├── LOCAL_TESTING_GUIDE.md          # Detailed testing guide
├── MULTIPLE_CONTEST_FLOW.md        # Architecture & flow
│
├── TESTING_CHECKLIST.md            # 38 test cases
├── TESTING_PROGRESS.md             # Progress tracker
├── README_TESTING.md               # Testing overview
│
├── test-data.sql                   # Test data SQL
├── test-api.sh                     # API test script
├── docker-compose.dev.yml          # Docker setup
│
└── GeneratePasswordHash/           # Password hash tool
```

---

## 🎯 What Changed?

### **Before (Single Contest):**
```
Login (with contestId) → Access challenges directly
```

### **After (Multiple Contest):**
```
Login (no contestId) → Select Contest → Access challenges
```

### **Key Changes:**
1. ✅ Login no longer requires contestId
2. ✅ New contest selection step
3. ✅ JWT token includes contestId after selection
4. ✅ Each contest has isolated data
5. ✅ Admin/Teacher can manage multiple contests
6. ✅ Students can participate in multiple contests

---

## 🧪 Test Accounts

| Username | Password | Role | Can Access |
|----------|----------|------|------------|
| admin | password123 | Admin | All contests |
| teacher1 | password123 | Teacher | Own + participated contests |
| student1 | password123 | Student | Test Contest 1, 2 |
| student2 | password123 | Student | Test Contest 1 |

---

## 🎯 Test Flow

### **1. Student Flow (5 minutes):**
```
1. Login as student1
2. See contest list
3. Click "Test Contest 1"
4. View challenges
5. Submit a flag
```

### **2. Admin Flow (10 minutes):**
```
1. Login as admin
2. Create new contest
3. Pull challenges from bank
4. Configure challenge properties
5. Import participants
```

---

## ✅ What Can Be Tested

- ✅ Login without contestId
- ✅ Contest selection
- ✅ Contest management
- ✅ Challenge listing
- ✅ Flag submission (non-deploy)
- ✅ Scoreboard
- ✅ Access control
- ✅ Data isolation

---

## ❌ What Cannot Be Tested Locally

- ❌ Challenge deployment (needs K8s)
- ❌ Pod management
- ❌ Dynamic instances
- ❌ TCP challenges

**Note:** This is expected - K8s features need staging/production environment

---

## 🔍 Quick Verification

### **Check Backend:**
```bash
curl http://localhost:5000/healthcheck
```

### **Check Database:**
```sql
SELECT id, name, email, type FROM users;
SELECT id, name, slug, state FROM contests;
```

### **Check Redis:**
```bash
docker exec -it fctf-redis redis-cli -a redis_password KEYS '*'
```

---

## 📊 Testing Phases

### **Phase 1: Setup (5 items)**
- Start Docker services
- Setup database
- Import test data
- Start backend
- Start frontend

### **Phase 2: Authentication (5 items)**
- Login flow
- JWT tokens
- Contest selection
- Access control

### **Phase 3: Contest Management (8 items)**
- View contests
- Create contest
- Contest details
- Role-based access

### **Phase 4: Challenge Management (6 items)**
- View challenge bank
- Pull challenges
- Configure properties
- Challenge isolation

### **Phase 5: Flag Submission (4 items)**
- Submit correct flag
- Submit incorrect flag
- Submission scoping
- Access control

### **Phase 6-10: Advanced Features (10 items)**
- Participant management
- Security testing
- Data integrity
- Contest switching
- Performance testing

**Total: 38 test cases**

---

## 🐛 Common Issues & Solutions

### **Issue 1: Backend won't start**
```bash
# Check MySQL
docker ps | grep mariadb

# Check connection
mysql -u root -p -e "SHOW DATABASES;"
```

### **Issue 2: Frontend can't connect**
```bash
# Check backend
curl http://localhost:5000/healthcheck

# Check .env.local
cat ContestantPortal/.env.local
```

### **Issue 3: Login fails**
```bash
# Generate new password hash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123

# Update database
mysql -u root -p
UPDATE users SET password = 'YOUR_HASH' WHERE email = 'student1@test.com';
```

---

## 📞 Need Help?

### **Step 1: Check Documentation**
- Read [DOCKER_TESTING_GUIDE.md](DOCKER_TESTING_GUIDE.md) troubleshooting section
- Read [LOCAL_TESTING_GUIDE.md](LOCAL_TESTING_GUIDE.md) for detailed steps

### **Step 2: Check Logs**
- Backend console output
- Browser DevTools console
- Docker logs: `docker compose -f docker-compose.dev.yml logs -f`

### **Step 3: Verify Configuration**
- Backend .env file
- Frontend .env.local file
- Database connection
- Redis connection

### **Step 4: Check Data**
- Run verification queries in test-data.sql
- Check Redis keys: `redis-cli KEYS *`
- Check database tables

---

## 🎯 Success Criteria

Your testing is successful when:

- [ ] All P1 tests pass (Critical Flow)
- [ ] At least 80% P2 tests pass (Admin Features)
- [ ] No critical bugs
- [ ] Performance < 2s response time
- [ ] No console errors
- [ ] No database errors
- [ ] Redis working correctly

---

## 📅 Recommended Testing Schedule

### **Day 1: Setup & Basic Flow (2-3 hours)**
- Setup environment
- Import test data
- Test login flow
- Test contest selection
- Test challenge viewing

### **Day 2: Admin Features (2-3 hours)**
- Test create contest
- Test pull challenges
- Test import participants
- Test contest management

### **Day 3: Advanced & Security (2-3 hours)**
- Test contest switching
- Test access control
- Test data isolation
- Test error handling

### **Day 4: Full Regression (2-3 hours)**
- Run all 38 test cases
- Document results
- Fix critical issues
- Re-test

---

## 🎉 Next Steps After Testing

1. ✅ Complete local testing
2. ⏭️ Deploy to staging
3. ⏭️ Test with K8s
4. ⏭️ Load testing
5. ⏭️ Security audit
6. ⏭️ Production deployment

---

## 💡 Pro Tips

1. **Use Swagger UI** for API testing: `http://localhost:5000/swagger`
2. **Use Browser DevTools** to inspect network requests
3. **Use Redis CLI** to monitor cache keys
4. **Use MySQL Workbench** for database inspection
5. **Check backend console** for detailed logs
6. **Use test-api.sh** for automated API testing

---

## 📚 Key Files to Know

### **Backend:**
- `ContestantBE/Services/AuthService.cs` - Authentication logic
- `ContestantBE/Services/ContestService.cs` - Contest management
- `ContestantBE/Controllers/ContestController.cs` - Contest APIs
- `ContestantBE/Attribute/RequireContestAttribute.cs` - Access control

### **Frontend:**
- `src/pages/ContestList.tsx` - Contest list page
- `src/pages/CreateContest.tsx` - Create contest page
- `src/pages/PullChallenges.tsx` - Pull challenges page
- `src/pages/ImportParticipants.tsx` - Import participants page
- `src/context/AuthContext.tsx` - Authentication context

### **Testing:**
- `test-data.sql` - Test data
- `test-api.sh` - API test script
- `docker-compose.dev.yml` - Docker setup

---

## 🏆 Summary

✅ **Implementation:** Complete  
✅ **Documentation:** Complete  
✅ **Test Data:** Ready  
✅ **Scripts:** Ready  
⏳ **Testing:** Ready to start  

**You have everything you need to start testing!**

---

## 🚀 Ready to Start?

### **Option 1: Quick Test (5 minutes)**
Follow [QUICK_START.md](QUICK_START.md)

### **Option 2: Docker Test (10 minutes)**
Follow [DOCKER_TESTING_GUIDE.md](DOCKER_TESTING_GUIDE.md)

### **Option 3: Comprehensive Test (1-2 days)**
Follow [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md)

---

## 📞 Questions?

- **Vietnamese summary:** [TOM_TAT_TIENG_VIET.md](TOM_TAT_TIENG_VIET.md)
- **Implementation details:** [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
- **Architecture:** [ARCHITECTURE_DIAGRAM.md](ARCHITECTURE_DIAGRAM.md)
- **Testing guide:** [DOCKER_TESTING_GUIDE.md](DOCKER_TESTING_GUIDE.md)

---

**Good luck with your testing! 🚀**

*If you encounter any issues, check the troubleshooting sections in the documentation.*

---

**Last Updated:** 2024  
**Version:** 1.0  
**Status:** Ready for Testing ✅

