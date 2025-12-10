#!/bin/bash

# Test script for Nakama leaderboard functionality

BASE_URL="http://localhost:7350"
HTTP_KEY="defaultkey"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Nakama Leaderboard Test Script ===${NC}\n"

# Function to register and login a user
register_and_login() {
    local email=$1
    local password=$2
    local username=$3

    # Add random suffix to avoid conflicts
    local random_suffix="$RANDOM$RANDOM"
    local unique_username="${username}_${random_suffix}"

    echo -e "${BLUE}Registering user: $unique_username${NC}"

    # Register/login using custom authentication (simpler than email)
    response=$(curl -s -X POST "$BASE_URL/v2/account/authenticate/custom?create=true&username=$unique_username" \
        -H "Authorization: Basic $(echo -n defaultkey: | base64)" \
        -H "Content-Type: application/json" \
        -d "{\"id\":\"${unique_username}\"}")

    token=$(echo "$response" | jq -r '.token // empty')

    if [ -z "$token" ]; then
        echo -e "${RED}Failed to register/login user: $unique_username${NC}"
        echo "Response: $response"
        return 1
    fi

    echo -e "${GREEN}Successfully logged in: $unique_username${NC}"
    echo "$token"
}

# Function to submit race time
submit_race_time() {
    local token=$1
    local race_time=$2
    local username=$3

    echo -e "\n${BLUE}Submitting race time for $username: ${race_time}s${NC}"

    # Lua RPC endpoints require JSON-encoded string payload and http_key
    response=$(curl -s -X POST "$BASE_URL/v2/rpc/submit_race_time?http_key=$HTTP_KEY" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "\"{\\\"race_time\\\":$race_time}\"")

    # Extract payload from response
    payload=$(echo "$response" | jq -r '.payload // empty')
    if [ -n "$payload" ]; then
        echo "Response: $payload" | jq '.'
    else
        echo "Response: $response"
    fi
}

# Function to get leaderboard
get_leaderboard() {
    local token=$1

    echo -e "\n${BLUE}=== Current Leaderboard ===${NC}"

    # Lua RPC endpoints require empty string payload and http_key
    response=$(curl -s -X POST "$BASE_URL/v2/rpc/get_leaderboard?http_key=$HTTP_KEY" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d '""')

    # Extract and parse payload
    payload=$(echo "$response" | jq -r '.payload // empty')
    if [ -n "$payload" ]; then
        echo "$payload" | jq '.'
    else
        echo "$response"
    fi
}

# Function to get reward history
get_reward_history() {
    local token=$1
    local username=$2

    echo -e "\n${BLUE}=== Reward History for $username ===${NC}"

    # Lua RPC endpoints require empty string payload and http_key
    response=$(curl -s -X POST "$BASE_URL/v2/rpc/race_reward_history?http_key=$HTTP_KEY" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d '""')

    # Extract and parse payload
    payload=$(echo "$response" | jq -r '.payload // empty')
    if [ -n "$payload" ]; then
        echo "$payload" | jq '.'
    else
        echo "$response"
    fi
}

# Main test flow
echo -e "${BLUE}Step 1: Register and login multiple users${NC}"
echo "================================================"

token1=$(register_and_login "racer1@example.com" "password123" "SpeedyRacer1")
token2=$(register_and_login "racer2@example.com" "password123" "FastDriver2")
token3=$(register_and_login "racer3@example.com" "password123" "TurboKing3")
token4=$(register_and_login "racer4@example.com" "password123" "NitroQueen4")
token5=$(register_and_login "racer5@example.com" "password123" "DriftMaster5")

sleep 1

echo -e "\n${BLUE}Step 2: Submit race times${NC}"
echo "================================================"

submit_race_time "$token1" "45.234" "SpeedyRacer1"
sleep 0.5
submit_race_time "$token2" "42.891" "FastDriver2"
sleep 0.5
submit_race_time "$token3" "48.567" "TurboKing3"
sleep 0.5
submit_race_time "$token4" "41.123" "NitroQueen4"
sleep 0.5
submit_race_time "$token5" "43.789" "DriftMaster5"

sleep 1

echo -e "\n${BLUE}Step 3: View leaderboard${NC}"
echo "================================================"
get_leaderboard "$token1"

echo -e "\n${BLUE}Step 4: Try submitting again (should fail - one per cycle)${NC}"
echo "================================================"
submit_race_time "$token1" "40.000" "SpeedyRacer1"

echo -e "\n${BLUE}Step 5: View reward history${NC}"
echo "================================================"
get_reward_history "$token1" "SpeedyRacer1"

echo -e "\n${GREEN}=== Test Complete ===${NC}"
echo -e "${BLUE}Note: Leaderboard tracks cycles (5-minute intervals).${NC}"
echo -e "${BLUE}Users can submit one race time per cycle.${NC}"
echo -e "${BLUE}Lower times rank higher on the leaderboard.${NC}"
