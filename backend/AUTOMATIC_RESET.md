# Automatic Leaderboard Reset System

## Overview

The leaderboard automatically resets every 5 minutes using an external scheduler service. This document explains how the automatic reset system works.

## Architecture

```
┌─────────────────┐
│   Scheduler     │  (Docker Container)
│   Service       │
│                 │
│  Cron: */5 min  │
└────────┬────────┘
         │
         │ HTTP POST
         │ /v2/rpc/manual_reset_leaderboard
         ▼
┌─────────────────┐
│  Nakama Server  │
│                 │
│  Lua Module:    │
│  racing.lua     │
└────────┬────────┘
         │
         │ 1. Snapshot top 10
         │ 2. Award rewards
         │ 3. Clear leaderboard
         ▼
┌─────────────────┐
│   PostgreSQL    │
│   Database      │
└─────────────────┘
```

## Components

### 1. Scheduler Service (`nakama/scheduler/`)

**Purpose:** Triggers leaderboard resets every 5 minutes

**Implementation:**
- Lightweight Alpine Linux container with `dcron`
- Runs `reset-cron.sh` every 5 minutes via cron
- Automatically starts when Nakama is healthy

**Files:**
- `Dockerfile` - Container definition
- `reset-cron.sh` - Script that calls the reset RPC endpoint

**Cron Schedule:**
```
*/5 * * * * /usr/local/bin/reset-cron.sh >> /var/log/cron.log 2>&1
```

### 2. Reset RPC Endpoint (`manual_reset_leaderboard`)

**Location:** `nakama/modules/racing.lua:308-317`

**Function:** `manual_reset_leaderboard(context, payload)`

**Process:**
1. Get current cycle number
2. Check if cycle already processed (idempotency)
3. Fetch top 10 players from leaderboard
4. Create snapshot and save to storage
5. Award reward to top player
6. Delete all leaderboard records
7. Return success response

**Idempotency:**
- Uses cycle-based natural key: `snapshot_{cycle_number}`
- Safe to call multiple times for the same cycle
- Second call returns `already_processed: true`

### 3. Reward Distribution

**Reward:** `gold_trophy` to rank 1 player

**Storage Key:** `reward_{user_id}_{cycle_number}`

**Idempotency Checks:**
1. Primary: Cycle snapshot existence
2. Secondary: Individual reward record check (defense in depth)

**Reward Data Structure:**
```lua
{
    user_id = "user123",
    username = "TopRacer",
    cycle = 5879617,
    reward = "gold_trophy",
    rank = 1,
    race_time = 45.123,
    timestamp = 1700745600,
    awarded_at = 1700745600
}
```

## Monitoring

### View Scheduler Logs
```bash
docker logs -f nakama-scheduler
```

### Check Scheduler Status
```bash
docker ps | grep scheduler
```

### Manually Trigger Reset (Testing)
```bash
docker exec nakama-scheduler /usr/local/bin/reset-cron.sh
```

### Expected Log Output

**Successful Reset:**
```
[2025-11-23 08:00:00] Triggering leaderboard reset...
[2025-11-23 08:00:00] ✓ Reset completed successfully
"message":"Reset completed"
```

**Already Processed (Idempotent):**
```
[2025-11-23 08:00:00] Triggering leaderboard reset...
[2025-11-23 08:00:00] ℹ Cycle already processed (idempotent)
"message":"Cycle already processed (idempotent)"
```

**No Records:**
```
[2025-11-23 08:00:00] Triggering leaderboard reset...
[2025-11-23 08:00:00] ℹ No records to reset
"message":"No records to reset"
```

## Configuration

### Changing Reset Interval

To change the reset interval, modify the cron schedule in `scheduler/Dockerfile`:

```dockerfile
# Every 5 minutes (default)
RUN echo "*/5 * * * * /usr/local/bin/reset-cron.sh >> /var/log/cron.log 2>&1" > /etc/crontabs/root

# Every 10 minutes
RUN echo "*/10 * * * * /usr/local/bin/reset-cron.sh >> /var/log/cron.log 2>&1" > /etc/crontabs/root

# Every hour at minute 0
RUN echo "0 * * * * /usr/local/bin/reset-cron.sh >> /var/log/cron.log 2>&1" > /etc/crontabs/root
```

**Important:** Also update `RESET_INTERVAL_SEC` in `modules/racing.lua`:

```lua
local RESET_INTERVAL_SEC = 300  -- 5 minutes (default)
-- Change to match your cron schedule
```

Then rebuild:
```bash
docker-compose up -d --build scheduler
```

## Troubleshooting

### Scheduler not running
```bash
docker ps | grep scheduler
# If not running:
docker-compose up -d scheduler
```

### No logs appearing
```bash
# Check cron is running
docker exec nakama-scheduler ps aux | grep crond

# Check crontab configuration
docker exec nakama-scheduler cat /etc/crontabs/root
```

### Reset not triggering
```bash
# Manually trigger to test
docker exec nakama-scheduler /usr/local/bin/reset-cron.sh

# Check Nakama is reachable
docker exec nakama-scheduler curl -f http://nakama:7350/
```

### Rewards not being distributed
```bash
# Check Nakama logs
docker logs nakama-go-server | grep -i reward

# Manually check reward history for a user
curl -X POST "http://localhost:7350/v2/rpc/race_reward_history?http_key=defaultkey" \
  -H "Authorization: Bearer USER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '""'
```

## Production Considerations

### 1. Authentication
The scheduler currently uses the HTTP key for authentication. In production:
- Create a dedicated service account in Nakama
- Use proper JWT tokens with limited permissions
- Rotate credentials regularly

### 2. Monitoring
- Set up alerting for scheduler failures
- Monitor reward distribution metrics
- Track cycle completion rates

### 3. Backup Strategy
- Snapshots are stored in Nakama storage
- Regular database backups recommended
- Consider archiving old snapshots to S3/external storage

### 4. High Availability
- Run multiple scheduler instances with leader election
- Use distributed cron (e.g., Kubernetes CronJob)
- Idempotency ensures safe concurrent execution

## Testing

Run the comprehensive test:
```bash
./test-automatic-reset.sh
```

This verifies:
- ✓ Scheduler service is running
- ✓ Reset functionality works
- ✓ Rewards are distributed correctly
- ✓ Leaderboard is cleared
- ✓ Idempotency is maintained

## Summary

The automatic reset system is:
- **Reliable:** Docker-based scheduler with automatic restart
- **Idempotent:** Safe to retry, no duplicate rewards
- **Observable:** Comprehensive logging and monitoring
- **Configurable:** Easy to adjust reset intervals
- **Production-ready:** Includes error handling and defense-in-depth
