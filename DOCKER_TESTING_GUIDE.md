# 🐳 Docker Testing Guide - FCTF Multiple Contest

## 📋 Tổng quan

Hướng dẫn này giúp bạn test hệ thống Multiple Contest sử dụng Docker cho infrastructure (MariaDB, Redis, RabbitMQ) và chạy backend/frontend native.

---

## 🚀 Quick Start

```bash
# 1. Start infrastructure
docker compose -f docker-compose.dev.yml up -d

# 2. Wait for services to be healthy
docker compose -f docker-compose.dev.yml ps

# 3. Setup database
./setup-database.sh

# 4. Start backend (terminal 1)
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run

# 5. Start frontend (terminal 2)
cd ContestantPortal
npm run dev

# 6. Open browser
# http://localhost:5173
```

---

## 📦 Services trong Docker

### **1. MariaDB** (Port 3306)
- **Container:** `fctf-mariadb`
- **Database:** `ctfd`
- **Root Password:** `root_password`
- **User:** `fctf_user`
- **Password:** `fctf_password`
- **Data Volume:** `mariadb_data`

### **2. Redis** (Port 6379)
- **Container:** `fctf-redis`
- **Password:** `redis_password`
- **Data Volume:** `redis_data`

### **3. RabbitMQ** (Port 5672, 15672)
- **Container:** `fctf-rabbitmq`
- **AMQP Port:** 5672
- **Management UI:** http://localhost:15672
- **Username:** `admin`
- **Password:** `rabbitmq_password`
- **VHost:** `fctf_deploy`
- **Data Volume:** `rabbitmq_data`

---

## 🔧 Bước 1: Start Infrastructure

### **Start tất cả services:**
```bash
docker compose -f docker-compose.dev.yml up -d
```

### **Check status:**
```bash
docker compose -f docker-compose.dev.yml ps
```

**Expected output:**
```
NAME              IMAGE                      STATUS         PORTS
fctf-mariadb      bitnami/mariadb:latest     Up (healthy)   0.0.0.0:3306->3306/tcp
fctf-redis        bitnami/redis:latest       Up (healthy)   0.0.0.0:6379->6379/tcp
fctf-rabbitmq     rabbitmq:3-management      Up (healthy)   0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
```

### **View logs:**
```bash
# All services
docker compose -f docker-compose.dev.yml logs -f

# Specific service
docker compose -f docker-compose.dev.yml logs -f mariadb
docker compose -f docker-compose.dev.yml logs -f redis
docker compose -f docker-compose.dev.yml logs -f rabbitmq
```

---

## 🗄️ Bước 2: Setup Database

### **Option 1: Sử dụng script tự động**

Tạo file `setup-database.sh`:

```bash
#!/bin/bash

echo "=== FCTF Database Setup ==="

# Wait for MariaDB to be ready
echo "Waiting for MariaDB to be ready..."
until docker exec fctf-mariadb mysqladmin ping -h localhost -u root -proot_password --silent; do
    echo "MariaDB is unavailable - sleeping"
    sleep 2
done

echo "MariaDB is ready!"

# Create database for multiple contest
echo "Creating database..."
docker exec -i fctf-mariadb mysql -u root -proot_password <<EOF
CREATE DATABASE IF NOT EXISTS fctf_multiple_contest CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
GRANT ALL PRIVILEGES ON fctf_multiple_contest.* TO 'fctf_user'@'%';
FLUSH PRIVILEGES;
EOF

echo "Database created: fctf_multiple_contest"

# Import schema (if exists)
if [ -f "schema.sql" ]; then
    echo "Importing schema..."
    docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest < schema.sql
    echo "Schema imported!"
fi

# Generate password hash
echo ""
echo "=== Generating password hash ==="
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
cd ../..

echo ""
echo "Copy the hash above and update test-data.sql"
echo "Then run: ./import-test-data.sh"
```

**Make executable:**
```bash
chmod +x setup-database.sh
./setup-database.sh
```

### **Option 2: Manual setup**

```bash
# Connect to MariaDB
docker exec -it fctf-mariadb mysql -u root -proot_password

# Create database
CREATE DATABASE IF NOT EXISTS fctf_multiple_contest CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
GRANT ALL PRIVILEGES ON fctf_multiple_contest.* TO 'fctf_user'@'%';
FLUSH PRIVILEGES;
USE fctf_multiple_contest;

# Import schema
SOURCE /path/to/schema.sql;

# Exit
exit;
```

---

## 📊 Bước 3: Import Test Data

### **Generate password hash:**

```bash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
```

**Output example:**
```
Password: password123
Hash: ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f
```

### **Update test-data.sql:**

Mở file `test-data.sql` và replace `REPLACE_WITH_HASH` với hash vừa generate:

```sql
-- Before
INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', 'REPLACE_WITH_HASH', 'admin', 1, 0, 0, NOW()),

-- After
INSERT INTO users (name, email, password, type, verified, hidden, banned, created) VALUES
('admin', 'admin@test.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'admin', 1, 0, 0, NOW()),
```

### **Import test data:**

Tạo file `import-test-data.sh`:

```bash
#!/bin/bash

echo "=== Importing test data ==="

if [ ! -f "test-data.sql" ]; then
    echo "Error: test-data.sql not found"
    exit 1
fi

docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest < test-data.sql

echo "Test data imported!"
echo ""
echo "=== Verification ==="
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "
SELECT 'Users:' as Info, COUNT(*) as Count FROM users
UNION ALL
SELECT 'Contests:', COUNT(*) FROM contests
UNION ALL
SELECT 'Participants:', COUNT(*) FROM contest_participants
UNION ALL
SELECT 'Challenges:', COUNT(*) FROM challenges
UNION ALL
SELECT 'Contest Challenges:', COUNT(*) FROM contests_challenges;
"
```

**Run:**
```bash
chmod +x import-test-data.sh
./import-test-data.sh
```

---

## 🔐 Bước 4: Configure Backend

### **Create .env file:**

`ControlCenterAndChallengeHostingServer/ContestantBE/.env`:

```env
# Database (Docker MariaDB)
DB_CONNECTION=Server=localhost;Port=3306;Database=fctf_multiple_contest;User=fctf_user;Password=fctf_password;

# Redis (Docker Redis)
REDIS_CONNECTION_STRING=localhost:6379,password=redis_password
REDIS_TLS_INSECURE_SKIP_VERIFY=true

# JWT Secret
PRIVATE_KEY=test-secret-key-for-local-development-change-in-production

# Cloudflare Turnstile (Disabled for testing)
CLOUDFLARE_TURNSTILE_SECRET_KEY=
CLOUDFLARE_TURNSTILE_ENABLED=false

# CTF Config
CTF_NAME=FCTF Multiple Contest Test
CTF_DESCRIPTION=Local Testing Environment
```

### **Start backend:**

```bash
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet restore
dotnet build
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

---

## 🎨 Bước 5: Configure Frontend

### **Create .env.local:**

`ContestantPortal/.env.local`:

```env
VITE_API_BASE_URL=http://localhost:5000
VITE_CLOUDFLARE_TURNSTILE_SITE_KEY=
```

### **Start frontend:**

```bash
cd ContestantPortal
npm install
npm run dev
```

**Expected output:**
```
  VITE v5.x.x  ready in xxx ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
```

---

## 🧪 Bước 6: Test Flow

### **Test 1: Verify Infrastructure**

```bash
# Check MariaDB
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password -e "SHOW DATABASES;"

# Check Redis
docker exec -it fctf-redis redis-cli -a redis_password ping

# Check RabbitMQ
curl -u admin:rabbitmq_password http://localhost:15672/api/overview
```

### **Test 2: Login Flow**

1. Open browser: `http://localhost:5173`
2. Login:
   - Username: `student1`
   - Password: `password123`
3. **Expected:** Redirect to `/contests`
4. **Expected:** See contest list

### **Test 3: Select Contest**

1. Click on "Test Contest 1"
2. **Expected:** Call API `/auth/select-contest`
3. **Expected:** Redirect to `/contest/1/challenges`
4. **Expected:** See challenges

### **Test 4: Submit Flag**

1. Click on a challenge
2. Enter flag: `flag{sql_injection_basic}`
3. Click "Submit"
4. **Expected:** Success message

### **Test 5: Check Data in Database**

```bash
# Check submissions
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "
SELECT s.id, u.name as user, c.name as contest, cc.name as challenge, s.type, s.provided
FROM submissions s
JOIN users u ON s.user_id = u.id
JOIN contests c ON s.contest_id = c.id
JOIN contests_challenges cc ON s.contest_challenge_id = cc.id
ORDER BY s.id DESC LIMIT 5;
"
```

### **Test 6: Check Redis Keys**

```bash
# View all keys
docker exec -it fctf-redis redis-cli -a redis_password KEYS '*'

# View contest-specific keys
docker exec -it fctf-redis redis-cli -a redis_password KEYS 'contest:1:*'

# Get specific key
docker exec -it fctf-redis redis-cli -a redis_password GET 'contest:1:auth:user:3'
```

---

## 🔍 Monitoring & Debugging

### **1. Database Monitoring**

```bash
# Watch queries in real-time
docker exec -it fctf-mariadb mysql -u root -proot_password -e "
SET GLOBAL general_log = 'ON';
SET GLOBAL log_output = 'TABLE';
"

# View recent queries
docker exec -it fctf-mariadb mysql -u root -proot_password -e "
SELECT event_time, user_host, SUBSTRING(argument, 1, 100) as query
FROM mysql.general_log
ORDER BY event_time DESC
LIMIT 20;
"
```

### **2. Redis Monitoring**

```bash
# Monitor commands in real-time
docker exec -it fctf-redis redis-cli -a redis_password MONITOR

# Get info
docker exec -it fctf-redis redis-cli -a redis_password INFO

# Check memory usage
docker exec -it fctf-redis redis-cli -a redis_password INFO memory
```

### **3. RabbitMQ Monitoring**

**Management UI:** http://localhost:15672
- Username: `admin`
- Password: `rabbitmq_password`

**CLI:**
```bash
# List queues
docker exec -it fctf-rabbitmq rabbitmqctl list_queues

# List exchanges
docker exec -it fctf-rabbitmq rabbitmqctl list_exchanges

# List connections
docker exec -it fctf-rabbitmq rabbitmqctl list_connections
```

### **4. Container Logs**

```bash
# Follow all logs
docker compose -f docker-compose.dev.yml logs -f

# Follow specific service
docker compose -f docker-compose.dev.yml logs -f mariadb

# Last 100 lines
docker compose -f docker-compose.dev.yml logs --tail=100 redis
```

### **5. Container Stats**

```bash
# Real-time stats
docker stats fctf-mariadb fctf-redis fctf-rabbitmq

# One-time stats
docker stats --no-stream fctf-mariadb fctf-redis fctf-rabbitmq
```

---

## 🛠️ Useful Commands

### **Database Operations**

```bash
# Backup database
docker exec fctf-mariadb mysqldump -u fctf_user -pfctf_password fctf_multiple_contest > backup.sql

# Restore database
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest < backup.sql

# Reset database
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password -e "
DROP DATABASE IF EXISTS fctf_multiple_contest;
CREATE DATABASE fctf_multiple_contest CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
"

# Connect to database
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest
```

### **Redis Operations**

```bash
# Flush all data
docker exec -it fctf-redis redis-cli -a redis_password FLUSHALL

# Flush specific database
docker exec -it fctf-redis redis-cli -a redis_password FLUSHDB

# Get all keys
docker exec -it fctf-redis redis-cli -a redis_password KEYS '*'

# Delete keys by pattern
docker exec -it fctf-redis redis-cli -a redis_password --scan --pattern 'contest:1:*' | xargs docker exec -i fctf-redis redis-cli -a redis_password DEL
```

### **Container Management**

```bash
# Start services
docker compose -f docker-compose.dev.yml up -d

# Stop services
docker compose -f docker-compose.dev.yml stop

# Restart services
docker compose -f docker-compose.dev.yml restart

# Stop and remove containers
docker compose -f docker-compose.dev.yml down

# Stop and remove containers + volumes (DELETE ALL DATA)
docker compose -f docker-compose.dev.yml down -v

# Rebuild containers
docker compose -f docker-compose.dev.yml up -d --build
```

---

## 🐛 Troubleshooting

### **Problem 1: Container won't start**

```bash
# Check logs
docker compose -f docker-compose.dev.yml logs mariadb

# Check if port is already in use
netstat -ano | findstr :3306  # Windows
lsof -i :3306                 # Linux/Mac

# Remove and recreate
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up -d
```

### **Problem 2: Cannot connect to database**

```bash
# Check if container is running
docker ps | grep fctf-mariadb

# Check if healthy
docker inspect fctf-mariadb | grep -A 10 Health

# Test connection
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password -e "SELECT 1;"

# Check backend connection string
# Should be: Server=localhost;Port=3306;...
```

### **Problem 3: Redis authentication failed**

```bash
# Check Redis password
docker exec -it fctf-redis redis-cli -a redis_password ping

# Backend connection string should include password
# REDIS_CONNECTION_STRING=localhost:6379,password=redis_password
```

### **Problem 4: Data not persisting**

```bash
# Check volumes
docker volume ls | grep fctf

# Inspect volume
docker volume inspect mariadb_data

# If needed, backup and recreate
docker compose -f docker-compose.dev.yml down
docker volume rm mariadb_data redis_data rabbitmq_data
docker compose -f docker-compose.dev.yml up -d
```

---

## 📊 Test Scenarios với Docker

### **Scenario 1: Fresh Start**

```bash
# 1. Clean everything
docker compose -f docker-compose.dev.yml down -v

# 2. Start fresh
docker compose -f docker-compose.dev.yml up -d

# 3. Wait for healthy
sleep 10

# 4. Setup database
./setup-database.sh

# 5. Import test data
./import-test-data.sh

# 6. Start backend & frontend
# (in separate terminals)
```

### **Scenario 2: Reset Data Only**

```bash
# 1. Keep containers running
# 2. Reset database
docker exec -i fctf-mariadb mysql -u root -proot_password -e "
DROP DATABASE IF EXISTS fctf_multiple_contest;
CREATE DATABASE fctf_multiple_contest CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
"

# 3. Re-import
./import-test-data.sh

# 4. Clear Redis
docker exec -it fctf-redis redis-cli -a redis_password FLUSHALL

# 5. Restart backend
```

### **Scenario 3: Test Data Isolation**

```bash
# 1. Login as student1, select Contest 1
# 2. Submit flags
# 3. Check Redis keys
docker exec -it fctf-redis redis-cli -a redis_password KEYS 'contest:1:*'

# 4. Login as student2, select Contest 2
# 5. Submit flags
# 6. Check Redis keys
docker exec -it fctf-redis redis-cli -a redis_password KEYS 'contest:2:*'

# 7. Verify isolation
docker exec -it fctf-redis redis-cli -a redis_password KEYS 'contest:*'
```

---

## 🎯 Complete Test Script

Tạo file `test-with-docker.sh`:

```bash
#!/bin/bash

set -e

echo "=== FCTF Multiple Contest - Docker Test ==="

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Step 1: Start infrastructure
echo -e "${YELLOW}Step 1: Starting infrastructure...${NC}"
docker compose -f docker-compose.dev.yml up -d

# Step 2: Wait for services
echo -e "${YELLOW}Step 2: Waiting for services to be healthy...${NC}"
sleep 10

# Step 3: Check health
echo -e "${YELLOW}Step 3: Checking service health...${NC}"
docker compose -f docker-compose.dev.yml ps

# Step 4: Setup database
echo -e "${YELLOW}Step 4: Setting up database...${NC}"
./setup-database.sh

# Step 5: Import test data
echo -e "${YELLOW}Step 5: Importing test data...${NC}"
./import-test-data.sh

# Step 6: Verify
echo -e "${YELLOW}Step 6: Verifying setup...${NC}"

# Check MariaDB
if docker exec fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "SELECT COUNT(*) FROM users;" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ MariaDB OK${NC}"
else
    echo -e "${RED}✗ MariaDB Failed${NC}"
    exit 1
fi

# Check Redis
if docker exec fctf-redis redis-cli -a redis_password ping > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Redis OK${NC}"
else
    echo -e "${RED}✗ Redis Failed${NC}"
    exit 1
fi

# Check RabbitMQ
if docker exec fctf-rabbitmq rabbitmqctl status > /dev/null 2>&1; then
    echo -e "${GREEN}✓ RabbitMQ OK${NC}"
else
    echo -e "${RED}✗ RabbitMQ Failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}=== Setup Complete! ===${NC}"
echo ""
echo "Next steps:"
echo "1. Start backend:"
echo "   cd ControlCenterAndChallengeHostingServer/ContestantBE"
echo "   dotnet run"
echo ""
echo "2. Start frontend (in new terminal):"
echo "   cd ContestantPortal"
echo "   npm run dev"
echo ""
echo "3. Open browser:"
echo "   http://localhost:5173"
echo ""
echo "4. Login:"
echo "   Username: student1"
echo "   Password: password123"
echo ""
echo "Services:"
echo "  - MariaDB: localhost:3306"
echo "  - Redis: localhost:6379"
echo "  - RabbitMQ: localhost:5672"
echo "  - RabbitMQ UI: http://localhost:15672 (admin/rabbitmq_password)"
```

**Run:**
```bash
chmod +x test-with-docker.sh
./test-with-docker.sh
```

---

## 📝 Summary

### **Advantages of Docker Setup:**
- ✅ Easy to start/stop all services
- ✅ Isolated environment
- ✅ Consistent across machines
- ✅ Easy to reset data
- ✅ No need to install MySQL/Redis locally

### **Services:**
- 🐳 MariaDB (Port 3306)
- 🐳 Redis (Port 6379)
- 🐳 RabbitMQ (Port 5672, 15672)
- 💻 Backend (Native - Port 5000)
- 💻 Frontend (Native - Port 5173)

### **Quick Commands:**
```bash
# Start
docker compose -f docker-compose.dev.yml up -d

# Stop
docker compose -f docker-compose.dev.yml stop

# Reset
docker compose -f docker-compose.dev.yml down -v

# Logs
docker compose -f docker-compose.dev.yml logs -f
```

---

**Happy Testing with Docker! 🐳🚀**
