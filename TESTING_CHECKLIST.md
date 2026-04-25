# ✅ Testing Checklist - Multiple Contest System

## 📋 Setup Phase

### Environment Setup
- [ ] MySQL 10.11+ installed and running
- [ ] Redis 6.0+ installed and running
- [ ] .NET 6.0+ SDK installed
- [ ] Node.js 18+ and npm installed
- [ ] Git Bash or PowerShell available

### Database Setup
- [ ] Database `fctf_multiple_contest` created
- [ ] Schema imported successfully
- [ ] Password hash generated using GeneratePasswordHash tool
- [ ] Test data imported (test-data.sql with correct hashes)
- [ ] Verified users exist: `SELECT * FROM users;`
- [ ] Verified contests exist: `SELECT * FROM contests;`
- [ ] Verified participants exist: `SELECT * FROM contest_participants;`
- [ ] Verified challenges exist: `SELECT * FROM challenges;`

### Backend Setup
- [ ] `.env` file created in ContestantBE folder
- [ ] DB_CONNECTION configured correctly
- [ ] REDIS_CONNECTION_STRING configured
- [ ] PRIVATE_KEY set
- [ ] Dependencies restored: `dotnet restore`
- [ ] Project builds: `dotnet build`
- [ ] Backend starts: `dotnet run`
- [ ] Health check works: `curl http://localhost:5000/healthcheck`
- [ ] Swagger UI accessible: `http://localhost:5000/swagger`

### Frontend Setup
- [ ] Dependencies installed: `npm install`
- [ ] `.env.local` created OR `env.template.js` updated
- [ ] VITE_API_BASE_URL set to `http://localhost:5000`
- [ ] Frontend starts: `npm run dev`
- [ ] Frontend accessible: `http://localhost:5173`
- [ ] No console errors in browser DevTools

---

## 🧪 Functional Testing

### Test 1: Login Flow (No Contest Required)
- [ ] Navigate to `/login`
- [ ] Enter username: `student1`, password: `password123`
- [ ] Click "Login"
- [ ] **Expected:** Login successful
- [ ] **Expected:** Redirect to `/contests`
- [ ] **Expected:** Token in localStorage has `contestId = 0`
- [ ] **Verify:** Check DevTools > Application > Local Storage > auth_token
- [ ] **Verify:** Decode JWT at jwt.io - should have `{ userId, teamId: 0, contestId: 0 }`

### Test 2: Contest List Display
- [ ] After login, on `/contests` page
- [ ] **Expected:** See list of contests student1 participates in
- [ ] **Expected:** See "Test Contest 1" and "Test Contest 2"
- [ ] **Expected:** Each contest shows: name, description, dates, participant count, challenge count
- [ ] **Expected:** No "Create Contest" button (student is not admin/teacher)
- [ ] **Verify:** Network tab shows `GET /api/Contest/list` with 200 status

### Test 3: Contest Selection
- [ ] Click on "Test Contest 1"
- [ ] **Expected:** Loading indicator appears
- [ ] **Expected:** API call to `POST /auth/select-contest` with `{ contestId: 1 }`
- [ ] **Expected:** Receive new token with `contestId = 1`
- [ ] **Expected:** Redirect to `/contest/1/challenges`
- [ ] **Expected:** Success toast: "Contest selected"
- [ ] **Verify:** New token in localStorage
- [ ] **Verify:** Decode JWT - should have `{ userId, teamId: X, contestId: 1 }`
- [ ] **Verify:** user_info in localStorage has team information

### Test 4: View Challenges in Contest
- [ ] On `/contest/1/challenges` page
- [ ] **Expected:** See challenge categories/topics
- [ ] **Expected:** See challenges grouped by category
- [ ] **Expected:** Each challenge shows: name, points, solve status
- [ ] **Expected:** Can click on a challenge to view details
- [ ] **Verify:** API call `GET /challenge/by-topic` with JWT containing contestId
- [ ] **Verify:** Backend logs show correct contestId

### Test 5: Challenge Detail View
- [ ] Click on a challenge (e.g., "Web Challenge 1")
- [ ] **Expected:** Challenge detail modal/page opens
- [ ] **Expected:** See description, points, files (if any)
- [ ] **Expected:** See flag submission form
- [ ] **Expected:** See hints section (if any)
- [ ] **Verify:** API call `GET /challenge/{id}` successful

### Test 6: Flag Submission (Correct)
- [ ] In challenge detail, enter correct flag: `flag{sql_injection_basic}`
- [ ] Click "Submit"
- [ ] **Expected:** Success message: "Correct!"
- [ ] **Expected:** Points added to score
- [ ] **Expected:** Challenge marked as solved
- [ ] **Verify:** Database has submission record: `SELECT * FROM submissions WHERE type = 'correct';`
- [ ] **Verify:** Database has solve record: `SELECT * FROM solves;`

### Test 7: Flag Submission (Incorrect)
- [ ] In another challenge, enter wrong flag: `flag{wrong}`
- [ ] Click "Submit"
- [ ] **Expected:** Error message: "Incorrect"
- [ ] **Expected:** Attempts counter decremented (if max_attempts > 0)
- [ ] **Expected:** No points added
- [ ] **Verify:** Database has submission record: `SELECT * FROM submissions WHERE type = 'incorrect';`

### Test 8: Access Control - No Contest Selected
- [ ] Clear localStorage: `localStorage.clear()`
- [ ] Login again (will get token with contestId = 0)
- [ ] Try to navigate directly to `/contest/1/challenges`
- [ ] Try to call API: `GET /challenge/by-topic`
- [ ] **Expected:** Error response: "No contest selected"
- [ ] **Expected:** Frontend shows error message
- [ ] **Verify:** Backend returns 400 Bad Request

### Test 9: Contest Switching
- [ ] Login as student1
- [ ] Select "Test Contest 1"
- [ ] View challenges (should see Web, Crypto, Forensics)
- [ ] Click "Back to Contests" (arrow icon in header)
- [ ] Select "Test Contest 2"
- [ ] **Expected:** New token generated with contestId = 2
- [ ] **Expected:** View challenges (should see Pwn, Reverse, Misc - different from Contest 1)
- [ ] **Verify:** Token changed in localStorage
- [ ] **Verify:** Challenges are different

### Test 10: Scoreboard
- [ ] Navigate to `/contest/1/scoreboard`
- [ ] **Expected:** See teams/users ranked by score
- [ ] **Expected:** Team with solved challenges has points
- [ ] **Expected:** Scoreboard scoped to Contest 1 only
- [ ] **Verify:** API call `GET /scoreboard/top/200` successful

### Test 11: Instances Page
- [ ] Navigate to `/contest/1/instances`
- [ ] **Expected:** See list of active challenge instances (if any)
- [ ] **Expected:** Empty state if no instances
- [ ] **Verify:** API call `GET /challenge/instances` successful

### Test 12: Action Logs
- [ ] Navigate to `/contest/1/action-logs`
- [ ] **Expected:** See log of user actions (login, view challenge, submit flag, etc.)
- [ ] **Expected:** Logs scoped to current contest
- [ ] **Verify:** API call `GET /ActionLogs/get-logs-team` successful

---

## 👨‍💼 Admin Testing

### Test 13: Admin Login
- [ ] Logout
- [ ] Login with username: `admin`, password: `password123`
- [ ] **Expected:** Login successful
- [ ] **Expected:** Redirect to `/contests`
- [ ] **Expected:** See "Create Contest" button
- [ ] **Expected:** See ALL contests (not just participated ones)
- [ ] **Verify:** Admin sees more contests than student

### Test 14: Create Contest
- [ ] Click "Create Contest"
- [ ] Fill form:
  - Name: "New Test Contest"
  - Slug: "new-test-contest"
  - Description: "Testing contest creation"
  - Semester: "Spring 2024"
  - User Mode: "Individual Users"
  - Start Time: (select date)
  - End Time: (select date)
- [ ] Click "Create Contest"
- [ ] **Expected:** Success message
- [ ] **Expected:** Redirect to contest challenges page
- [ ] **Verify:** Database has new contest: `SELECT * FROM contests WHERE slug = 'new-test-contest';`

### Test 15: Pull Challenges - View Bank
- [ ] From contest list, click "Pull Challenges" on a contest
- [ ] **Expected:** See list of challenges from bank
- [ ] **Expected:** Each challenge shows: name, category, points, type, state
- [ ] **Expected:** Checkboxes to select challenges
- [ ] **Expected:** "Configure" button for each challenge
- [ ] **Verify:** API call `GET /api/Contest/bank/challenges` successful

### Test 16: Pull Challenges - Configure Challenge
- [ ] Select "Web Challenge 1" (check the checkbox)
- [ ] Click "Configure" button
- [ ] **Expected:** Configuration dialog opens
- [ ] Change settings:
  - Points: 200 (override from 100)
  - Max Attempts: 5
  - State: visible
  - Cooldown: 30 seconds
- [ ] Click "Save Configuration"
- [ ] **Expected:** Success message
- [ ] **Expected:** Challenge marked as "Configured"

### Test 17: Pull Challenges - Submit
- [ ] Select 2-3 challenges (including the configured one)
- [ ] Click "Pull X Challenge(s)"
- [ ] **Expected:** Success message
- [ ] **Expected:** Redirect to contest challenges page
- [ ] **Expected:** Pulled challenges appear in contest
- [ ] **Verify:** Database: `SELECT * FROM contests_challenges WHERE contest_id = X;`
- [ ] **Verify:** Configured challenge has value = 200 (overridden)
- [ ] **Verify:** Non-configured challenges have original values from bank

### Test 18: Import Participants - View
- [ ] From contest list, click "Import Users" on a contest
- [ ] **Expected:** See import form
- [ ] **Expected:** Text area for emails
- [ ] **Expected:** Role selector (Contestant/Jury/Challenge Writer)

### Test 19: Import Participants - Import Existing Users
- [ ] Enter emails:
  ```
  student3@test.com
  student4@test.com
  ```
- [ ] Select Role: "Contestant"
- [ ] Click "Import Participants"
- [ ] **Expected:** Success message
- [ ] **Expected:** Summary shows:
  - Total Emails: 2
  - Existing Users Added: 2
  - New Users Created: 0
- [ ] **Verify:** Database: `SELECT * FROM contest_participants WHERE user_id IN (SELECT id FROM users WHERE email IN ('student3@test.com', 'student4@test.com'));`

### Test 20: Import Participants - Import New User
- [ ] Click "Import More"
- [ ] Enter email: `newstudent@test.com`
- [ ] Click "Import Participants"
- [ ] **Expected:** Success message
- [ ] **Expected:** Summary shows:
  - New Users Created: 1
- [ ] **Verify:** Database: `SELECT * FROM users WHERE email = 'newstudent@test.com';`
- [ ] **Verify:** User has random password and verified = 0
- [ ] **Verify:** User added to contest_participants

### Test 21: Admin Contest Actions
- [ ] On contest list, each contest card should have action buttons
- [ ] **Expected:** "Pull Challenges" button
- [ ] **Expected:** "Import Users" button
- [ ] **Expected:** Buttons only visible for admin/owner

---

## 👨‍🏫 Teacher Testing

### Test 22: Teacher Login
- [ ] Logout
- [ ] Login with username: `teacher1`, password: `password123`
- [ ] **Expected:** Login successful
- [ ] **Expected:** See "Create Contest" button
- [ ] **Expected:** See contests teacher owns OR participates in
- [ ] **Expected:** Do NOT see all contests (unlike admin)

### Test 23: Teacher Create Contest
- [ ] Click "Create Contest"
- [ ] Create a new contest
- [ ] **Expected:** Contest created successfully
- [ ] **Expected:** Teacher is set as owner
- [ ] **Verify:** Database: `SELECT * FROM contests WHERE owner_id = (SELECT id FROM users WHERE email = 'teacher1@test.com');`

### Test 24: Teacher Manage Own Contest
- [ ] Find contest owned by teacher1
- [ ] **Expected:** See "Pull Challenges" and "Import Users" buttons
- [ ] Click "Pull Challenges"
- [ ] **Expected:** Can pull challenges
- [ ] Click "Import Users"
- [ ] **Expected:** Can import participants

### Test 25: Teacher Cannot Manage Other's Contest
- [ ] Find contest owned by admin or teacher2
- [ ] **Expected:** No "Pull Challenges" or "Import Users" buttons
- [ ] Try to access `/contest/X/pull-challenges` directly (where X is not owned by teacher1)
- [ ] **Expected:** Access denied or error

---

## 🔒 Security Testing

### Test 26: Token Isolation
- [ ] Login as student1
- [ ] Select Contest 1 (get token with contestId = 1)
- [ ] Copy token from localStorage
- [ ] Try to access Contest 2's challenges using Contest 1's token
- [ ] **Expected:** Error or no data (token is for Contest 1 only)

### Test 27: Redis Key Isolation
- [ ] Login as student1, select Contest 1
- [ ] Submit a flag
- [ ] Check Redis: `redis-cli KEYS contest:1:*`
- [ ] **Expected:** See keys prefixed with `contest:1:`
- [ ] Login as student2, select Contest 2
- [ ] Submit a flag
- [ ] Check Redis: `redis-cli KEYS contest:2:*`
- [ ] **Expected:** See keys prefixed with `contest:2:`
- [ ] **Verify:** Keys are isolated by contest

### Test 28: Participant Access Control
- [ ] Login as student1
- [ ] **Expected:** Only see contests where student1 is participant
- [ ] Try to access a contest where student1 is NOT participant
- [ ] **Expected:** Contest not in list
- [ ] Try to select that contest via API
- [ ] **Expected:** Error: "You are not registered for this contest"

### Test 29: Role-Based Access
- [ ] Login as student (regular user)
- [ ] **Expected:** No "Create Contest" button
- [ ] Try to access `/contest/create` directly
- [ ] **Expected:** Can access page but API will fail
- [ ] Try to call `POST /api/Contest/create`
- [ ] **Expected:** Error: "Only admin or teacher can create contests"

---

## 🔄 Data Integrity Testing

### Test 30: Challenge Independence
- [ ] Pull "Web Challenge 1" to Contest 1 with value = 100
- [ ] Pull "Web Challenge 1" to Contest 2 with value = 200
- [ ] **Expected:** Two separate records in contests_challenges
- [ ] **Expected:** Same bank_id but different contest_id
- [ ] **Expected:** Different values (100 vs 200)
- [ ] **Verify:** `SELECT * FROM contests_challenges WHERE bank_id = 1;`

### Test 31: Submission Isolation
- [ ] Student1 submits flag in Contest 1
- [ ] Student1 submits flag in Contest 2
- [ ] **Expected:** Two separate submission records
- [ ] **Expected:** Different contest_id
- [ ] **Expected:** Different contest_challenge_id
- [ ] **Verify:** `SELECT * FROM submissions WHERE user_id = (SELECT id FROM users WHERE email = 'student1@test.com');`

### Test 32: Score Isolation
- [ ] Student1 solves challenge in Contest 1
- [ ] Check scoreboard in Contest 1
- [ ] **Expected:** Student1 has points
- [ ] Check scoreboard in Contest 2
- [ ] **Expected:** Student1 has 0 points (different contest)
- [ ] **Verify:** Scores are independent per contest

---

## 🐛 Error Handling Testing

### Test 33: Invalid Contest Selection
- [ ] Login successfully
- [ ] Try to select non-existent contest: `POST /auth/select-contest` with `{ contestId: 99999 }`
- [ ] **Expected:** Error: "Contest not found"

### Test 34: Invalid Credentials
- [ ] Try to login with wrong password
- [ ] **Expected:** Error: "Invalid username or password"
- [ ] Try to login with non-existent user
- [ ] **Expected:** Error: "Invalid username or password"

### Test 35: Expired Token
- [ ] Login and get token
- [ ] Wait for token to expire (or manually set expiration in past)
- [ ] Try to access protected endpoint
- [ ] **Expected:** Error: "Unauthorized" or "Token expired"

### Test 36: Missing Required Fields
- [ ] Try to create contest without name
- [ ] **Expected:** Validation error
- [ ] Try to pull challenges with empty array
- [ ] **Expected:** Error: "Please select at least one challenge"

---

## 📊 Performance Testing

### Test 37: Multiple Concurrent Users
- [ ] Open 3 browser windows
- [ ] Login as different users in each
- [ ] Select different contests
- [ ] Submit flags simultaneously
- [ ] **Expected:** All submissions processed correctly
- [ ] **Expected:** No race conditions
- [ ] **Expected:** Redis keys don't collide

### Test 38: Large Contest List
- [ ] Create 20+ contests
- [ ] Login as admin
- [ ] View contest list
- [ ] **Expected:** Page loads in reasonable time
- [ ] **Expected:** All contests displayed
- [ ] **Expected:** No performance issues

---

## ✅ Final Verification

### Database Integrity
- [ ] All foreign keys valid
- [ ] No orphaned records
- [ ] Contests have valid owner_id
- [ ] Participants reference valid user_id and contest_id
- [ ] Contests_challenges reference valid contest_id and bank_id
- [ ] Submissions reference valid contest_id and contest_challenge_id

### Redis Integrity
- [ ] All keys have proper prefix `contest:{contestId}:`
- [ ] No keys without contest prefix
- [ ] TTL set correctly on temporary keys
- [ ] No memory leaks

### API Consistency
- [ ] All endpoints return consistent response format
- [ ] Error messages are clear and helpful
- [ ] HTTP status codes are correct
- [ ] JWT tokens are properly validated

### Frontend UX
- [ ] All pages load without errors
- [ ] Navigation works smoothly
- [ ] Loading states shown appropriately
- [ ] Error messages displayed to user
- [ ] Success messages shown
- [ ] No console errors

---

## 📝 Test Results Summary

**Date:** _______________

**Tester:** _______________

**Environment:**
- Backend Version: _______________
- Frontend Version: _______________
- Database: MySQL _______________
- Redis: _______________

**Results:**
- Total Tests: 38
- Passed: _____ / 38
- Failed: _____ / 38
- Skipped: _____ / 38

**Critical Issues Found:**
1. _______________________________________________
2. _______________________________________________
3. _______________________________________________

**Notes:**
_______________________________________________
_______________________________________________
_______________________________________________

**Sign-off:**
- [ ] All critical tests passed
- [ ] System ready for staging deployment
- [ ] Documentation updated

---

**Testing Complete! 🎉**
