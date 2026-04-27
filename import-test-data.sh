#!/bin/bash

echo "=== Importing test data ==="

# Database name
DB_NAME="ctfd"

if [ ! -f "test-data.sql" ]; then
    echo "Error: test-data.sql not found"
    echo "Please make sure test-data.sql exists in the current directory"
    exit 1
fi

# Check if password hash has been updated
if grep -q "REPLACE_WITH_HASH" test-data.sql; then
    echo "Warning: test-data.sql still contains REPLACE_WITH_HASH"
    echo "Please generate password hash first:"
    echo "  cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash"
    echo "  dotnet run password123"
    echo "Then replace REPLACE_WITH_HASH in test-data.sql with the generated hash"
    exit 1
fi

echo "Importing test data into database $DB_NAME..."
docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password $DB_NAME < test-data.sql

if [ $? -eq 0 ]; then
    echo "Test data imported successfully!"
    echo ""
    echo "=== Verification ==="
    docker exec -i fctf-mariadb mysql -u fctf_user -pfctf_password $DB_NAME -e "
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
else
    echo "Error: Failed to import test data"
    exit 1
fi
