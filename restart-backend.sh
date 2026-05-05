#!/bin/bash
# Script to restart the backend with clean build

echo "=== Stopping any running backend processes ==="
# Note: You need to manually stop the backend with Ctrl+C in the terminal

echo ""
echo "=== Cleaning the solution ==="
cd FCTF-Multiple-Contest/ControlCenterAndChallengeHostingServer
dotnet clean

echo ""
echo "=== Building the solution ==="
dotnet build

echo ""
echo "=== Starting ContestantBE ==="
cd ContestantBE
dotnet run

# After running this script:
# 1. Stop the current backend process (Ctrl+C)
# 2. Run: bash restart-backend.sh
# 3. Test: http://localhost:5173/contest/3/challenges
