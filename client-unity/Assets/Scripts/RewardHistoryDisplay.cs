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
