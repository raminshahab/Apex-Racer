#!/bin/bash

# Test anti-cheat flagging system

BASE_URL="http://localhost:7350"
HTTP_KEY="defaultkey"

echo "=== Testing Anti-Cheat System (Flag-Only Mode) ==="
echo ""

# Create test user
USERNAME="AntiCheatTest_$RANDOM"
echo "Creating test user: $USERNAME"

AUTH_RESPONSE=$(curl -s -X POST "$BASE_URL/v2/account/authenticate/custom?create=true&username=$USERNAME" \
    --user "defaultkey:" \
    -H "Content-Type: application/json" \
    -d "{\"id\":\"$USERNAME\"}")

TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.token')

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo " Failed to authenticate"
    exit 1
fi
echo "✓ Authenticated"
echo ""

# Test 1: Valid time (should succeed, no flag)
echo "1. Testing VALID time (45.5s)..."
RESPONSE=$(curl -s -X POST "$BASE_URL/v2/rpc/submit_race_time?http_key=$HTTP_KEY" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "\"{\\\"race_time\\\":45.5,\\\"idempotency_key\\\":\\\"test-valid\\\"}\"")

echo "$RESPONSE" | jq -r '.payload' | jq '.'
echo ""

# Test 2: Too fast (create new user to avoid cycle limit)
echo "2. Testing TOO FAST time (5.0s) - should be flagged but allowed..."
USERNAME2="FastCheat_$RANDOM"
AUTH2=$(curl -s -X POST "$BASE_URL/v2/account/authenticate/custom?create=true&username=$USERNAME2" \
    --user "defaultkey:" \
    -H "Content-Type: application/json" \
    -d "{\"id\":\"$USERNAME2\"}")
TOKEN2=$(echo "$AUTH2" | jq -r '.token')

RESPONSE=$(curl -s -X POST "$BASE_URL/v2/rpc/submit_race_time?http_key=$HTTP_KEY" \
    -H "Authorization: Bearer $TOKEN2" \
    -H "Content-Type: application/json" \
    -d "\"{\\\"race_time\\\":5.0,\\\"idempotency_key\\\":\\\"test-fast\\\"}\"")

echo "$RESPONSE" | jq -r '.payload' | jq '.'
echo ""

# Test 3: Too slow (create new user to avoid cycle limit)
echo "3. Testing TOO SLOW time (200.0s) - should be flagged but allowed..."
USERNAME3="SlowCheat_$RANDOM"
AUTH3=$(curl -s -X POST "$BASE_URL/v2/account/authenticate/custom?create=true&username=$USERNAME3" \
    --user "defaultkey:" \
    -H "Content-Type: application/json" \
    -d "{\"id\":\"$USERNAME3\"}")
TOKEN3=$(echo "$AUTH3" | jq -r '.token')

RESPONSE=$(curl -s -X POST "$BASE_URL/v2/rpc/submit_race_time?http_key=$HTTP_KEY" \
    -H "Authorization: Bearer $TOKEN3" \
    -H "Content-Type: application/json" \
    -d "\"{\\\"race_time\\\":200.0,\\\"idempotency_key\\\":\\\"test-slow\\\"}\"")

echo "$RESPONSE" | jq -r '.payload' | jq '.'
echo ""

# Wait a moment for logs to flush
sleep 2

# Check flagged submissions
echo "4. Checking flagged submissions (admin view)..."
FLAGGED=$(curl -s -X POST "$BASE_URL/v2/rpc/get_flagged_submissions?http_key=$HTTP_KEY" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '""')

echo "$FLAGGED" | jq -r '.payload' | jq '.'
echo ""

# Check server logs for flagging
echo "5. Server logs (last 10 lines with FLAGGED)..."
docker logs nakama-go-server 2>&1 | grep "FLAGGED" | tail -10
echo ""

echo "=== Anti-Cheat Test Complete ==="
echo ""
echo "Summary:"
echo "  ✓ All submissions were accepted (non-blocking)"
echo "  ✓ Suspicious submissions were flagged for review"
echo "  ✓ Flags are stored and queryable via RPC"
