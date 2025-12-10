#!/bin/bash

# Simple test script for Nakama leaderboard

BASE_URL="http://localhost:7350"
HTTP_KEY="defaultkey"

echo "=== Testing Nakama Leaderboard (Lua) ==="
echo ""

# Create 3 test users and submit times
for i in 1 2 3; do
    USERNAME="Racer${i}_$RANDOM"
    echo "Creating user: $USERNAME"

    # Authenticate
    AUTH_RESPONSE=$(curl -s -X POST "$BASE_URL/v2/account/authenticate/custom?create=true&username=$USERNAME" \
        --user "defaultkey:" \
        -H "Content-Type: application/json" \
        -d "{\"id\":\"$USERNAME\"}")

    TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.token')

    if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
        echo "  Failed to authenticate"
        echo "  Response: $AUTH_RESPONSE"
        continue
    fi

    echo "  ✓ Authenticated"

    # Submit race time with idempotency key
    RACE_TIME="4${i}.${RANDOM:0:3}"
    IDEMPOTENCY_KEY="race-$(uuidgen)"
    echo "  Submitting time: ${RACE_TIME}s"
    echo "  Idempotency key: ${IDEMPOTENCY_KEY}"

    SUBMIT_RESPONSE=$(curl -s -X POST "$BASE_URL/v2/rpc/submit_race_time?http_key=$HTTP_KEY" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "\"{\\\"race_time\\\":$RACE_TIME,\\\"idempotency_key\\\":\\\"$IDEMPOTENCY_KEY\\\"}\"")

    PAYLOAD=$(echo "$SUBMIT_RESPONSE" | jq -r '.payload')

    if [ "$PAYLOAD" != "null" ] && [ -n "$PAYLOAD" ]; then
        echo "  ✓ Race time submitted"
        echo "  $PAYLOAD" | jq -c '{race_time, cycle, idempotency_key}'
    else
        echo "  Failed to submit"
        echo "  Response: $SUBMIT_RESPONSE"
    fi

    echo ""
done

echo "=== Getting Leaderboard ==="
echo ""

# Get leaderboard using the last token
LEADERBOARD_RESPONSE=$(curl -s -X POST "$BASE_URL/v2/rpc/get_leaderboard?http_key=$HTTP_KEY" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '""')

PAYLOAD=$(echo "$LEADERBOARD_RESPONSE" | jq -r '.payload')

if [ "$PAYLOAD" != "null" ] && [ -n "$PAYLOAD" ]; then
    echo "$PAYLOAD" | jq '.leaderboard[] | "\(.rank). \(.username) - \(.race_time)s (cycle: \(.cycle))"'
else
    echo "Failed to get leaderboard"
    echo "Response: $LEADERBOARD_RESPONSE"
fi

echo ""
echo "=== Test Complete ==="
