using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RaceUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NakamaConnectionManager nakamaManager;
    [SerializeField] private RaceManager raceManager;

    [Header("Race Panel")]
    [SerializeField] private GameObject racePanel;
    [SerializeField] private TextMeshProUGUI raceStatusText;
    [SerializeField] private TextMeshProUGUI raceTimerText;
    [SerializeField] private Button startRaceButton;
    [SerializeField] private Button finishRaceButton;
    [SerializeField] private TMP_InputField raceIdInput;

    [Header("Rewards Panel")]
    [SerializeField] private GameObject rewardsPanel;
    [SerializeField] private Button viewRewardsButton;
    [SerializeField] private Button closeRewardsButton;
    [SerializeField] private Transform rewardsContentParent;
    [SerializeField] private GameObject rewardItemPrefab;
    [SerializeField] private TextMeshProUGUI rewardsTitleText;
    [SerializeField] private TextMeshProUGUI noRewardsText;

    [Header("Result Display")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button closeResultButton;

    private float startTime;
    private bool isRaceActive = false;

    void Start()
    {
        Debug.Log("[RaceUIManager] Start() called");
        // Find references if not set
        if (nakamaManager == null)
            nakamaManager = FindObjectOfType<NakamaConnectionManager>();

        if (raceManager == null)
            raceManager = FindObjectOfType<RaceManager>();

        // Setup button listeners
        if (startRaceButton != null)
            startRaceButton.onClick.AddListener(OnStartRaceClicked);

        if (finishRaceButton != null)
            finishRaceButton.onClick.AddListener(OnFinishRaceClicked);

        if (viewRewardsButton != null)
            viewRewardsButton.onClick.AddListener(OnViewRewardsClicked);

        if (closeRewardsButton != null)
            closeRewardsButton.onClick.AddListener(() => ShowRewardsPanel(false));

        if (closeResultButton != null)
            closeResultButton.onClick.AddListener(() => ShowResultPanel(false));

        // Initialize UI state
        ShowRacePanel(true);
        ShowRewardsPanel(false);
        ShowResultPanel(false);
        UpdateRaceUI();
    }

    void Update()
    {
        // Update timer during race
        if (isRaceActive && raceTimerText != null)
        {
            float elapsedTime = Time.time - startTime;
            raceTimerText.text = $"Time: {elapsedTime:F2}s";
        }
    }

    public void OnStartRaceClicked()
    {
        Debug.Log("[RaceUIManager] StartRace button clicked");
        
        string raceId = raceIdInput != null ? raceIdInput.text : "default_track";

        if (string.IsNullOrEmpty(raceId))
        {
            raceId = "default_track";
        }

        // Start race through RaceManager
        if (raceManager != null)
        {
            raceManager.StartRace(raceId);
        }

        // Update local UI state
        isRaceActive = true;
        startTime = Time.time;
        UpdateRaceUI();
    }

    public void OnFinishRaceClicked()
    {
        if (!isRaceActive)
        {
            Debug.LogWarning("No race in progress!");
            return;
        }

        float finalTime = Time.time - startTime;
        SubmitRaceResult(finalTime);
    }

    private async void SubmitRaceResult(float timeInSeconds)
    {
        if (raceManager == null)
        {
            Debug.LogError("RaceManager not found!");
            return;
        }

        // Convert to milliseconds
        long timeInMs = (long)(timeInSeconds * 1000);

        // Update UI
        if (raceStatusText != null)
            raceStatusText.text = "Submitting result...";

        // Submit through RaceManager
        await raceManager.SubmitRaceResult(timeInMs);

        // Update UI state
        isRaceActive = false;
        UpdateRaceUI();

        // Show result
        ShowResultPanel(true);
        if (resultText != null)
            resultText.text = $"Race Complete!\n\nTime: {timeInSeconds:F2}s\n\nResult submitted successfully!";
    }

    public void OnViewRewardsClicked()
    {
        ShowRewardsPanel(true);
        LoadRewardHistory();
    }

    private async void LoadRewardHistory()
    {
        if (nakamaManager == null)
        {
            Debug.LogError("NakamaConnectionManager not found!");
            return;
        }

        // Show loading state
        if (rewardsTitleText != null)
            rewardsTitleText.text = "Loading Rewards...";

        // Clear existing rewards
        ClearRewardsList();

        // Fetch rewards
        var rewardHistory = await nakamaManager.GetRewardHistory();

        if (rewardHistory != null && rewardHistory.rewards != null && rewardHistory.rewards.Count > 0)
        {
            // Update title
            if (rewardsTitleText != null)
                rewardsTitleText.text = $"Your Rewards ({rewardHistory.total})";

            // Hide "no rewards" message
            if (noRewardsText != null)
                noRewardsText.gameObject.SetActive(false);

            // Create reward items
            foreach (var reward in rewardHistory.rewards)
            {
                CreateRewardItem(reward);
            }
        }
        else
        {
            // No rewards found
            if (rewardsTitleText != null)
                rewardsTitleText.text = "Your Rewards";

            if (noRewardsText != null)
            {
                noRewardsText.gameObject.SetActive(true);
                noRewardsText.text = "No rewards yet. Complete races to earn rewards!";
            }
        }
    }

    private void CreateRewardItem(RewardData reward)
    {
        if (rewardItemPrefab == null || rewardsContentParent == null)
        {
            Debug.LogWarning("Reward item prefab or content parent not set!");
            return;
        }

        GameObject itemObj = Instantiate(rewardItemPrefab, rewardsContentParent);
        RewardItemUI itemUI = itemObj.GetComponent<RewardItemUI>();

        if (itemUI != null)
        {
            itemUI.SetRewardData(reward);
        }
        else
        {
            // Fallback if RewardItemUI component not found
            TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                texts[0].text = $"Cycle {reward.cycle} - Rank #{reward.rank}\n{reward.reward}\nTime: {reward.race_time:F2}s";
            }
        }
    }

    private void ClearRewardsList()
    {
        if (rewardsContentParent == null) return;

        foreach (Transform child in rewardsContentParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateRaceUI()
    {
        if (raceStatusText != null)
        {
            raceStatusText.text = isRaceActive ? "Race In Progress" : "Ready to Race";
        }

        if (startRaceButton != null)
        {
            startRaceButton.interactable = !isRaceActive;
        }

        if (finishRaceButton != null)
        {
            finishRaceButton.interactable = isRaceActive;
        }

        if (raceTimerText != null)
        {
            if (!isRaceActive)
            {
                raceTimerText.text = "Time: 0.00s";
            }
        }
    }

    public void ShowRacePanel(bool show)
    {
        if (racePanel != null)
            racePanel.SetActive(show);
    }

    public void ShowRewardsPanel(bool show)
    {
        if (rewardsPanel != null)
            rewardsPanel.SetActive(show);
    }

    public void ShowResultPanel(bool show)
    {
        if (resultPanel != null)
            resultPanel.SetActive(show);
    }

    public void SetRaceActive(bool active)
    {
        isRaceActive = active;
        if (active)
        {
            startTime = Time.time;
        }
        UpdateRaceUI();
    }
}
