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
using Nakama;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public class NakamaConnectionManager : MonoBehaviour
{
    // Default Local Nakama Server Settings
    private const string Scheme = "http";
    private const string Host = "127.0.0.1";
    private const int Port = 7350;
    private const string ServerKey = "defaultkey";

    // Retry and Backoff Settings
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private int initialBackoffMs = 500;
    [SerializeField] private int maxBackoffMs = 8000;
    [SerializeField] private int timeoutMs = 10000; // 10 second timeout per request

    private IClient _client;
    private ISession _session;

    public bool IsAuthenticated => _session != null && !_session.IsExpired;

    void Awake()
    {
        // Create the client object to connect to the Nakama server
        _client = new Client(Scheme, Host, Port, ServerKey, UnityWebRequestAdapter.Instance);
    }

    async void Start()
    {
        await ConnectAndAuthenticate();
    }

    /// <summary>
    /// Executes an async operation with exponential backoff retry logic and timeout
    /// </summary>
    private async Task<T> RetryWithExponentialBackoff<T>(Func<Task<T>> operation, string operationName)
    {
        int attempt = 0;
        int currentBackoffMs = initialBackoffMs;

        while (attempt <= maxRetries)
        {
            try
            {
                // Create a task with timeout
                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    var operationTask = operation();
                    var timeoutTask = Task.Delay(timeoutMs, cts.Token);

                    var completedTask = await Task.WhenAny(operationTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException($"{operationName} exceeded timeout of {timeoutMs}ms");
                    }

                    cts.Cancel(); // Cancel the timeout task if operation completed
                    return await operationTask;
                }
            }
            catch (TimeoutException)
            {
                attempt++;

                if (attempt > maxRetries)
                {
                    Debug.LogError($"{operationName} timed out after {maxRetries} retries (timeout: {timeoutMs}ms)");
                    throw;
                }

                Debug.LogWarning($"{operationName} timed out (attempt {attempt}/{maxRetries}). Retrying in {currentBackoffMs}ms...");
            }
            catch (Exception ex)
            {
                attempt++;

                if (attempt > maxRetries)
                {
                    Debug.LogError($"{operationName} failed after {maxRetries} retries: {ex.Message}");
                    throw;
                }

                Debug.LogWarning($"{operationName} failed (attempt {attempt}/{maxRetries}). Retrying in {currentBackoffMs}ms... Error: {ex.Message}");
            }

            // Wait for the backoff period
            await Task.Delay(currentBackoffMs);

            // Exponential backoff: double the wait time for next retry
            currentBackoffMs = Math.Min(currentBackoffMs * 2, maxBackoffMs);
        }

        throw new Exception($"{operationName} failed after {maxRetries} retries");
    }

    private async Task ConnectAndAuthenticate()
    {
        try
        {
            // Authenticate using a Device ID (simple, default method)
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            _session = await RetryWithExponentialBackoff(
                async () => await _client.AuthenticateDeviceAsync(deviceId),
                "Authentication"
            );

            Debug.Log($"Successfully authenticated! User ID: {_session.UserId}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Authentication failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a unique idempotent key for a race submission.
    /// This key ensures that duplicate submissions of the same race result are identifiable.
    /// </summary>
    /// <param name="raceId">Optional race/track identifier</param>
    /// <param name="raceStartTime">Optional race start timestamp (ticks)</param>
    /// <returns>A unique idempotent key string</returns>
    public string GenerateIdempotentKey(string raceId = null, long? raceStartTime = null)
    {
        // Use a combination of user ID, race ID, and timestamp (or generate a new GUID)
        if (raceStartTime.HasValue && !string.IsNullOrEmpty(raceId))
        {
            // Deterministic key based on race parameters
            return $"{_session.UserId}_{raceId}_{raceStartTime.Value}";
        }
        else
        {
            // Generate a unique GUID for this race attempt
            return $"{_session.UserId}_{Guid.NewGuid().ToString()}";
        }
    }

    public async Task<bool> SubmitLeaderboardScore(string leaderboardId, long score, string idempotentKey, long subscore = 0, string metadata = null)
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("Cannot submit score: User is not authenticated");
            return false;
        }

        try
        {
            // Embed the idempotent key in the metadata
            string metadataWithKey = metadata ?? "";
            if (!string.IsNullOrEmpty(metadataWithKey))
            {
                metadataWithKey = $"{{\"idempotentKey\":\"{idempotentKey}\",\"data\":{metadata}}}";
            }
            else
            {
                metadataWithKey = $"{{\"idempotentKey\":\"{idempotentKey}\"}}";
            }

            await RetryWithExponentialBackoff(
                async () => {
                    await _client.WriteLeaderboardRecordAsync(_session, leaderboardId, score, subscore, metadataWithKey);
                    return true;
                },
                "Submit Leaderboard Score"
            );

            Debug.Log($"Successfully submitted score {score} to leaderboard {leaderboardId} with idempotent key: {idempotentKey}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to submit leaderboard score: {ex.Message}");
            return false;
        }
    }

    public async Task<IApiLeaderboardRecordList> GetLeaderboardRecords(string leaderboardId, int limit = 10)
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("Cannot fetch leaderboard: User is not authenticated");
            return null;
        }

        try
        {
            var result = await RetryWithExponentialBackoff(
                async () => await _client.ListLeaderboardRecordsAsync(_session, leaderboardId, limit: limit),
                "Get Leaderboard Records"
            );

            Debug.Log($"Successfully fetched {result.Records.Count()} leaderboard records");
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch leaderboard records: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches the reward history for the authenticated user
    /// </summary>
    /// <returns>RewardHistoryResponse containing the user's rewards</returns>
    public async Task<RewardHistoryResponse> GetRewardHistory()
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("Cannot fetch reward history: User is not authenticated");
            return null;
        }

        try
        {
            var result = await RetryWithExponentialBackoff(
                async () => await _client.RpcAsync(_session, "race_reward_history", "{}"),
                "Get Reward History"
            );

            var response = JsonUtility.FromJson<RewardHistoryResponse>(result.Payload);
            Debug.Log($"Successfully fetched {response.total} rewards for user {response.user_id}");
            return response;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch reward history: {ex.Message}");
            return null;
        }
    }
}

[System.Serializable]
public class RewardHistoryResponse
{
    public string user_id;
    public List<RewardData> rewards;
    public int total;
}

[System.Serializable]
public class RewardData
{
    public string user_id;
    public string username;
    public int cycle;
    public string reward;
    public int rank;
    public float race_time;
    public long timestamp;
    public long awarded_at;
}