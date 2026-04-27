# ⚡ Quick Import Test Data

## 🎯 3 Bước Đơn Giản

### **Bước 1: Generate Password Hash**
```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy hash
cd ../..
```

### **Bước 2: Update test-data.sql**
```
Mở test-data.sql
Find & Replace: REPLACE_WITH_HASH → your_hash
Save file
```

### **Bước 3: Import**
```bash
./import-test-data.sh
```

**Hoặc manual:**
```bash
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password ctfd < test-data.sql
```

---

## ✅ Verify

```bash
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "
SELECT 'Users' as Info, COUNT(*) FROM users
UNION ALL SELECT 'Contests', COUNT(*) FROM contests
UNION ALL SELECT 'Challenges', COUNT(*) FROM challenges;
"
```

**Expected:**
```
Info        COUNT(*)
Users       7
Contests    3
Challenges  10
```

---

## 🔑 Test Accounts

| Username | Password | Role |
|----------|----------|------|
| admin | password123 | Admin |
| teacher1 | password123 | Teacher |
| student1 | password123 | Student |
| student2 | password123 | Student |

---

## 🐛 Troubleshooting

**Hash not updated?**
```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Update test-data.sql with hash
```

**Database not found?**
```bash
docker compose -f docker-compose.dev.yml restart mariadb
```

**Table doesn't exist?**
```bash
./run-migrations.sh
```

---

**See full guide:** [IMPORT_TEST_DATA_GUIDE.md](IMPORT_TEST_DATA_GUIDE.md)

