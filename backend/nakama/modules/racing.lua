--[[
Apex-Racer - Racing event simulator using Unity and Nakama
Copyright (C) 2024  Ramin Shahab

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.
--]]

local nk = require("nakama")

-- Configuration
local LEADERBOARD_ID = "race_times"
local RESET_INTERVAL_SEC = 300  -- 5 minutes
local COLLECTION_NAME = "race_data"

-- Anti-cheat configuration
local MIN_VALID_TIME = 15.0   -- Minimum realistic race time (seconds)
local MAX_VALID_TIME = 180.0  -- Maximum reasonable time (3 minutes)

-- Get current cycle number based on time
local function get_current_cycle()
    return math.floor(os.time() / RESET_INTERVAL_SEC)
end

-- Anti-cheat: Check time bounds and flag suspicious submissions (non-blocking)
local function check_time_bounds(user_id, username, race_time, cycle)
    local is_suspicious = false
    local reason = nil

    if race_time < MIN_VALID_TIME then
        is_suspicious = true
        reason = string.format("too_fast: %.3fs (min: %.1fs)", race_time, MIN_VALID_TIME)
    elseif race_time > MAX_VALID_TIME then
        is_suspicious = true
        reason = string.format("too_slow: %.3fs (max: %.1fs)", race_time, MAX_VALID_TIME)
    end

    if is_suspicious then
        nk.logger_warn(string.format("FLAGGED: User %s submitted suspicious time: %s", username, reason))

        -- flag on server 
        nk.storage_write({
            {
                collection = "flagged_submissions",
                key = string.format("flag_%s_%d", user_id, os.time()),
                user_id = nil,  
                value = {
                    user_id = user_id,
                    username = username,
                    race_time = race_time,
                    cycle = cycle,
                    reason = reason,
                    timestamp = os.time()
                },
                permission_read = 0,
                permission_write = 0
            }
        })

        return true, reason  -- Flagged but allow submission
    end

    return false, nil  
end

-- RPC: Submit race time (idempotent with client-provided key)
local function submit_race_time(context, payload)
    local user_id = context.user_id
    if not user_id or user_id == "" then
        error("User ID not found in context")
    end

    local username = context.username or "unknown"

    local request = nk.json_decode(payload)
    if not request.race_time or request.race_time <= 0 then
        error("race_time must be a positive number")
    end

    -- Require idempotency key from client
    if not request.idempotency_key or request.idempotency_key == "" then
        error("idempotency_key is required")
    end

    -- Check if this idempotency key was already processed 
    local idempotency_storage_key = string.format("idempotency_%s", request.idempotency_key)
    local existing_submissions = nk.storage_read({
        {
            collection = COLLECTION_NAME,
            key = idempotency_storage_key,
            user_id = user_id
        }
    })

    -- If we've seen this idempotency key before, return the cached response
    if existing_submissions and #existing_submissions > 0 then
        local cached_response = existing_submissions[1].value
        nk.logger_info(string.format("User %s re-submitted with idempotency key %s - returning cached response", username, request.idempotency_key))
        return nk.json_encode(cached_response)
    end

    -- Convert time to score (milliseconds for precision)
    local score = math.floor(request.race_time * 1000)

    -- Get current cycle
    local cycle_num = get_current_cycle()

    -- Anti-cheat: Check time bounds (flags but doesn't block)
    local flagged, flag_reason = check_time_bounds(user_id, username, request.race_time, cycle_num)
    if flagged then
        nk.logger_info(string.format("Allowing flagged submission from %s: %s", username, flag_reason))
    end

    -- Check if user already submitted for this cycle (different idempotency key)
    local owner_records = nk.leaderboard_records_list(LEADERBOARD_ID, {user_id}, 1)
    if owner_records and #owner_records > 0 then
        local record = owner_records[1]
        -- Only check if this record actually belongs to the current user
        if record.owner_id == user_id and record.metadata and record.metadata.cycle and record.metadata.cycle == cycle_num then
            error(string.format("Already submitted for this cycle (use same idempotency key to retry)"))
        end
    end

    -- Submit to leaderboard with cycle metadata and idempotency key
    local metadata = {
        cycle = cycle_num,
        timestamp = os.time(),
        idempotency_key = request.idempotency_key
    }
    
    nk.leaderboard_record_write(LEADERBOARD_ID, user_id, username, score, 0, metadata)

    nk.logger_info(string.format("User %s submitted race time: %.3fs for cycle %d (idempotency key: %s)", username, request.race_time, cycle_num, request.idempotency_key))

    -- Create response
    local response = {
        success = true,
        race_time = request.race_time,
        cycle = cycle_num,
        idempotency_key = request.idempotency_key,
        message = "Race time submitted successfully"
    }

    -- Store the response with the idempotency key for future retries
    nk.storage_write({
        {
            collection = COLLECTION_NAME,
            key = idempotency_storage_key,
            user_id = user_id,
            value = response,
            permission_read = 0,  -- Only owner can read
            permission_write = 0  -- Only server can write
        }
    })

    return nk.json_encode(response)
end

-- RPC: Get leaderboard
local function get_leaderboard(context, payload)
    -- Get top 100 records
    local records = nk.leaderboard_records_list(LEADERBOARD_ID, nil, 100)

    local entries = {}
    for _, record in ipairs(records) do
        local entry = {
            rank = record.rank,
            user_id = record.owner_id,
            username = record.username,
            race_time = record.score / 1000.0,
            cycle = 0,
            timestamp = 0
        }

        if record.metadata then
            -- Metadata is already a table in Lua, no need to decode
            if record.metadata.cycle then
                entry.cycle = record.metadata.cycle
            end
            if record.metadata.timestamp then
                entry.timestamp = record.metadata.timestamp
            end
        end

        table.insert(entries, entry)
    end

    local response = {
        leaderboard = entries,
        cycle = get_current_cycle(),
        total = #entries
    }

    return nk.json_encode(response)
end

-- RPC: Get reward history
local function reward_history(context, payload)
    local user_id = context.user_id
    if not user_id or user_id == "" then
        error("User ID not found in context")
    end

    -- Read all storage objects for this user in the race_data collection
    local objects = nk.storage_list(user_id, COLLECTION_NAME, 100)

    local rewards = {}
    if objects then
        for _, obj in ipairs(objects) do
            -- Filter for reward_ prefixed keys
            if string.sub(obj.key, 1, 7) == "reward_" then
                -- Storage value is already a Lua table, no need to decode
                table.insert(rewards, obj.value)
            end
        end
    end

    local response = {
        user_id = user_id,
        rewards = rewards,
        total = #rewards
    }

    return nk.json_encode(response)
end

-- RPC: Get flagged submissions (admin review)
local function get_flagged_submissions(context, payload)
    -- Read all flagged submissions
    local objects = nk.storage_list(nil, "flagged_submissions", 100)

    local flags = {}
    if objects then
        for _, obj in ipairs(objects) do
            table.insert(flags, obj.value)
        end
    end

    -- Sort by most recent
    table.sort(flags, function(a, b)
        return (a.timestamp or 0) > (b.timestamp or 0)
    end)

    local response = {
        total = #flags,
        flags = flags
    }

    return nk.json_encode(response)
end

-- Reset leaderboard and save snapshot (idempotent)
local function reset_leaderboard()
    local cycle_num = get_current_cycle()
    nk.logger_info(string.format("Starting leaderboard reset for cycle %d", cycle_num))

    -- Check if this cycle has already been processed (idempotency check)
    local snapshot_key = string.format("snapshot_%d", cycle_num)
    local existing_snapshots = nk.storage_read({
        {
            collection = COLLECTION_NAME,
            key = snapshot_key,
            user_id = nil
        }
    })

    if existing_snapshots and #existing_snapshots > 0 then
        nk.logger_info(string.format("Cycle %d has already been processed (idempotent) - skipping", cycle_num))
        return {
            success = true,
            cycle = cycle_num,
            message = "Cycle already processed (idempotent)",
            already_processed = true
        }
    end

    -- Get top 10 players
    local records = nk.leaderboard_records_list(LEADERBOARD_ID, nil, 10)

    if not records or #records == 0 then
        nk.logger_info("No records to reset")
        return {
            success = true,
            cycle = cycle_num,
            message = "No records to reset"
        }
    end

    -- Create snapshot
    local snapshot = {}
    for _, record in ipairs(records) do
        table.insert(snapshot, {
            rank = record.rank,
            user_id = record.owner_id,
            username = record.username,
            race_time = record.score / 1000.0,
            score = record.score
        })
    end

    -- Save snapshot to storage (idempotency marker)
    local snapshot_data = {
        cycle = cycle_num,
        timestamp = os.time(),
        top_10 = snapshot,
        processed = true
    }

    nk.storage_write({
        {
            collection = COLLECTION_NAME,
            key = snapshot_key,
            user_id = nil,
            value = snapshot_data,
            permission_read = 1,  -- Public read
            permission_write = 0  -- Server write only
        }
    })

    nk.logger_info(string.format("Snapshot saved with key: %s", snapshot_key))

    -- Award reward to top player (idempotent by design - only written once per cycle)
    if #records > 0 then
        local top_player = records[1]
        local reward = "gold_trophy"
        local reward_key = string.format("reward_%s_%d", top_player.owner_id, cycle_num)

        -- Double-check reward hasn't been awarded (defense in depth)
        local existing_rewards = nk.storage_read({
            {
                collection = COLLECTION_NAME,
                key = reward_key,
                user_id = top_player.owner_id
            }
        })

        if existing_rewards and #existing_rewards > 0 then
            nk.logger_info(string.format("Reward for cycle %d already awarded to %s (idempotent)", cycle_num, top_player.username))
        else
            local reward_data = {
                user_id = top_player.owner_id,
                username = top_player.username,
                cycle = cycle_num,
                reward = reward,
                rank = 1,
                race_time = top_player.score / 1000.0,
                timestamp = os.time(),
                awarded_at = os.time()
            }

            nk.storage_write({
                {
                    collection = COLLECTION_NAME,
                    key = reward_key,
                    user_id = top_player.owner_id,
                    value = reward_data,
                    permission_read = 2,  -- Owner read
                    permission_write = 0  -- Server write only
                }
            })

            nk.logger_info(string.format("Rewarded %s to user %s for cycle %d", reward, top_player.username, cycle_num))
        end
    end

    -- Delete all leaderboard records to reset (top 10)
    for _, record in ipairs(records) do
        pcall(function()
            nk.leaderboard_record_delete(LEADERBOARD_ID, record.owner_id)
        end)
    end

    -- Get all remaining records and delete them too (up to 1000)
    local all_records = nk.leaderboard_records_list(LEADERBOARD_ID, nil, 1000)
    if all_records then
        for _, record in ipairs(all_records) do
            pcall(function()
                nk.leaderboard_record_delete(LEADERBOARD_ID, record.owner_id)
            end)
        end
    end

    nk.logger_info(string.format("Leaderboard reset completed for cycle %d. Top 10 snapshot saved, %d total records cleared", cycle_num, #records))
end

-- RPC: Manual reset trigger (for testing/admin)
local function manual_reset_leaderboard(context, payload)
    nk.logger_info("Manual reset triggered")

    local result = reset_leaderboard()

    return nk.json_encode(result or {
        success = true,
        message = "Reset completed"
    })
end

-- Module initialization - this runs when the module is loaded
nk.logger_info("Lua racing module loaded")

-- Create leaderboard
local success, err = pcall(function()
    nk.leaderboard_create(LEADERBOARD_ID, false, "asc", "best", "", {
        description = "Race times leaderboard - lower is better"
    })
    nk.logger_info(string.format("Leaderboard '%s' created successfully", LEADERBOARD_ID))
end)

if not success then
    nk.logger_error(string.format("Failed to create leaderboard (may already exist): %s", tostring(err)))
end

-- Register RPC endpoints
nk.register_rpc(submit_race_time, "submit_race_time")
nk.register_rpc(get_leaderboard, "get_leaderboard")
nk.register_rpc(reward_history, "race_reward_history")
nk.register_rpc(manual_reset_leaderboard, "manual_reset_leaderboard")
nk.register_rpc(get_flagged_submissions, "get_flagged_submissions")

nk.logger_info("RPC endpoints registered: submit_race_time, get_leaderboard, race_reward_history, manual_reset_leaderboard, get_flagged_submissions")
nk.logger_info(string.format("Leaderboard '%s' initialized. Automatic resets handled by external scheduler.", LEADERBOARD_ID))
