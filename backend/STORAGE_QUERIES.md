# Nakama Storage Queries

## Overview

Nakama stores game data in the `storage` table using a key-value structure with JSONB values. All leaderboard snapshots, rewards, and idempotency data for this project are stored in the `race_data` collection.

## Storage Table Structure

```sql
\d storage

-- Key columns:
-- collection: Group/namespace for data (e.g., "race_data")
-- key: Unique identifier within collection
-- user_id: Owner (00000000-0000-0000-0000-000000000000 for server-owned)
-- value: JSONB data
-- create_time/update_time: Timestamps
```

---

## Common Queries

### 1. View All Leaderboard Snapshots

```sql
-- Get all snapshots ordered by most recent
SELECT
    key,
    value->>'cycle' as cycle_number,
    jsonb_array_length(value->'top_10') as players_count,
    create_time
FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'snapshot_%'
ORDER BY create_time DESC
LIMIT 10;
```

### 2. View Specific Snapshot Details

```sql
-- Get snapshot for a specific cycle
SELECT
    key,
    jsonb_pretty(value) as snapshot_data
FROM storage
WHERE collection = 'race_data'
  AND key = 'snapshot_5879830';  -- Replace with your cycle number
```

### 3. View All Rewards for a User

```sql
-- Get all rewards for a specific user
SELECT
    key,
    value->>'cycle' as cycle,
    value->>'reward' as reward_type,
    value->>'rank' as rank,
    value->>'race_time' as time,
    create_time
FROM storage
WHERE collection = 'race_data'
  AND user_id = 'fa1ec374-0d2c-41ad-9b93-5f3e15608cfe'  -- Replace with user ID
  AND key LIKE 'reward_%'
ORDER BY create_time DESC;
```

### 4. View Top Winners Across All Cycles

```sql
-- Get all rank 1 winners from snapshots
SELECT
    value->>'cycle' as cycle,
    value->'top_10'->0->>'username' as winner,
    value->'top_10'->0->>'user_id' as user_id,
    (value->'top_10'->0->>'race_time')::float as winning_time,
    create_time as cycle_ended
FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'snapshot_%'
ORDER BY create_time DESC
LIMIT 20;
```

### 5. Count Records by Type

```sql
-- Summary of all race_data storage
SELECT
    CASE
        WHEN key LIKE 'snapshot_%' THEN 'Snapshots'
        WHEN key LIKE 'reward_%' THEN 'Rewards'
        WHEN key LIKE 'idempotency_%' THEN 'Idempotency Keys'
        ELSE 'Other'
    END as data_type,
    COUNT(*) as count,
    pg_size_pretty(SUM(pg_column_size(value))) as total_size,
    MAX(create_time) as latest_record
FROM storage
WHERE collection = 'race_data'
GROUP BY data_type
ORDER BY count DESC;
```

### 6. Find Snapshots with Multiple Players

```sql
-- Find cycles with competitive leaderboards (2+ players)
SELECT
    key,
    value->>'cycle' as cycle,
    jsonb_array_length(value->'top_10') as player_count,
    value->'top_10'->0->>'username' as winner,
    (value->'top_10'->0->>'race_time')::float as winning_time,
    create_time
FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'snapshot_%'
  AND jsonb_array_length(value->'top_10') > 1
ORDER BY create_time DESC;
```

### 7. Verify Idempotency (Check for Duplicates)

```sql
-- Check if any cycle has multiple snapshots (shouldn't happen if idempotent)
SELECT
    value->>'cycle' as cycle,
    COUNT(*) as snapshot_count,
    array_agg(key) as snapshot_keys
FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'snapshot_%'
GROUP BY value->>'cycle'
HAVING COUNT(*) > 1;

-- Should return 0 rows if idempotency is working correctly
```

### 8. Storage Growth Analysis

```sql
-- Analyze storage growth over time
SELECT
    date_trunc('day', create_time) as day,
    COUNT(*) as records_created,
    pg_size_pretty(SUM(pg_column_size(value))) as size_added
FROM storage
WHERE collection = 'race_data'
GROUP BY date_trunc('day', create_time)
ORDER BY day DESC;
```

### 9. Find Oldest Idempotency Keys (Cleanup Candidates)

```sql
-- Find idempotency keys older than 1 hour (could be cleaned up)
SELECT
    key,
    create_time,
    NOW() - create_time as age
FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'idempotency_%'
  AND create_time < NOW() - INTERVAL '1 hour'
ORDER BY create_time ASC
LIMIT 100;
```

### 10. Get Full Leaderboard History for Analysis

```sql
-- Export all snapshots as CSV-friendly format
SELECT
    (value->>'cycle')::bigint as cycle,
    (value->>'timestamp')::bigint as unix_timestamp,
    to_timestamp((value->>'timestamp')::bigint) as readable_time,
    jsonb_array_length(value->'top_10') as players_in_top_10,
    value->'top_10' as leaderboard_json
FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'snapshot_%'
ORDER BY (value->>'cycle')::bigint DESC;
```

---

## Useful Docker Commands

### Connect to Database
```bash
docker exec -it nakama-postgres psql -U nakama -d nakama
```

### Run Query from Command Line
```bash
docker exec nakama-postgres psql -U nakama -d nakama -c "YOUR_QUERY_HERE"
```

### Export Snapshots to JSON File
```bash
docker exec nakama-postgres psql -U nakama -d nakama -t -A -F"," -c \
  "SELECT value FROM storage WHERE collection = 'race_data' AND key LIKE 'snapshot_%'" \
  > snapshots.json
```

---

## Data Retention Strategy

Based on SCALABILITY.md recommendations:

### Current State (No Cleanup)
- Snapshots: **Kept forever** ❌ (grows unbounded)
- Rewards: **Kept forever** ✓ (intentional, part of player history)
- Idempotency keys: **Kept forever** ❌ (only needed for ~1 hour)

### Recommended Cleanup

**Option 1: Archive Old Snapshots (Keep 30 days)**
```sql
-- Archive snapshots older than 30 days to external storage
WITH old_snapshots AS (
    SELECT key, value
    FROM storage
    WHERE collection = 'race_data'
      AND key LIKE 'snapshot_%'
      AND create_time < NOW() - INTERVAL '30 days'
)
-- Export these first, then:
DELETE FROM storage
WHERE collection = 'race_data'
  AND key IN (SELECT key FROM old_snapshots);
```

**Option 2: Add TTL for Idempotency Keys**
```lua
-- In racing.lua, add expiry when writing
nk.storage_write({
    {
        collection = COLLECTION_NAME,
        key = idempotency_storage_key,
        user_id = user_id,
        value = response,
        permission_read = 0,
        permission_write = 0,
        -- Auto-delete after 1 hour
        ttl_seconds = 3600
    }
})
```

**Note:** Nakama storage doesn't natively support TTL in version 3.3.0, so you'd need a periodic cleanup job:

```sql
-- Cleanup job (run daily via cron)
DELETE FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'idempotency_%'
  AND create_time < NOW() - INTERVAL '1 hour';
```

---

## Backup Recommendations

### Full Backup (PostgreSQL)
```bash
docker exec nakama-postgres pg_dump -U nakama nakama > nakama_backup.sql
```

### Snapshots Only
```bash
docker exec nakama-postgres psql -U nakama -d nakama -c \
  "COPY (SELECT * FROM storage WHERE collection = 'race_data' AND key LIKE 'snapshot_%') TO STDOUT WITH CSV HEADER" \
  > snapshots_backup.csv
```

### Rewards Only
```bash
docker exec nakama-postgres psql -U nakama -d nakama -c \
  "COPY (SELECT * FROM storage WHERE collection = 'race_data' AND key LIKE 'reward_%') TO STDOUT WITH CSV HEADER" \
  > rewards_backup.csv
```

---

## Performance Considerations

### Current Indexes
```sql
-- Check indexes on storage table
\d storage

-- Primary key: (collection, read, key, user_id)
-- This makes queries by collection + key very fast ✓
```

### Query Performance
```sql
-- Check query plan for leaderboard snapshot retrieval
EXPLAIN ANALYZE
SELECT value FROM storage
WHERE collection = 'race_data'
  AND key = 'snapshot_5879830';

-- Should use: Index Scan using storage_pkey
-- Execution time: < 1ms ✓
```

### Slow Query: Full Table Scan
```sql
-- This will be slow if you have many records (requires full scan)
SELECT * FROM storage
WHERE value->>'cycle' = '5879830';  -- ❌ No index on JSONB content

-- Better: Use key-based lookup
SELECT * FROM storage
WHERE collection = 'race_data'
  AND key = 'snapshot_5879830';  -- ✓ Uses index
```

---

## Troubleshooting

### Snapshot Not Found
```sql
-- Check if snapshot exists for cycle
SELECT key, create_time
FROM storage
WHERE collection = 'race_data'
  AND key = 'snapshot_5879830';

-- If not found, check scheduler logs:
-- docker logs nakama-scheduler

-- Or check Nakama logs:
-- docker logs nakama-go-server | grep "Leaderboard reset"
```

### Duplicate Snapshots (Idempotency Failure)
```sql
-- Check for duplicate cycle snapshots
SELECT
    value->>'cycle' as cycle,
    COUNT(*) as count
FROM storage
WHERE collection = 'race_data'
  AND key LIKE 'snapshot_%'
GROUP BY value->>'cycle'
HAVING COUNT(*) > 1;
```

### Storage Growth Too Fast
```sql
-- Check what's growing
SELECT
    CASE
        WHEN key LIKE 'snapshot_%' THEN 'Snapshots'
        WHEN key LIKE 'reward_%' THEN 'Rewards'
        WHEN key LIKE 'idempotency_%' THEN 'Idempotency'
    END as type,
    COUNT(*),
    pg_size_pretty(SUM(pg_column_size(value))) as size
FROM storage
WHERE collection = 'race_data'
GROUP BY type;

-- If idempotency keys are large, implement cleanup
```

---

## Summary

**Where data lives:**
- Database: `nakama`
- Table: `storage`
- Collection: `race_data`
- Key patterns:
  - `snapshot_{cycle}` - Leaderboard snapshots
  - `reward_{user_id}_{cycle}` - Player rewards
  - `idempotency_{uuid}` - Submission deduplication

**Access methods:**
1. SQL queries (this document)
2. Nakama Console UI: http://localhost:7351 → Storage tab
3. RPC endpoints: `race_reward_history` (for rewards)
4. Direct API: Nakama Storage API (for programmatic access)
