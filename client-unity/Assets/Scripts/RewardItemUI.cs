using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RewardItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI cycleText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI rewardNameText;
    [SerializeField] private TextMeshProUGUI raceTimeText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image rewardIcon;

    [Header("Rank Colors")]
    [SerializeField] private Color rank1Color = new Color(1f, 0.84f, 0f); // Gold
    [SerializeField] private Color rank2Color = new Color(0.75f, 0.75f, 0.75f); // Silver
    [SerializeField] private Color rank3Color = new Color(0.8f, 0.5f, 0.2f); // Bronze
    [SerializeField] private Color defaultColor = new Color(0.3f, 0.3f, 0.3f); // Gray

    private RewardData rewardData;

    public void SetRewardData(RewardData reward)
    {
        rewardData = reward;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (rewardData == null) return;

        // Cycle
        if (cycleText != null)
            cycleText.text = $"Cycle {rewardData.cycle}";

        // Rank
        if (rankText != null)
        {
            rankText.text = $"#{rewardData.rank}";

            // Color based on rank
            Color rankColor = GetRankColor(rewardData.rank);
            rankText.color = rankColor;
        }

        // Reward name
        if (rewardNameText != null)
            rewardNameText.text = FormatRewardName(rewardData.reward);

        // Race time
        if (raceTimeText != null)
            raceTimeText.text = $"Time: {rewardData.race_time:F2}s";

        // Date
        if (dateText != null)
        {
            try
            {
                DateTime awardedDate = DateTimeOffset.FromUnixTimeSeconds(rewardData.awarded_at).DateTime;
                dateText.text = awardedDate.ToString("MMM dd, yyyy");
            }
            catch
            {
                dateText.text = "Unknown date";
            }
        }

        // Background color based on rank
        if (backgroundImage != null)
        {
            Color bgColor = GetRankColor(rewardData.rank);
            bgColor.a = 0.3f; // Semi-transparent background
            backgroundImage.color = bgColor;
        }
    }

    private Color GetRankColor(int rank)
    {
        switch (rank)
        {
            case 1:
                return rank1Color;
            case 2:
                return rank2Color;
            case 3:
                return rank3Color;
            default:
                return defaultColor;
        }
    }

    private string FormatRewardName(string rewardName)
    {
        if (string.IsNullOrEmpty(rewardName))
            return "Unknown Reward";

        // Replace underscores with spaces and title case
        string formatted = rewardName.Replace("_", " ");
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted.ToLower());
    }
}
