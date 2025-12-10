# Database Schema Documentation

## Overview

The Apex Racer backend uses PostgreSQL through **Nakama's built-in systems** for the MVP implementation.

## ⚠️ Important: What's Actually Being Used

### Current Implementation (MVP)

The **live system** uses:

1. **Nakama's Built-in Leaderboard System**
   - Leaderboard ID: `race_times`
   - Stores: player scores, ranks, metadata (cycle number, timestamp)
   - Managed by: Nakama's internal tables

2. **Nakama Storage API** (collection: `race_data`)
   - `snapshot_{cycle}` - Top 10 players per cycle
   - `reward_{user_id}_{cycle}` - Reward history per player
   - `idempotency_{uuid}` - Submission deduplication cache
   - `flag_{user_id}_{timestamp}` - Anti-cheat flagged submissions

**No custom tables are used in the MVP!**

### Future Schema (Planned - Not Implemented)

The SQL schema in `migrations/001_racing_schema.sql` defines tables for **future features**:
- Regional leaderboards (with `region_id`)
- Multiple concurrent events
- Advanced race event management

See the comments in `001_racing_schema.sql` for when to migrate to this schema.

---

## Custom Schema Created (For Future Use)

### Tables

#### 1. `players`
Stores racing player information (extends Nakama's built-in user system).

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key, player identifier |
| display_name | TEXT | Player's display name |
| created_at | TIMESTAMPTZ | Account creation timestamp |

#### 2. `race_events`
Defines racing events with time windows, regions, and car classes.

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key (auto-generated) |
| name | TEXT | Event name (e.g., "Week 42 1/4 Mile Event") |
| region_id | TEXT | Region identifier ('us', 'eu', etc.) |
| race_type | TEXT | Race type ('quarter_mile', etc.) |
| car_class | TEXT | Car class ('A', 'B', etc.) |
| starts_at | TIMESTAMPTZ | Event start time |
| ends_at | TIMESTAMPTZ | Event end time |
| is_active | BOOLEAN | Whether event is currently active |
| created_at | TIMESTAMPTZ | Record creation timestamp |

**Indexes:**
- Primary key on `id`
- Partial index on active events: `(is_active, starts_at, ends_at) WHERE is_active = TRUE`
- Index on time range: `(starts_at, ends_at)`

#### 3. `race_results`
Stores individual race results submitted by players.

| Column | Type | Description |
|--------|------|-------------|
| id | BIGSERIAL | Primary key (auto-increment) |
| event_id | UUID | Foreign key to race_events |
| player_id | UUID | Foreign key to players |
| region_id | TEXT | Region where race was performed |
| race_type | TEXT | Type of race |
| car_class | TEXT | Car class used |
| elapsed_time_ms | INTEGER | Race time in milliseconds (must be > 0) |
| submitted_at | TIMESTAMPTZ | Submission timestamp |
| validated | BOOLEAN | Anti-cheat validation status |
| source | TEXT | Source of submission ('game_client', 'admin', etc.) |

**Indexes:**
- Primary key on `id`
- Index on `event_id` for event lookups
- Index on `player_id` for player history
- Index on `submitted_at DESC` for recent results
- Composite leaderboard index: `(event_id, elapsed_time_ms ASC, submitted_at ASC) WHERE validated = TRUE`
- Unique index: `(event_id, player_id, elapsed_time_ms)` - ensures one record per player per event per time

**Constraints:**
- `elapsed_time_ms > 0` (times must be positive)
- Foreign keys with `ON DELETE CASCADE` to race_events and players

## Applying Migrations

### Initial Setup
```bash
# Apply all migrations
./apply-migrations.sh
```

### Adding New Migrations
1. Create a new SQL file in `nakama/migrations/` with incrementing number prefix:
   - `002_your_migration_name.sql`
   - `003_another_migration.sql`

2. Run the migration script:
   ```bash
   ./apply-migrations.sh
   ```

## Testing

### Running Unit Tests
```bash
# Run all database unit tests
./test-database.sh
```

### Test Coverage

The test suite (`nakama/migrations/test_schema.sql`) includes 16 comprehensive tests:

1. **Data Insertion Tests**
   - Insert valid players
   - Insert valid race events
   - Insert valid race results

2. **Constraint Tests**
   - CHECK constraint: Negative elapsed_time_ms rejection
   - CHECK constraint: Zero elapsed_time_ms rejection
   - UNIQUE constraint: Duplicate (event, player, time) rejection
   - UNIQUE constraint: Different times for same player/event acceptance
   - Foreign key constraint: Invalid player_id rejection
   - Foreign key constraint: Invalid event_id rejection

3. **Data Integrity Tests**
   - CASCADE delete: Player deletion cascades to results
   - CASCADE delete: Event deletion cascades to results
   - Default values: Verify all default values are applied

4. **Query Tests**
   - Leaderboard query: Returns unique players
   - Leaderboard ordering: Fastest player appears first
   - Active events query: Returns only currently active events
   - Index usage: Verifies leaderboard queries use proper indexes

### Test Features

- **Transactional**: All tests run in a transaction that is rolled back, leaving no test data
- **Non-destructive**: Safe to run against production database (though not recommended)
- **Self-contained**: Creates and cleans up its own test data
- **Index verification**: Includes EXPLAIN plans to verify index usage

## Database Queries

### Check Tables
```bash
docker exec nakama-postgres psql -U nakama -d nakama -c "\dt"
```

### View Table Structure
```bash
docker exec nakama-postgres psql -U nakama -d nakama -c "\d players"
docker exec nakama-postgres psql -U nakama -d nakama -c "\d race_events"
docker exec nakama-postgres psql -U nakama -d nakama -c "\d race_results"
```

### Connect Directly to Database
```bash
docker exec -it nakama-postgres psql -U nakama -d nakama
```

## Common Use Cases

### Leaderboard Query (Top 10 for an Event)
```sql
SELECT
    rr.player_id,
    p.display_name,
    MIN(rr.elapsed_time_ms) as best_time,
    MIN(rr.submitted_at) as first_achieved
FROM race_results rr
JOIN players p ON p.id = rr.player_id
WHERE
    rr.event_id = 'event-uuid-here'
    AND rr.validated = TRUE
GROUP BY rr.player_id, p.display_name
ORDER BY best_time ASC, first_achieved ASC
LIMIT 10;
```

### Get Active Events
```sql
SELECT * FROM race_events
WHERE is_active = TRUE
  AND starts_at <= NOW()
  AND ends_at >= NOW()
ORDER BY ends_at ASC;
```

### Player's Race History
```sql
SELECT
    re.name as event_name,
    rr.elapsed_time_ms,
    rr.submitted_at,
    rr.validated
FROM race_results rr
JOIN race_events re ON re.id = rr.event_id
WHERE rr.player_id = 'player-uuid-here'
ORDER BY rr.submitted_at DESC
LIMIT 20;
```

## Performance Considerations

1. **Leaderboard Queries**: Use the composite index `idx_race_results_leaderboard` for optimal performance
2. **Time-based Queries**: The partial index on active events speeds up current event lookups
3. **Unique Constraint**: The `ux_best_result_per_player_event` index prevents duplicate times but allows different times
4. **Foreign Key Cascades**: Deleting an event or player automatically removes associated race results

## Connection Details

- **Host**: localhost (when accessing from host machine)
- **Port**: 5432
- **Database**: nakama
- **User**: nakama
- **Password**: nakama

⚠️ **Security Note**: Change these credentials for production use!

## Next Steps

Now that the database schema is ready, you can:

1. Implement RPC endpoints for:
   - Submitting race results
   - Querying leaderboards
   - Getting active events
   - Player race history

2. Add validation logic:
   - Anti-cheat checks
   - Time validation
   - Rate limiting

3. Implement the 5-minute reset cycle mentioned in SETUP_NOTES.md

4. Add reward distribution based on leaderboard rankings
