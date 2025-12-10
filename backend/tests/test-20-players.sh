#!/bin/bash

# Test script: Submit race results for 20 different players with varied times

BASE_URL="http://localhost:7350"
HTTP_KEY="defaultkey"

echo "=== Testing Leaderboard with 20 Players ==="
echo ""

# Array to store player info
declare -a USERNAMES
declare -a TOKENS
declare -a RACE_TIMES

# Generate varied race times (in seconds)
# Mix of fast, medium, and slow times
TIMES=(
    35.234   # Very fast
    38.567
    40.123
    41.890
    43.456
    45.234   # Medium-fast
    47.890
    49.123
    51.456
    53.789
    55.234   # Medium
    57.890
    60.123
    62.456
    65.890
    68.234   # Medium-slow
    72.567
    76.890
    82.123
    88.456   # Slow
)

echo "1. Creating 20 players and submitting race times..."
echo ""

for i in {1..20}; do
    USERNAME="Racer_${i}_$RANDOM"
    USERNAMES+=("$USERNAME")

    # Authenticate
    AUTH_RESPONSE=$(curl -s -X POST "$BASE_URL/v2/account/authenticate/custom?create=true&username=$USERNAME" \
        --user "defaultkey:" \
        -H "Content-Type: application/json" \
        -d "{\"id\":\"$USERNAME\"}")

    TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.token')
    TOKENS+=("$TOKEN")

    if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
        echo " Failed to authenticate $USERNAME"
        continue
    fi

    # Get race time for this player
    RACE_TIME="${TIMES[$i-1]}"
    RACE_TIMES+=("$RACE_TIME")

    # Submit race time
    IDEMPOTENCY_KEY="race-$(uuidgen)"

    SUBMIT_RESPONSE=$(curl -s -X POST "$BASE_URL/v2/rpc/submit_race_time?http_key=$HTTP_KEY" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "\"{\\\"race_time\\\":$RACE_TIME,\\\"idempotency_key\\\":\\\"$IDEMPOTENCY_KEY\\\"}\"")

    PAYLOAD=$(echo "$SUBMIT_RESPONSE" | jq -r '.payload')

    if [ "$PAYLOAD" != "null" ] && [ -n "$PAYLOAD" ]; then
        SUCCESS=$(echo "$PAYLOAD" | jq -r '.success')
        if [ "$SUCCESS" = "true" ]; then
            printf "  ✓ Player %2d: %-25s - %.3fs\n" "$i" "$USERNAME" "$RACE_TIME"
        else
            echo "  Failed: $USERNAME - $PAYLOAD"
        fi
    else
        echo "  Failed to submit: $USERNAME"
    fi

    # Small delay to avoid overwhelming the server
    sleep 0.1
done

echo ""
echo "2. Fetching full leaderboard (top 100)..."
echo ""

# Use the first token to fetch leaderboard
LEADERBOARD_RESPONSE=$(curl -s -X POST "$BASE_URL/v2/rpc/get_leaderboard?http_key=$HTTP_KEY" \
    -H "Authorization: Bearer ${TOKENS[0]}" \
    -H "Content-Type: application/json" \
    -d '""')

PAYLOAD=$(echo "$LEADERBOARD_RESPONSE" | jq -r '.payload')

if [ "$PAYLOAD" != "null" ] && [ -n "$PAYLOAD" ]; then
    TOTAL=$(echo "$PAYLOAD" | jq '.total')
    CYCLE=$(echo "$PAYLOAD" | jq '.cycle')

    echo "Cycle: $CYCLE"
    echo "Total Submissions: $TOTAL"
    echo ""
    echo "Rank | Username                  | Race Time"
    echo "-----+---------------------------+----------"

    echo "$PAYLOAD" | jq -r '.leaderboard[] | "\(.rank)|\(.username)|\(.race_time)"' | \
    while IFS='|' read -r rank username time; do
        printf "%4s | %-25s | %8.3fs\n" "$rank" "$username" "$time"
    done
else
    echo "Failed to fetch leaderboard"
    echo "Response: $LEADERBOARD_RESPONSE"
fi

echo ""
echo "3. Leaderboard Statistics..."
echo ""

# Calculate stats
FASTEST=$(echo "$PAYLOAD" | jq -r '.leaderboard[0].race_time')
SLOWEST=$(echo "$PAYLOAD" | jq -r '.leaderboard[-1].race_time')
AVG=$(echo "$PAYLOAD" | jq '[.leaderboard[].race_time] | add / length')

echo "  Fastest Time: ${FASTEST}s"
echo "  Slowest Time: ${SLOWEST}s"
echo "  Average Time: ${AVG}s"
echo "  Total Players: $TOTAL"

echo ""
echo "4. Top 3 Winners..."
echo ""

echo "$PAYLOAD" | jq -r '.leaderboard[0:3][] | " \(.rank). \(.username) - \(.race_time)s"'

echo ""
echo "=== Test Complete ==="
echo ""
echo "You can now:"
echo "  - Trigger a manual reset: docker exec nakama-scheduler /usr/local/bin/reset-cron.sh"
echo "  - Check top player's rewards: curl -X POST \"$BASE_URL/v2/rpc/race_reward_history?http_key=$HTTP_KEY\" -H \"Authorization: Bearer ${TOKENS[0]}\" -H \"Content-Type: application/json\" -d '\"\"'"
