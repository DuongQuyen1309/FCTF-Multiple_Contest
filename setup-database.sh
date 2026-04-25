#!/bin/bash

echo "=== FCTF Database Setup ==="

# Database name (using existing database from docker-compose)
DB_NAME="ctfd"

# Wait for MariaDB to be ready
echo "Waiting for MariaDB to be ready..."
until docker exec fctf-mariadb mysqladmin ping -h localhost -u root -proot_password --silent 2>/dev/null; do
    echo "MariaDB is unavailable - sleeping"
    sleep 2
done

echo "MariaDB is ready!"

# Database already created by docker-compose, just verify
echo "Verifying database..."
docker exec -i fctf-mariadb mysql -u root -proot_password <<EOF
SHOW DATABASES LIKE '$DB_NAME';
EOF

echo "Database verified: $DB_NAME"

# Run Alembic migrations
echo ""
echo "=== Running Database Migrations ==="
if [ -d "FCTF-ManagementPlatform/migrations" ]; then
    echo "Found migrations directory. Running Alembic migrations..."
    
    # Check if Python virtual environment exists
    if [ -d "FCTF-ManagementPlatform/.venv" ]; then
        echo "Using existing virtual environment..."
        source FCTF-ManagementPlatform/.venv/bin/activate 2>/dev/null || source FCTF-ManagementPlatform/.venv/Scripts/activate 2>/dev/null
    elif [ -d "FCTF-ManagementPlatform/venv" ]; then
        echo "Using existing virtual environment..."
        source FCTF-ManagementPlatform/venv/bin/activate 2>/dev/null || source FCTF-ManagementPlatform/venv/Scripts/activate 2>/dev/null
    else
        echo "Warning: No virtual environment found. Using system Python..."
    fi
    
    # Run migrations
    cd FCTF-ManagementPlatform
    
    # Check if alembic is available
    if command -v alembic &> /dev/null; then
        echo "Running: alembic upgrade head"
        alembic upgrade head
        
        if [ $? -eq 0 ]; then
            echo "✓ Migrations completed successfully!"
        else
            echo "✗ Migrations failed. Please check the error above."
        fi
    else
        echo "Warning: alembic command not found."
        echo "You can run migrations manually:"
        echo "  cd FCTF-ManagementPlatform"
        echo "  source .venv/bin/activate  # or venv/bin/activate"
        echo "  alembic upgrade head"
    fi
    
    cd ..
else
    echo "Warning: migrations directory not found at FCTF-ManagementPlatform/migrations"
    echo "Skipping migrations..."
fi

echo ""
echo "=== Next Steps ==="
echo "1. Generate password hash:"
echo "   cd ControlCenterAndChallengeHostingServer/GeneratePasswordHash"
echo "   dotnet run password123"
echo ""
echo "2. Update test-data.sql with the generated hash"
echo ""
echo "3. Import test data:"
echo "   ./import-test-data.sh"
