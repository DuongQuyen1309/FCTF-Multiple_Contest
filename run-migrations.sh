#!/bin/bash

echo "=== FCTF Database Migrations ==="

# Check if migrations directory exists
if [ ! -d "FCTF-ManagementPlatform/migrations" ]; then
    echo "Error: migrations directory not found at FCTF-ManagementPlatform/migrations"
    exit 1
fi

# Check if Python virtual environment exists
VENV_PATH=""
if [ -d "FCTF-ManagementPlatform/.venv" ]; then
    VENV_PATH="FCTF-ManagementPlatform/.venv"
elif [ -d "FCTF-ManagementPlatform/venv" ]; then
    VENV_PATH="FCTF-ManagementPlatform/venv"
else
    echo "Error: No virtual environment found."
    echo "Please create a virtual environment first:"
    echo "  cd FCTF-ManagementPlatform"
    echo "  python -m venv .venv"
    echo "  source .venv/bin/activate  # Linux/Mac"
    echo "  # or .venv\\Scripts\\activate  # Windows"
    echo "  pip install -r requirements.txt"
    exit 1
fi

echo "Using virtual environment: $VENV_PATH"

# Activate virtual environment
if [ -f "$VENV_PATH/bin/activate" ]; then
    source "$VENV_PATH/bin/activate"
elif [ -f "$VENV_PATH/Scripts/activate" ]; then
    source "$VENV_PATH/Scripts/activate"
else
    echo "Error: Could not find activation script in virtual environment"
    exit 1
fi

# Navigate to FCTF-ManagementPlatform
cd FCTF-ManagementPlatform

# Check if alembic is installed
if ! command -v alembic &> /dev/null; then
    echo "Error: alembic command not found."
    echo "Please install requirements:"
    echo "  pip install -r requirements.txt"
    exit 1
fi

# Show current revision
echo ""
echo "=== Current Database Revision ==="
alembic current

# Show pending migrations
echo ""
echo "=== Pending Migrations ==="
alembic history

# Ask for confirmation
echo ""
read -p "Do you want to run migrations? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Migration cancelled."
    exit 0
fi

# Run migrations
echo ""
echo "=== Running Migrations ==="
alembic upgrade head

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Migrations completed successfully!"
    
    # Show new revision
    echo ""
    echo "=== New Database Revision ==="
    alembic current
else
    echo ""
    echo "✗ Migrations failed. Please check the error above."
    exit 1
fi

# Deactivate virtual environment
deactivate

cd ..

echo ""
echo "=== Migration Complete ==="
echo "You can now import test data:"
echo "  ./import-test-data.sh"
