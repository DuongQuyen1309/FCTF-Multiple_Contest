#!/bin/bash

# ============================================
# FCTF Multiple Contest - API Test Script
# ============================================

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# API Base URL
API_URL="http://localhost:5000"

# Test results
PASSED=0
FAILED=0

# Function to print test header
print_test() {
    echo ""
    echo -e "${BLUE}=== $1 ===${NC}"
}

# Function to print success
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
    ((PASSED++))
}

# Function to print failure
print_failure() {
    echo -e "${RED}✗ $1${NC}"
    ((FAILED++))
}

# Function to print info
print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# ============================================
# Test 1: Health Check
# ============================================
print_test "Test 1: Health Check"

RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/healthcheck")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    print_success "Health check passed"
    print_info "Response: $BODY"
else
    print_failure "Health check failed (HTTP $HTTP_CODE)"
fi

# ============================================
# Test 2: Login (Student)
# ============================================
print_test "Test 2: Login as Student"

LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/Auth/login" \
    -H "Content-Type: application/json" \
    -d '{"username":"student1","password":"password123"}')

HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -n1)
BODY=$(echo "$LOGIN_RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    TOKEN=$(echo "$BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    if [ -n "$TOKEN" ]; then
        print_success "Login successful"
        print_info "Token: ${TOKEN:0:50}..."
        
        # Check if contestId is 0
        if echo "$BODY" | grep -q '"contestId":0'; then
            print_success "Temporary token has contestId=0"
        else
            print_failure "Token should have contestId=0"
        fi
    else
        print_failure "No token in response"
    fi
else
    print_failure "Login failed (HTTP $HTTP_CODE)"
    print_info "Response: $BODY"
fi

# ============================================
# Test 3: Get Contests
# ============================================
print_test "Test 3: Get Contests"

if [ -n "$TOKEN" ]; then
    CONTESTS_RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/Contest/list" \
        -H "Authorization: Bearer $TOKEN")
    
    HTTP_CODE=$(echo "$CONTESTS_RESPONSE" | tail -n1)
    BODY=$(echo "$CONTESTS_RESPONSE" | head -n-1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Get contests successful"
        
        # Count contests
        CONTEST_COUNT=$(echo "$BODY" | grep -o '"id":' | wc -l)
        print_info "Found $CONTEST_COUNT contests"
        
        # Extract first contest ID
        CONTEST_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)
        print_info "First contest ID: $CONTEST_ID"
    else
        print_failure "Get contests failed (HTTP $HTTP_CODE)"
        print_info "Response: $BODY"
    fi
else
    print_failure "Skipped - no token available"
fi

# ============================================
# Test 4: Select Contest
# ============================================
print_test "Test 4: Select Contest"

if [ -n "$TOKEN" ] && [ -n "$CONTEST_ID" ]; then
    SELECT_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/Auth/select-contest" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $TOKEN" \
        -d "{\"contestId\":$CONTEST_ID}")
    
    HTTP_CODE=$(echo "$SELECT_RESPONSE" | tail -n1)
    BODY=$(echo "$SELECT_RESPONSE" | head -n-1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        NEW_TOKEN=$(echo "$BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
        if [ -n "$NEW_TOKEN" ]; then
            print_success "Select contest successful"
            print_info "New token: ${NEW_TOKEN:0:50}..."
            TOKEN="$NEW_TOKEN"
            
            # Check if contestId is set
            if echo "$BODY" | grep -q "\"contestId\":$CONTEST_ID"; then
                print_success "New token has contestId=$CONTEST_ID"
            else
                print_failure "Token should have contestId=$CONTEST_ID"
            fi
        else
            print_failure "No token in response"
        fi
    else
        print_failure "Select contest failed (HTTP $HTTP_CODE)"
        print_info "Response: $BODY"
    fi
else
    print_failure "Skipped - no token or contest ID available"
fi

# ============================================
# Test 5: Get Challenges
# ============================================
print_test "Test 5: Get Challenges"

if [ -n "$TOKEN" ]; then
    CHALLENGES_RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/Challenge" \
        -H "Authorization: Bearer $TOKEN")
    
    HTTP_CODE=$(echo "$CHALLENGES_RESPONSE" | tail -n1)
    BODY=$(echo "$CHALLENGES_RESPONSE" | head -n-1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Get challenges successful"
        
        # Count challenges
        CHALLENGE_COUNT=$(echo "$BODY" | grep -o '"id":' | wc -l)
        print_info "Found $CHALLENGE_COUNT challenges"
    else
        print_failure "Get challenges failed (HTTP $HTTP_CODE)"
        print_info "Response: $BODY"
    fi
else
    print_failure "Skipped - no token available"
fi

# ============================================
# Test 6: Access Control (Try to access with contestId=0)
# ============================================
print_test "Test 6: Access Control"

# Login again to get temporary token
LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/Auth/login" \
    -H "Content-Type: application/json" \
    -d '{"username":"student1","password":"password123"}')

HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -n1)
BODY=$(echo "$LOGIN_RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    TEMP_TOKEN=$(echo "$BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    
    # Try to access challenges with temporary token
    CHALLENGES_RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/Challenge" \
        -H "Authorization: Bearer $TEMP_TOKEN")
    
    HTTP_CODE=$(echo "$CHALLENGES_RESPONSE" | tail -n1)
    
    if [ "$HTTP_CODE" = "403" ] || [ "$HTTP_CODE" = "401" ]; then
        print_success "Access control working - temporary token blocked"
    else
        print_failure "Access control failed - temporary token should be blocked (HTTP $HTTP_CODE)"
    fi
else
    print_failure "Could not get temporary token for test"
fi

# ============================================
# Test 7: Admin Login
# ============================================
print_test "Test 7: Admin Login"

ADMIN_LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/Auth/login" \
    -H "Content-Type: application/json" \
    -d '{"username":"admin","password":"password123"}')

HTTP_CODE=$(echo "$ADMIN_LOGIN_RESPONSE" | tail -n1)
BODY=$(echo "$ADMIN_LOGIN_RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    ADMIN_TOKEN=$(echo "$BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    if [ -n "$ADMIN_TOKEN" ]; then
        print_success "Admin login successful"
        
        # Check user type
        if echo "$BODY" | grep -q '"type":"admin"'; then
            print_success "User type is admin"
        else
            print_failure "User type should be admin"
        fi
    else
        print_failure "No token in response"
    fi
else
    print_failure "Admin login failed (HTTP $HTTP_CODE)"
fi

# ============================================
# Test 8: Get Challenge Bank (Admin)
# ============================================
print_test "Test 8: Get Challenge Bank"

if [ -n "$ADMIN_TOKEN" ]; then
    BANK_RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/Contest/bank/challenges" \
        -H "Authorization: Bearer $ADMIN_TOKEN")
    
    HTTP_CODE=$(echo "$BANK_RESPONSE" | tail -n1)
    BODY=$(echo "$BANK_RESPONSE" | head -n-1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Get challenge bank successful"
        
        # Count challenges
        BANK_COUNT=$(echo "$BODY" | grep -o '"id":' | wc -l)
        print_info "Found $BANK_COUNT challenges in bank"
    else
        print_failure "Get challenge bank failed (HTTP $HTTP_CODE)"
        print_info "Response: $BODY"
    fi
else
    print_failure "Skipped - no admin token available"
fi

# ============================================
# Test Summary
# ============================================
echo ""
echo -e "${BLUE}=== Test Summary ===${NC}"
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
TOTAL=$((PASSED + FAILED))
echo "Total: $TOTAL"

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All tests passed! ✓${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed! ✗${NC}"
    exit 1
fi
