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
using System.Threading.Tasks;

public class RewardHistoryDisplay : MonoBehaviour
{
    [SerializeField] private NakamaConnectionManager nakamaManager;

    void Start()
    {
        if (nakamaManager == null)
        {
            nakamaManager = FindObjectOfType<NakamaConnectionManager>();
        }
    }

    /// <summary>
    /// Example: Fetch and display reward history
    /// Call this method when you want to show the player's rewards
    /// </summary>
    public async Task FetchAndDisplayRewards()
    {
        var rewardHistory = await nakamaManager.GetRewardHistory();

        if (rewardHistory != null)
        {
            Debug.Log($"=== Reward History for User: {rewardHistory.user_id} ===");
            Debug.Log($"Total Rewards: {rewardHistory.total}");

            foreach (var reward in rewardHistory.rewards)
            {
                Debug.Log($"[Cycle {reward.cycle}] Rank #{reward.rank} - {reward.reward}");
                Debug.Log($"  Username: {reward.username}");
                Debug.Log($"  Race Time: {reward.race_time}s");
                Debug.Log($"  Awarded At: {System.DateTimeOffset.FromUnixTimeSeconds(reward.awarded_at).DateTime}");
            }
        }
        else
        {
            Debug.LogWarning("Failed to fetch reward history");
        }
    }

    /// <summary>
    /// Example: Button click handler for UI
    /// </summary>
    public void OnViewRewardsButtonClicked()
    {
        _ = FetchAndDisplayRewards();
    }
}
