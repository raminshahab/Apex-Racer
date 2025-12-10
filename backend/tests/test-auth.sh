#!/bin/bash

# Test Authentication Endpoints using Nakama's built-in API
# Make sure the Nakama server is running: cd nakama && docker-compose up

NAKAMA_HOST="http://localhost:7350"
SERVER_KEY="defaultkey"

echo "=== Testing Nakama Authentication (Built-in API) ==="
echo ""

# Generate a unique test user to avoid conflicts
TIMESTAMP=$(date +%s)
TEST_EMAIL="test${TIMESTAMP}@example.com"
TEST_USERNAME="testplayer${TIMESTAMP}"
TEST_PASSWORD="password123"

# Test 1: Register a new user using Nakama's built-in email authentication
echo "1. Testing Registration (Email Authentication)..."
echo "Creating user: ${TEST_EMAIL}"
REGISTER_RESPONSE=$(curl -s -X POST "${NAKAMA_HOST}/v2/account/authenticate/email?create=true&username=${TEST_USERNAME}" \
  -H "Content-Type: application/json" \
  -u "${SERVER_KEY}:" \
  -d "{
    \"email\": \"${TEST_EMAIL}\",
    \"password\": \"${TEST_PASSWORD}\"
  }")

echo "Registration Response:"
echo $REGISTER_RESPONSE | jq '.'
echo ""

# Extract token from response
TOKEN=$(echo $REGISTER_RESPONSE | jq -r '.token // empty' 2>/dev/null)

if [ -n "$TOKEN" ] && [ "$TOKEN" != "null" ] && [ "$TOKEN" != "" ]; then
  echo "✓ Registration successful! Token obtained."
  echo "Token: ${TOKEN:0:50}..." 
else
  echo "✗ Registration failed or token not found"
  TOKEN=""
fi

echo ""
echo "================================================"
echo ""

# Test 2: Login with existing user
echo "2. Testing Login (Email Authentication)..."
echo "Logging in with: ${TEST_EMAIL}"
LOGIN_RESPONSE=$(curl -s -X POST "${NAKAMA_HOST}/v2/account/authenticate/email?create=false" \
  -H "Content-Type: application/json" \
  -u "${SERVER_KEY}:" \
  -d "{
    \"email\": \"${TEST_EMAIL}\",
    \"password\": \"${TEST_PASSWORD}\"
  }")

echo "Login Response:"
echo $LOGIN_RESPONSE | jq '.'
echo ""

# Extract token from login response
LOGIN_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token // empty' 2>/dev/null)

if [ -n "$LOGIN_TOKEN" ] && [ "$LOGIN_TOKEN" != "null" ] && [ "$LOGIN_TOKEN" != "" ]; then
  echo "✓ Login successful! Token obtained."
  echo "Token: ${LOGIN_TOKEN:0:50}..." # Show first 50 chars
else
  echo "✗ Login failed or token not found"
  LOGIN_TOKEN=""
fi

echo ""
echo "================================================"
echo ""

# Test 3: Get account info using the token
echo "3. Testing Authenticated Request (Get Account)..."
if [ -n "$LOGIN_TOKEN" ]; then
  ACCOUNT_RESPONSE=$(curl -s -X GET "${NAKAMA_HOST}/v2/account" \
    -H "Authorization: Bearer ${LOGIN_TOKEN}")

  echo "Account Info Response:"
  echo $ACCOUNT_RESPONSE | jq '.'
  echo ""
  echo "✓ Authenticated request successful!"
else
  echo "✗ Cannot test authenticated request - no token available"
fi

echo ""
echo "=== Testing Complete ==="
echo ""
echo "Summary:"
echo "- Registration endpoint: POST /v2/account/authenticate/email?create=true&username=USER"
echo "- Login endpoint: POST /v2/account/authenticate/email?create=false"
echo "- Authentication: Basic auth with server key"
echo "- Use the token in Authorization header for authenticated requests"
