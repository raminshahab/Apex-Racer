-- *** Future Schema: NOT CURRENTLY IMPLEMENTED ***  
-- Migration: 001_racing_schema
-- Created: 2025-11-21

-- Players table (simplified - Nakama already has users table)
-- This extends Nakama's built-in user system with racing-specific data
CREATE TABLE IF NOT EXISTS players (
    id            UUID PRIMARY KEY,
    display_name  TEXT NOT NULL,
    created_at    TIMESTAMPTZ DEFAULT now()
);

-- Racing events (e.g. "Week 42 1/4 Mile Event")
CREATE TABLE IF NOT EXISTS race_events (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name          TEXT NOT NULL,
    region_id     TEXT NOT NULL,   -- 'us', 'eu', etc.
    race_type     TEXT NOT NULL,   -- 'quarter_mile', etc.
    car_class     TEXT NOT NULL,   -- 'A', 'B', ...
    starts_at     TIMESTAMPTZ NOT NULL,
    ends_at       TIMESTAMPTZ NOT NULL,
    is_active     BOOLEAN DEFAULT TRUE,
    created_at    TIMESTAMPTZ DEFAULT now()
);

-- Individual race results (raw writes)
CREATE TABLE IF NOT EXISTS race_results (
    id             BIGSERIAL PRIMARY KEY,
    event_id       UUID REFERENCES race_events(id) ON DELETE CASCADE,
    player_id      UUID REFERENCES players(id) ON DELETE CASCADE,
    region_id      TEXT NOT NULL,
    race_type      TEXT NOT NULL,
    car_class      TEXT NOT NULL,
    elapsed_time_ms INTEGER NOT NULL CHECK (elapsed_time_ms > 0),
    submitted_at   TIMESTAMPTZ DEFAULT now(),
    validated      BOOLEAN DEFAULT TRUE,
    source         TEXT DEFAULT 'game_client'
);

-- Indexes for fast queries
CREATE INDEX IF NOT EXISTS idx_race_results_event_id
    ON race_results(event_id);

CREATE INDEX IF NOT EXISTS idx_race_results_player_id
    ON race_results(player_id);

CREATE INDEX IF NOT EXISTS idx_race_results_submitted_at
    ON race_results(submitted_at DESC);

CREATE INDEX IF NOT EXISTS idx_race_events_active
    ON race_events(is_active, starts_at, ends_at)
    WHERE is_active = TRUE;

CREATE INDEX IF NOT EXISTS idx_race_events_time_range
    ON race_events(starts_at, ends_at);

-- Composite index for leaderboard queries
CREATE INDEX IF NOT EXISTS idx_race_results_leaderboard
    ON race_results(event_id, elapsed_time_ms ASC, submitted_at ASC)
    WHERE validated = TRUE;

-- Best result per player per event (for fast lookup)
-- This allows only one row per (event_id, player_id, elapsed_time_ms) combination
CREATE UNIQUE INDEX IF NOT EXISTS ux_best_result_per_player_event
    ON race_results (event_id, player_id, elapsed_time_ms);

-- Comments for documentation
COMMENT ON TABLE players IS 'Racing players - extends Nakama users with racing-specific data';
COMMENT ON TABLE race_events IS 'Racing events with time windows, regions, and car classes';
COMMENT ON TABLE race_results IS 'Individual race results submitted by players';

COMMENT ON COLUMN race_results.elapsed_time_ms IS 'Race completion time in milliseconds';
COMMENT ON COLUMN race_results.validated IS 'Whether this result passed anti-cheat validation';
COMMENT ON COLUMN race_results.source IS 'Source of the submission (game_client, admin, etc.)';
