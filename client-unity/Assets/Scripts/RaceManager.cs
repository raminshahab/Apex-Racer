/*
 * Apex-Racer - Racing event simulator using Unity and Nakama
 * Copyright (C) 2024  Ramin Shahab
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using UnityEngine;
using System;
using System.Threading.Tasks;

public class RaceManager : MonoBehaviour
{
    [SerializeField] private NakamaConnectionManager nakamaManager;
    [SerializeField] private string leaderboardId = "race_times";

    private string currentRaceId;
    private long raceStartTime;
    private string currentIdempotentKey;
    private bool raceInProgress = false;

    void Start()
    {
        if (nakamaManager == null)
        {
            nakamaManager = FindObjectOfType<NakamaConnectionManager>();
        }
    }

    /// <summary>
    /// Call this when the race starts
    /// </summary>
    /// <param name="raceId">Unique identifier for the track/race type</param>
    public void StartRace(string raceId)
    {
        if (raceInProgress)
        {
            Debug.LogWarning("Race already in progress!");
            return;
        }

        Debug.Log("[RaceManager] StartRace() called");

        currentRaceId = raceId;
        raceStartTime = DateTime.UtcNow.Ticks;
        currentIdempotentKey = nakamaManager.GenerateIdempotentKey(currentRaceId, raceStartTime);
        raceInProgress = true;

        Debug.Log($"Race started! ID: {currentRaceId}, Idempotent Key: {currentIdempotentKey}");
    }

    /// <summary>
    /// Call this when the race ends to submit results
    /// </summary>
    /// <param name="finalTime">Race completion time in milliseconds</param>
    public async Task SubmitRaceResult(long finalTime)
    {
        if (!raceInProgress)
        {
            Debug.LogError("No race in progress!");
            return;
        }

        // Submit the score with the idempotent key generated at race start
        bool success = await nakamaManager.SubmitLeaderboardScore(
            leaderboardId,
            finalTime,
            currentIdempotentKey
        );

        if (success)
        {
            Debug.Log($"Race result submitted: {finalTime}ms");
        }
        else
        {
            Debug.LogError("Failed to submit race result");
        }

        // Reset race state
        raceInProgress = false;
        currentIdempotentKey = null;
    }

    /// <summary>
    /// Call this if the race is cancelled/abandoned
    /// </summary>
    public void CancelRace()
    {
        raceInProgress = false;
        currentIdempotentKey = null;
        Debug.Log("Race cancelled");
    }

    /// <summary>
    /// Example: Get the current idempotent key if you need to retry submission
    /// </summary>
    public string GetCurrentIdempotentKey()
    {
        return currentIdempotentKey;
    }
}
