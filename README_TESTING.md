# 📚 FCTF Multiple Contest - Testing Documentation

## 📖 Tài liệu hướng dẫn test hệ thống Multiple Contest

Hệ thống đã được chuyển đổi từ **single contest** sang **multiple contest** architecture. Tài liệu này hướng dẫn chi tiết cách test ở local.

---

## 📂 Cấu trúc tài liệu

### 1. **QUICK_START.md** ⚡
**Mục đích:** Hướng dẫn nhanh nhất để bắt đầu test

**Nội dung:**
- TL;DR - Các bước tối thiểu để chạy hệ thống
- Cấu hình backend (.env)
- Cấu hình frontend (.env.local)
- Test accounts
- Test scenarios cơ bản
- Common issues và solutions

**Khi nào dùng:** Khi bạn muốn setup và test nhanh nhất có thể

---

### 2. **LOCAL_TESTING_GUIDE.md** 📋
**Mục đích:** Hướng dẫn chi tiết từng bước

**Nội dung:**
- Chuẩn bị môi trường (MySQL, Redis, .NET, Node.js)
- Setup database chi tiết
- Setup Redis
- Chạy backend
- Chạy frontend
- 10 test scenarios chi tiết
- Troubleshooting guide
- Monitoring & debugging tips

**Khi nào dùng:** Khi bạn cần hướng dẫn chi tiết từng bước

---

### 3. **MULTIPLE_CONTEST_FLOW.md** 🔄
**Mục đích:** Giải thích kiến trúc và flow mới

**Nội dung:**
- Overview về multiple contest architecture
- Authentication flow (old vs new)
- Key changes (backend & frontend)
- Security & access control
- Database schema
- Redis key structure
- API documentation
- Usage examples

**Khi nào dùng:** Khi bạn muốn hiểu kiến trúc và cách hoạt động của hệ thống

---

### 4. **TESTING_CHECKLIST.md** ✅
**Mục đích:** Checklist đầy đủ để verify tất cả features

**Nội dung:**
- Setup phase checklist
- 38 test cases chi tiết
- Functional testing
- Admin testing
- Teacher testing
- Security testing
- Data integrity testing
- Error handling testing
- Performance testing
- Final verification

**Khi nào dùng:** Khi bạn muốn test toàn diện và đảm bảo không bỏ sót gì

---

### 5. **test-data.sql** 💾
**Mục đích:** SQL script để tạo test data

**Nội dung:**
- Insert test users (admin, teachers, students)
- Insert test semesters
- Insert test contests
- Insert test teams
- Insert contest participants
- Insert test challenges (bank)
- Insert flags
- Pull challenges to contests
- Verification queries

**Khi nào dùng:** Sau khi tạo database, chạy script này để có data test

---

### 6. **test-api.sh** 🧪
**Mục đích:** Bash script để test APIs tự động

**Nội dung:**
- Test health check
- Test login
- Test get contests
- Test select contest
- Test get challenges
- Test access control
- Test admin features

**Khi nào dùng:** Để test nhanh các APIs mà không cần dùng frontend

---

### 7. **GeneratePasswordHash/** 🔐
**Mục đích:** Tool để generate password hash

**Nội dung:**
- C# console app
- Generate SHA256 hash cho passwords
- Support interactive mode và command-line args

**Khi nào dùng:** Để generate password hash cho test users trong database

---

## 🚀 Quy trình test khuyến nghị

### **Lần đầu tiên:**

```
1. Đọc QUICK_START.md
   ↓
2. Generate password hash (GeneratePasswordHash)
   ↓
3. Setup database và import test-data.sql
   ↓
4. Cấu hình backend (.env)
   ↓
5. Cấu hình frontend (.env.local)
   ↓
6. Chạy backend và frontend
   ↓
7. Test theo QUICK_START.md
   ↓
8. Nếu có vấn đề, xem LOCAL_TESTING_GUIDE.md
```

### **Test toàn diện:**

```
1. Hoàn thành setup theo QUICK_START.md
   ↓
2. Chạy test-api.sh để verify APIs
   ↓
3. Follow TESTING_CHECKLIST.md từng bước
   ↓
4. Mark các test cases đã pass
   ↓
5. Document các issues tìm được
   ↓
6. Fix issues và re-test
```

### **Hiểu kiến trúc:**

```
1. Đọc MULTIPLE_CONTEST_FLOW.md
   ↓
2. Xem database schema
   ↓
3. Xem API documentation
   ↓
4. Trace flow từ login → select contest → view challenges
   ↓
5. Hiểu JWT token structure
   ↓
6. Hiểu Redis key structure
```

---

## 🎯 Test priorities

### **Priority 1: Critical Flow** (Phải test trước)
- [ ] Login without contestId
- [ ] Redirect to contest list
- [ ] Select contest
- [ ] View challenges
- [ ] Submit flag

### **Priority 2: Admin Features** (Test sau)
- [ ] Create contest
- [ ] Pull challenges
- [ ] Import participants

### **Priority 3: Advanced Features** (Test cuối)
- [ ] Contest switching
- [ ] Scoreboard
- [ ] Action logs
- [ ] Security & access control

---

## 📊 Test coverage

| Feature | Test Cases | Priority |
|---------|-----------|----------|
| Authentication | 5 | P1 |
| Contest Management | 8 | P2 |
| Challenge Management | 6 | P1 |
| Flag Submission | 4 | P1 |
| Admin Features | 9 | P2 |
| Teacher Features | 4 | P2 |
| Security | 4 | P3 |
| Data Integrity | 3 | P3 |
| Error Handling | 4 | P3 |
| Performance | 2 | P3 |

**Total: 38 test cases**

---

## 🛠️ Tools cần thiết

### **Backend:**
- .NET 6.0+ SDK
- MySQL/MariaDB 10.11+
- Redis 6.0+

### **Frontend:**
- Node.js 18+
- npm

### **Testing:**
- curl (for API testing)
- jq (for JSON parsing)
- Browser DevTools
- Postman (optional)
- MySQL Workbench (optional)
- Redis Commander (optional)

---

## 📝 Test accounts

Sau khi import `test-data.sql`:

| Role | Username | Password | Email | Purpose |
|------|----------|----------|-------|---------|
| Admin | admin | password123 | admin@test.com | Test admin features |
| Teacher | teacher1 | password123 | teacher1@test.com | Test teacher features |
| Teacher | teacher2 | password123 | teacher2@test.com | Test multi-teacher |
| Student | student1 | password123 | student1@test.com | Test student flow |
| Student | student2 | password123 | student2@test.com | Test multi-student |
| Student | student3 | password123 | student3@test.com | Test team features |
| Student | student4 | password123 | student4@test.com | Test team features |

---

## 🎪 Test contests

| Contest | Slug | Owner | Challenges | Participants |
|---------|------|-------|------------|--------------|
| Test Contest 1 | test-contest-1 | admin | Web, Crypto, Forensics | student1, student2, student3 |
| Test Contest 2 | test-contest-2 | teacher1 | Pwn, Reverse, Misc | student1, student4 |
| Test Contest 3 | test-contest-3 | teacher2 | (empty) | (none) |

---

## 🚩 Test flags

| Challenge | Flag |
|-----------|------|
| Web Challenge 1 | `flag{sql_injection_basic}` |
| Web Challenge 2 | `flag{xss_vulnerability}` |
| Crypto Challenge 1 | `flag{caesar_cipher_decoded}` |
| Crypto Challenge 2 | `flag{rsa_decrypted}` |
| Pwn Challenge 1 | `flag{buffer_overflow_pwned}` |
| Reverse Challenge 1 | `flag{reversed_successfully}` |
| Forensics Challenge 1 | `flag{hidden_in_image}` |
| Misc Challenge 1 | `flag{osint_master}` |

---

## 🐛 Known limitations (Local testing)

### **Cannot test:**
- ❌ Challenge deployment (requires K8s)
- ❌ Pod management
- ❌ Dynamic challenge instances
- ❌ TCP challenges
- ❌ Challenge auto-stop

### **Can test:**
- ✅ Authentication flow
- ✅ Contest selection
- ✅ Contest management
- ✅ Challenge listing
- ✅ Flag submission (non-deploy)
- ✅ Scoreboard
- ✅ User management
- ✅ Access control
- ✅ Data isolation

---

## 📞 Support

### **Nếu gặp vấn đề:**

1. **Check logs:**
   - Backend console output
   - Browser DevTools console
   - MySQL error log
   - Redis log

2. **Check configuration:**
   - .env file (backend)
   - .env.local file (frontend)
   - Database connection
   - Redis connection

3. **Check documentation:**
   - LOCAL_TESTING_GUIDE.md (Troubleshooting section)
   - QUICK_START.md (Common Issues section)

4. **Verify data:**
   - Run verification queries in test-data.sql
   - Check Redis keys: `redis-cli KEYS *`
   - Check database tables

---

## ✅ Success criteria

Hệ thống được coi là test thành công khi:

- [ ] Tất cả P1 tests pass (Critical Flow)
- [ ] Ít nhất 80% P2 tests pass (Admin Features)
- [ ] Không có critical bugs
- [ ] Performance acceptable (< 2s response time)
- [ ] No console errors
- [ ] No database errors
- [ ] Redis working correctly

---

## 📅 Test schedule

### **Day 1: Setup & Basic Flow**
- Setup environment
- Import test data
- Test login flow
- Test contest selection
- Test challenge viewing

### **Day 2: Admin Features**
- Test create contest
- Test pull challenges
- Test import participants
- Test contest management

### **Day 3: Advanced & Security**
- Test contest switching
- Test access control
- Test data isolation
- Test error handling

### **Day 4: Full Regression**
- Run all 38 test cases
- Document results
- Fix critical issues
- Re-test

---

## 🎉 Next steps after local testing

1. ✅ All local tests pass
2. ⏭️ Deploy to staging environment
3. ⏭️ Test with K8s (challenge deployment)
4. ⏭️ Load testing
5. ⏭️ Security audit
6. ⏭️ Production deployment

---

## 📚 Additional resources

- **Swagger UI:** `http://localhost:5000/swagger`
- **JWT Decoder:** https://jwt.io
- **JSON Formatter:** https://jsonformatter.org
- **MySQL Workbench:** https://www.mysql.com/products/workbench/
- **Redis Commander:** https://github.com/joeferner/redis-commander

---

**Happy Testing! 🚀**

*Last updated: 2024*
*Version: 1.0*
