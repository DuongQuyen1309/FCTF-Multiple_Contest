# 🔄 Database Migration Guide - FCTF Multiple Contest

## 📋 Overview

This guide explains how to work with database migrations using **Alembic** (Python) for the FCTF Multiple Contest system.

---

## 🏗️ Migration Architecture

### **Your Setup:**
```
FCTF-ManagementPlatform/
├── migrations/
│   ├── versions/          # Migration files
│   ├── env.py            # Alembic environment
│   ├── alembic.ini       # Alembic configuration
│   └── script.py.mako    # Migration template
├── .venv/ or venv/       # Python virtual environment
└── requirements.txt      # Python dependencies
```

### **Migration Tool:**
- **Alembic** - Database migration tool for SQLAlchemy
- **Flask-Migrate** - Flask extension for Alembic

---

## 🚀 Quick Start with Migrations

### **Option 1: Automated Setup (Recommended)**

```bash
# 1. Make scripts executable
chmod +x *.sh

# 2. Run automated setup (includes migrations)
./test-with-docker.sh
```

This will:
- Start Docker services
- Create database
- Run Alembic migrations automatically
- Prompt for test data import

### **Option 2: Manual Setup**

```bash
# 1. Start Docker services
docker compose -f docker-compose.dev.yml up -d

# 2. Create database
./setup-database.sh

# 3. Run migrations manually
./run-migrations.sh

# 4. Import test data
./import-test-data.sh
```

---

## 📜 Available Scripts

### **1. setup-database.sh** (Updated)
**What it does:**
- Creates database
- Runs Alembic migrations automatically
- Shows next steps

**Usage:**
```bash
./setup-database.sh
```

### **2. run-migrations.sh** (New)
**What it does:**
- Activates Python virtual environment
- Shows current database revision
- Shows pending migrations
- Asks for confirmation
- Runs `alembic upgrade head`
- Shows new revision

**Usage:**
```bash
./run-migrations.sh
```

### **3. import-test-data.sh** (Unchanged)
**What it does:**
- Imports test data after migrations

**Usage:**
```bash
./import-test-data.sh
```

---

## 🔧 Manual Migration Commands

### **Setup Python Environment:**

```bash
# Navigate to FCTF-ManagementPlatform
cd FCTF-ManagementPlatform

# Create virtual environment (if not exists)
python -m venv .venv

# Activate virtual environment
source .venv/bin/activate  # Linux/Mac
# or
.venv\Scripts\activate  # Windows

# Install dependencies
pip install -r requirements.txt
```

### **Check Migration Status:**

```bash
# Show current revision
alembic current

# Show migration history
alembic history

# Show pending migrations
alembic history --verbose
```

### **Run Migrations:**

```bash
# Upgrade to latest
alembic upgrade head

# Upgrade one step
alembic upgrade +1

# Downgrade one step
alembic downgrade -1

# Downgrade to specific revision
alembic downgrade <revision_id>
```

### **Create New Migration:**

```bash
# Auto-generate migration from model changes
alembic revision --autogenerate -m "description"

# Create empty migration
alembic revision -m "description"
```

---

## 📊 Migration Workflow

### **For Testing (Local Development):**

```
1. Start Docker services
   ↓
2. Create database
   ↓
3. Run migrations (alembic upgrade head)
   ↓
4. Import test data
   ↓
5. Start backend & frontend
   ↓
6. Test!
```

### **For Production:**

```
1. Backup database
   ↓
2. Test migrations on staging
   ↓
3. Run migrations on production
   ↓
4. Verify data integrity
   ↓
5. Deploy new code
```

---

## 🔍 Understanding Migrations

### **What Migrations Do:**
- Create tables
- Add/remove columns
- Create indexes
- Add constraints
- Modify data types
- Populate initial data

### **What Migrations DON'T Do:**
- Import test data (use test-data.sql)
- Create database (use setup-database.sh)
- Manage Docker services

---

## 🐛 Troubleshooting

### **Problem 1: alembic command not found**

```bash
# Solution: Install requirements
cd FCTF-ManagementPlatform
source .venv/bin/activate
pip install -r requirements.txt
```

### **Problem 2: Virtual environment not found**

```bash
# Solution: Create virtual environment
cd FCTF-ManagementPlatform
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

### **Problem 3: Migration fails**

```bash
# Check current revision
alembic current

# Check migration history
alembic history

# Try to stamp current revision
alembic stamp head

# Or downgrade and retry
alembic downgrade -1
alembic upgrade head
```

### **Problem 4: Database already exists**

```bash
# Option 1: Drop and recreate
docker exec -it fctf-mariadb mysql -u root -proot_password -e "
DROP DATABASE IF EXISTS fctf_multiple_contest;
CREATE DATABASE fctf_multiple_contest CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
"

# Then run migrations
./run-migrations.sh

# Option 2: Stamp existing database
cd FCTF-ManagementPlatform
source .venv/bin/activate
alembic stamp head
```

### **Problem 5: Migration conflicts**

```bash
# Check for multiple heads
alembic heads

# Merge heads if needed
alembic merge heads -m "merge"

# Then upgrade
alembic upgrade head
```

---

## 📝 Migration Best Practices

### **1. Always Backup Before Migrations:**
```bash
# Backup database
docker exec fctf-mariadb mysqldump -u fctf_user -pfctf_password fctf_multiple_contest > backup.sql

# Restore if needed
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest < backup.sql
```

### **2. Test Migrations on Staging First:**
```bash
# Test on staging
alembic upgrade head

# Verify data
# Check application

# Then apply to production
```

### **3. Review Auto-Generated Migrations:**
```bash
# After auto-generate
alembic revision --autogenerate -m "description"

# Review the generated file
cat migrations/versions/<revision_id>_description.py

# Edit if needed
# Then apply
alembic upgrade head
```

### **4. Keep Migrations Small:**
- One migration per logical change
- Don't mix schema and data changes
- Use separate migrations for complex changes

---

## 🔄 Complete Workflow Example

### **Fresh Start:**

```bash
# 1. Start Docker
docker compose -f docker-compose.dev.yml up -d

# 2. Setup database (includes migrations)
./setup-database.sh

# 3. Generate password hash
cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash
dotnet run password123
# Copy hash
cd ../..

# 4. Update test-data.sql
# Replace REPLACE_WITH_HASH with hash

# 5. Import test data
./import-test-data.sh

# 6. Start backend
cd ControlCenterAndChallengeHostingServer/ContestantBE
dotnet run

# 7. Start frontend (new terminal)
cd ContestantPortal
npm run dev

# 8. Test
# http://localhost:5173
```

### **Reset Everything:**

```bash
# Stop and remove containers + volumes
docker compose -f docker-compose.dev.yml down -v

# Start fresh
./test-with-docker.sh
```

---

## 📊 Migration vs Test Data

### **Migrations (Alembic):**
- **Purpose:** Create database schema
- **When:** Before importing data
- **What:** Tables, columns, indexes, constraints
- **Tool:** Alembic (Python)
- **Files:** `FCTF-ManagementPlatform/migrations/versions/*.py`

### **Test Data (SQL):**
- **Purpose:** Populate database with test data
- **When:** After migrations
- **What:** Users, contests, challenges, flags
- **Tool:** MySQL import
- **Files:** `test-data.sql`

### **Workflow:**
```
1. Migrations (schema) → 2. Test Data (content)
```

---

## 🎯 Quick Reference

| Task | Command |
|------|---------|
| Run all migrations | `./run-migrations.sh` |
| Check current revision | `alembic current` |
| Show migration history | `alembic history` |
| Upgrade to latest | `alembic upgrade head` |
| Downgrade one step | `alembic downgrade -1` |
| Create new migration | `alembic revision --autogenerate -m "desc"` |
| Stamp current revision | `alembic stamp head` |

---

## 📞 Need Help?

- **Alembic Documentation:** https://alembic.sqlalchemy.org/
- **Flask-Migrate Documentation:** https://flask-migrate.readthedocs.io/
- **Check migration files:** `FCTF-ManagementPlatform/migrations/versions/`
- **Check Alembic config:** `FCTF-ManagementPlatform/migrations/alembic.ini`

---

## ✅ Verification

### **After Migrations:**

```bash
# Check tables exist
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "SHOW TABLES;"

# Check specific table
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password fctf_multiple_contest -e "DESCRIBE users;"

# Check migration version
cd FCTF-ManagementPlatform
source .venv/bin/activate
alembic current
```

---

**Happy Migrating! 🚀**

*Last Updated: 2024*
*Version: 1.0*

