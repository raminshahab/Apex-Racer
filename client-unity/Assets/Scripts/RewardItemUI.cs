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
