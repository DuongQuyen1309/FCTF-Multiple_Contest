#!/bin/bash

set -e

echo "=== FCTF Multiple Contest - Docker Test ==="

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

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

# Step 5: Check if test data should be imported
echo -e "${YELLOW}Step 5: Test data import...${NC}"
if [ -f "test-data.sql" ]; then
    if grep -q "REPLACE_WITH_HASH" test-data.sql; then
        echo -e "${YELLOW}Please generate password hash and update test-data.sql${NC}"
        echo "Run: cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash && dotnet run password123"
        echo "Then run: ./import-test-data.sh"
    else
        ./import-test-data.sh
    fi
else
    echo -e "${YELLOW}test-data.sql not found. Skipping test data import.${NC}"
fi

# Step 6: Verify
echo -e "${YELLOW}Step 6: Verifying setup...${NC}"

# Check MariaDB
if docker exec fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT 1;" > /dev/null 2>&1; then
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
echo ""
echo "Note: Database schema was created using Alembic migrations"
echo "If you need to run migrations manually:"
echo "  ./run-migrations.sh"
