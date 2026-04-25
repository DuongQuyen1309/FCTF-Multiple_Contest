# 📊 Import Sample Contest Data

## Mục đích

Import dữ liệu contest mẫu để test hệ thống CTF.

## Dữ liệu sẽ được tạo

### 5 Contests:
1. **Spring CTF 2024** - Beginner-friendly CTF (3 challenges)
2. **Summer Security Challenge** - Advanced challenges (3 challenges)
3. **Fall Hacking Contest** - Intermediate level (0 challenges - để bạn tự thêm)
4. **Winter Cyber Games** - Year-end competition (0 challenges)
5. **Practice Arena** - Always-on practice (5 challenges)

### 5 Sample Challenges:
1. **Welcome Challenge** (Misc, 10 pts) - Flag: `FCTF{welcome_to_ctf}`
2. **Basic Web** (Web, 50 pts) - Flag: `FCTF{basic_web_exploit}`
3. **Crypto 101** (Crypto, 75 pts) - Flag: `FCTF{caesar_cipher_solved}`
4. **Binary Basics** (Pwn, 100 pts) - Flag: `FCTF{buffer_overflow_found}`
5. **Reverse Me** (Reverse, 150 pts) - Flag: `FCTF{reversed_successfully}`

### User Participation:
- User `student1` (ID=5) sẽ được add vào tất cả 5 contests

---

## Cách 1: Dùng PowerShell Script (Khuyến nghị)

### Bước 1: Đảm bảo MySQL client đã cài đặt

Kiểm tra:
```powershell
mysql --version
```

Nếu chưa có, download và cài đặt:
- **MariaDB Client**: https://mariadb.org/download/
- **MySQL Client**: https://dev.mysql.com/downloads/mysql/

Hoặc nếu dùng Docker, có thể exec vào container:
```powershell
docker exec -it <mariadb_container_name> mysql -u fctf_user -p
```

### Bước 2: Chạy script import

```powershell
.\import_sample_data.ps1
```

---

## Cách 2: Import thủ công qua MySQL Client

### Bước 1: Kết nối database

```bash
mysql -h localhost -P 3306 -u fctf_user -p
# Password: fctf_password
```

### Bước 2: Select database

```sql
USE ctfd;
```

### Bước 3: Copy và paste nội dung file `sample_contest_data.sql`

Hoặc:

```bash
mysql -h localhost -P 3306 -u fctf_user -p ctfd < sample_contest_data.sql
```

---

## Cách 3: Dùng GUI Tool (HeidiSQL, MySQL Workbench, DBeaver)

### Bước 1: Mở tool và kết nối

- **Host**: localhost
- **Port**: 3306
- **User**: fctf_user
- **Password**: fctf_password
- **Database**: ctfd

### Bước 2: Import SQL file

- **HeidiSQL**: File > Run SQL file > Chọn `sample_contest_data.sql`
- **MySQL Workbench**: File > Run SQL Script > Chọn `sample_contest_data.sql`
- **DBeaver**: SQL Editor > Open SQL Script > Execute

---

## Cách 4: Dùng Docker (nếu database chạy trong Docker)

### Bước 1: Copy SQL file vào container

```powershell
docker cp sample_contest_data.sql <container_name>:/tmp/
```

### Bước 2: Exec vào container và import

```powershell
docker exec -it <container_name> bash
mysql -u fctf_user -p ctfd < /tmp/sample_contest_data.sql
# Password: fctf_password
```

---

## Kiểm tra sau khi import

### 1. Kiểm tra contests

```sql
SELECT Id, Name, State, StartTime, EndTime FROM Contests;
```

### 2. Kiểm tra challenges

```sql
SELECT Id, Name, Category, Points, Difficulty FROM Challenges;
```

### 3. Kiểm tra student1 participations

```sql
SELECT c.Name AS ContestName, cp.JoinedAt
FROM ContestParticipants cp
JOIN Contests c ON cp.ContestId = c.Id
WHERE cp.UserId = 5;
```

### 4. Kiểm tra challenges trong contests

```sql
SELECT c.Name AS ContestName, ch.Name AS ChallengeName, ch.Points
FROM ContestsChallenges cc
JOIN Contests c ON cc.ContestId = c.Id
JOIN Challenges ch ON cc.ChallengeId = ch.Id
ORDER BY c.Name, ch.Points;
```

---

## Test trên Frontend

### Bước 1: Login

- URL: http://localhost:5173/login
- Username: `student1`
- Password: `test123`

### Bước 2: Xem danh sách contests

- URL: http://localhost:5173/contests
- Bạn sẽ thấy 5 contests

### Bước 3: Select một contest

Click vào một contest để vào contest page

### Bước 4: Xem challenges

- Vào tab "Challenges" để xem danh sách challenges
- Thử submit flag để test

---

## Xóa dữ liệu mẫu (nếu cần)

```sql
-- Xóa contest-challenge links
DELETE FROM ContestsChallenges WHERE ContestId IN (
    SELECT Id FROM Contests WHERE Name LIKE '%CTF%' OR Name LIKE '%Challenge%' OR Name LIKE '%Arena%'
);

-- Xóa participations
DELETE FROM ContestParticipants WHERE ContestId IN (
    SELECT Id FROM Contests WHERE Name LIKE '%CTF%' OR Name LIKE '%Challenge%' OR Name LIKE '%Arena%'
);

-- Xóa contests
DELETE FROM Contests WHERE Name LIKE '%CTF%' OR Name LIKE '%Challenge%' OR Name LIKE '%Arena%';

-- Xóa challenges
DELETE FROM Challenges WHERE Flag LIKE 'FCTF{%}';
```

---

## Troubleshooting

### Lỗi: "mysql command not found"

**Giải pháp**: Cài đặt MySQL/MariaDB client hoặc dùng GUI tool

### Lỗi: "Access denied for user"

**Giải pháp**: Kiểm tra lại username/password trong `.env` file

### Lỗi: "Unknown database 'ctfd'"

**Giải pháp**: Database chưa được tạo. Chạy migrations:
```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet ef database update
```

### Lỗi: "Duplicate entry"

**Giải pháp**: Dữ liệu đã tồn tại. Xóa dữ liệu cũ trước khi import lại.

---

## Notes

- Tất cả contests đều có state = `visible` để có thể test ngay
- User `student1` đã được add vào tất cả contests
- Challenges có flag đơn giản để dễ test
- Bạn có thể modify SQL file để thêm/bớt dữ liệu theo ý muốn

---

## Next Steps

Sau khi import xong, bạn có thể:

1. ✅ Test login flow
2. ✅ Test contest selection
3. ✅ Test challenge listing
4. ✅ Test flag submission
5. ✅ Test scoreboard
6. ✅ Test team features (nếu có)

Chúc bạn test vui vẻ! 🎉
