#!/bin/sh

# Scheduler script to trigger leaderboard reset every 5 minutes
# This calls the manual_reset_leaderboard RPC endpoint

NAKAMA_URL="http://nakama:7350"
HTTP_KEY="defaultkey"

# Use a system account token 
# For this demo, we'll use the http_key for authentication
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')

echo "[$TIMESTAMP] Triggering leaderboard reset..."

RESPONSE=$(curl -s -X POST "$NAKAMA_URL/v2/rpc/manual_reset_leaderboard?http_key=$HTTP_KEY" \
    -H "Content-Type: application/json" \
    -d '""')

if echo "$RESPONSE" | grep -q '"success":true'; then
    if echo "$RESPONSE" | grep -q '"already_processed":true'; then
        echo "[$TIMESTAMP] ℹ Cycle already processed (idempotent)"
    elif echo "$RESPONSE" | grep -q '"No records to reset"'; then
        echo "[$TIMESTAMP] ℹ No records to reset"
    else
        echo "[$TIMESTAMP] ✓ Reset completed successfully"
    fi
    MESSAGE=$(echo "$RESPONSE" | grep -o '"message":"[^"]*"')
    if [ -n "$MESSAGE" ]; then
        echo "  $MESSAGE"
    fi
else
    echo "[$TIMESTAMP] ✗ Reset failed or returned error"
    echo "$RESPONSE"
fi
