using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;

public class UISetupHelper : EditorWindow
{
    [MenuItem("Tools/Race UI/Create Race UI Structure")]
    public static void CreateRaceUIStructure()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Create Race Panel
        GameObject racePanel = CreatePanel(canvas.transform, "RacePanel", new Vector2(400, 350));
        SetAnchor(racePanel.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10));

        CreateText(racePanel.transform, "RaceStatusText", "Ready to Race", 24, new Vector2(0, -30));
        CreateText(racePanel.transform, "RaceTimerText", "Time: 0.00s", 32, new Vector2(0, -80));
        CreateInputField(racePanel.transform, "RaceIdInput", new Vector2(0, -130));
        CreateButton(racePanel.transform, "StartRaceButton", "Start Race", new Vector2(0, -180), new Color(0.4f, 0.8f, 0.4f));
        CreateButton(racePanel.transform, "FinishRaceButton", "Finish Race", new Vector2(0, -230), new Color(0.8f, 0.4f, 0.4f));
        CreateButton(racePanel.transform, "ViewRewardsButton", "View Rewards", new Vector2(0, -280), new Color(0.4f, 0.6f, 1f));

        // Create Rewards Panel
        GameObject rewardsPanel = CreateFullScreenPanel(canvas.transform, "RewardsPanel");
        rewardsPanel.SetActive(false);
        rewardsPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        // Header
        GameObject header = CreatePanel(rewardsPanel.transform, "Header", new Vector2(0, 80));
        SetAnchor(header.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), Vector2.zero);
        CreateText(header.transform, "RewardsTitleText", "Your Rewards", 32, new Vector2(-50, 0));
        CreateButton(header.transform, "CloseButton", "X", new Vector2(-20, -10), Color.white, new Vector2(60, 60));

        // Scroll View
        GameObject scrollView = CreateScrollView(rewardsPanel.transform, "RewardsScrollView");

        // No Rewards Text
        CreateText(rewardsPanel.transform, "NoRewardsText", "No rewards yet. Complete races to earn rewards!", 20, new Vector2(0, 0));

        // Create Result Panel
        GameObject resultPanel = CreatePanel(canvas.transform, "ResultPanel", new Vector2(400, 250));
        resultPanel.SetActive(false);
        SetAnchor(resultPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        CreateText(resultPanel.transform, "ResultText", "Race Complete!", 28, new Vector2(0, 30));
        CreateButton(resultPanel.transform, "CloseResultButton", "Close", new Vector2(0, -70), new Color(0.6f, 0.6f, 0.6f));

        Debug.Log("Race UI Structure created successfully! Please assign references in RaceUIManager.");
    }

    [MenuItem("Tools/Race UI/Create Reward Item Prefab")]
    public static void CreateRewardItemPrefab()
    {
        GameObject rewardItem = new GameObject("RewardItemPrefab");

        // Add Image background
        Image bg = rewardItem.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        RectTransform rt = rewardItem.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700, 80);

        // Add Horizontal Layout Group
        HorizontalLayoutGroup layout = rewardItem.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        // Create text elements
        CreateRewardItemText(rewardItem.transform, "CycleText", "Cycle 1", 80);
        CreateRewardItemText(rewardItem.transform, "RankText", "#1", 60);
        CreateRewardItemText(rewardItem.transform, "RewardNameText", "Gold Trophy", 150);
        CreateRewardItemText(rewardItem.transform, "RaceTimeText", "45.23s", 100);
        CreateRewardItemText(rewardItem.transform, "DateText", "Nov 23", 100);

        // Add RewardItemUI script
        rewardItem.AddComponent<RewardItemUI>();

        Debug.Log("RewardItemPrefab created! Drag to Assets/Prefabs folder and assign fields in RewardItemUI component.");
        Selection.activeGameObject = rewardItem;
    }

    // Helper methods
    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.8f, 0.85f, 0.9f, 1f);

        return panel;
    }

    private static GameObject CreateFullScreenPanel(Transform parent, string name)
    {
        GameObject panel = CreatePanel(parent, name, Vector2.zero);
        RectTransform rt = panel.GetComponent<RectTransform>();
        SetAnchor(rt, Vector2.zero, Vector2.one, Vector2.zero);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(350, 50);
        rt.anchoredPosition = position;

        return textObj;
    }

    private static GameObject CreateRewardItemText(Transform parent, string name, string text, float width)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 60);

        LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;

        return textObj;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 position, Color color, Vector2? size = null)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = size ?? new Vector2(350, 40);
        rt.anchoredPosition = position;

        Image img = buttonObj.AddComponent<Image>();
        img.color = color;

        Button btn = buttonObj.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return buttonObj;
    }

    private static GameObject CreateInputField(Transform parent, string name, Vector2 position)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);

        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(350, 40);
        rt.anchoredPosition = position;

        Image img = inputObj.AddComponent<Image>();
        img.color = Color.white;

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRt = textArea.AddComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(10, 6);
        textAreaRt.offsetMax = new Vector2(-10, -7);
        textArea.AddComponent<RectMask2D>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 18;
        tmp.color = Color.black;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = "Enter track ID...";
        placeholderTmp.fontSize = 18;
        placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f);
        placeholderTmp.fontStyle = FontStyles.Italic;
        RectTransform placeholderRt = placeholderObj.GetComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = Vector2.zero;
        placeholderRt.offsetMax = Vector2.zero;

        inputField.textComponent = tmp;
        inputField.placeholder = placeholderTmp;

        return inputObj;
    }

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        GameObject scrollView = new GameObject(name);
        scrollView.transform.SetParent(parent, false);

        RectTransform rt = scrollView.GetComponent<RectTransform>();
        SetAnchor(rt, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero);
        rt.offsetMin = new Vector2(50, 100);
        rt.offsetMax = new Vector2(-50, -100);

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRt = viewport.AddComponent<RectTransform>();
        SetAnchor(viewportRt, Vector2.zero, Vector2.one, Vector2.zero);
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.AddComponent<RectTransform>();
        SetAnchor(contentRt, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.viewport = viewportRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return scrollView;
    }

    private static void SetAnchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
    }
}
#endif
