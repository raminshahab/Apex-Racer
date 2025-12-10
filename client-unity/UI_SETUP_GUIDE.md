# Race UI Setup Guide

This guide explains how to set up the Race and Rewards UI in Unity.

## Prerequisites

1. TextMesh Pro package installed (Window > TextMesh Pro > Import TMP Essential Resources)
2. Scripts created:
   - `RaceUIManager.cs`
   - `RewardItemUI.cs`
   - `NakamaConnectionManager.cs`
   - `RaceManager.cs`

## UI Hierarchy Structure

Create the following GameObject hierarchy in your scene:

```
Canvas (Canvas component)
├── RacePanel (Panel)
│   ├── RaceStatusText (TextMeshProUGUI) - "Ready to Race"
│   ├── RaceTimerText (TextMeshProUGUI) - "Time: 0.00s"
│   ├── RaceIdInput (TMP_InputField) - Placeholder: "Enter track ID"
│   ├── StartRaceButton (Button)
│   │   └── ButtonText (TextMeshProUGUI) - "Start Race"
│   ├── FinishRaceButton (Button)
│   │   └── ButtonText (TextMeshProUGUI) - "Finish Race"
│   └── ViewRewardsButton (Button)
│       └── ButtonText (TextMeshProUGUI) - "View Rewards"
│
├── RewardsPanel (Panel) - Initially disabled
│   ├── Header (Panel/GameObject)
│   │   ├── RewardsTitleText (TextMeshProUGUI) - "Your Rewards"
│   │   └── CloseButton (Button)
│   │       └── ButtonText (TextMeshProUGUI) - "X"
│   ├── ScrollView (Scroll Rect)
│   │   ├── Viewport (Mask + Image)
│   │   │   └── Content (Vertical Layout Group + Content Size Fitter)
│   │   │       └── (Reward items will spawn here)
│   │   └── Scrollbar Vertical (Scrollbar)
│   └── NoRewardsText (TextMeshProUGUI) - "No rewards yet..."
│
└── ResultPanel (Panel) - Initially disabled
    ├── ResultText (TextMeshProUGUI) - "Race Complete!"
    └── CloseResultButton (Button)
        └── ButtonText (TextMeshProUGUI) - "Close"
```

## Step-by-Step Setup

### 1. Create Canvas

1. Right-click in Hierarchy > UI > Canvas
2. Set Canvas Scaler to "Scale With Screen Size"
3. Reference Resolution: 1920x1080

### 2. Create Race Panel

1. Right-click Canvas > UI > Panel (rename to "RacePanel")
2. Anchor to top-left, set size to approximately 400x300
3. Add UI elements as children:
   - **RaceStatusText**: TextMeshProUGUI, font size 24, centered
   - **RaceTimerText**: TextMeshProUGUI, font size 32, centered, bold
   - **RaceIdInput**: TMP_InputField with placeholder
   - **StartRaceButton**: Button with green background
   - **FinishRaceButton**: Button with red background
   - **ViewRewardsButton**: Button with blue background

### 3. Create Rewards Panel

1. Right-click Canvas > UI > Panel (rename to "RewardsPanel")
2. Set to full screen (anchor preset: stretch/stretch)
3. **Disable this GameObject** initially
4. Add background with semi-transparent black color (RGBA: 0,0,0,200)

#### 3.1 Header Section
- Add Panel for header at top
- Add TextMeshProUGUI for title
- Add Button for close (X)

#### 3.2 Scroll View
1. Right-click RewardsPanel > UI > Scroll View
2. Set anchors to stretch
3. Configure Viewport
4. Configure Content:
   - Add **Vertical Layout Group** component:
     - Spacing: 10
     - Child Force Expand: Width only
     - Padding: 10 on all sides
   - Add **Content Size Fitter** component:
     - Vertical Fit: Preferred Size

#### 3.3 No Rewards Text
- Add TextMeshProUGUI at bottom of RewardsPanel
- Center it, initially enabled

### 4. Create Result Panel

1. Right-click Canvas > UI > Panel (rename to "ResultPanel")
2. Center it on screen (300x200 size)
3. **Disable this GameObject** initially
4. Add:
   - **ResultText**: Large centered text
   - **CloseResultButton**: Button at bottom

### 5. Create Reward Item Prefab

1. Create new GameObject in Hierarchy (not under Canvas initially)
2. Rename to "RewardItemPrefab"
3. Add components:
   - **Image** component (background)
   - **Horizontal Layout Group** (spacing: 10, padding: 10)
4. Add child UI elements:
   ```
   RewardItemPrefab
   ├── CycleText (TextMeshProUGUI) - "Cycle X"
   ├── RankText (TextMeshProUGUI) - "#1"
   ├── RewardNameText (TextMeshProUGUI) - "Gold Trophy"
   ├── RaceTimeText (TextMeshProUGUI) - "Time: 45.23s"
   └── DateText (TextMeshProUGUI) - "Nov 23, 2025"
   ```
5. Add **RewardItemUI.cs** script to RewardItemPrefab
6. Drag all child texts to the corresponding fields in RewardItemUI component
7. Drag RewardItemPrefab to your Assets/Prefabs folder
8. Delete RewardItemPrefab from Hierarchy

### 6. Setup RaceUIManager

1. Create empty GameObject under Canvas (rename to "RaceUIManager")
2. Add **RaceUIManager.cs** script
3. Drag all UI elements to their corresponding fields:

   **References:**
   - NakamaConnectionManager (from scene)
   - RaceManager (from scene)

   **Race Panel:**
   - RacePanel GameObject
   - RaceStatusText
   - RaceTimerText
   - StartRaceButton
   - FinishRaceButton
   - RaceIdInput

   **Rewards Panel:**
   - RewardsPanel GameObject
   - ViewRewardsButton
   - CloseRewardsButton
   - RewardsContentParent (Content inside ScrollView)
   - RewardItemPrefab (from Assets/Prefabs)
   - RewardsTitleText
   - NoRewardsText

   **Result Display:**
   - ResultPanel GameObject
   - ResultText
   - CloseResultButton

## Testing

1. Ensure Nakama server is running
2. Press Play in Unity
3. Enter a track ID (or leave default)
4. Click "Start Race"
5. Wait a few seconds
6. Click "Finish Race"
7. Click "View Rewards" to see your reward history

## Styling Tips

### Colors
- **Race Panel**: Light blue background (RGBA: 200, 220, 255, 255)
- **Start Button**: Green (RGBA: 100, 200, 100, 255)
- **Finish Button**: Red (RGBA: 200, 100, 100, 255)
- **Rewards Panel**: Dark semi-transparent (RGBA: 0, 0, 0, 200)

### Fonts
- **Headers**: Size 32-36, Bold
- **Body Text**: Size 18-24
- **Buttons**: Size 20-24, Bold

### Layout
- Use **Layout Groups** for automatic positioning
- Use **Content Size Fitter** for dynamic sizing
- Set **Anchors** properly for responsive design

## Troubleshooting

**"NullReferenceException" errors:**
- Ensure all fields in RaceUIManager are assigned
- Check that NakamaConnectionManager and RaceManager exist in scene

**Rewards not showing:**
- Check Nakama server is running
- Check authentication succeeded (console logs)
- Verify rewards exist in database

**UI elements not visible:**
- Check Canvas Render Mode is "Screen Space - Overlay"
- Verify UI elements are enabled
- Check Z positions (should be 0 for UI)

**Scroll view not scrolling:**
- Ensure Content is larger than Viewport
- Check ScrollRect component is enabled
- Verify Content Size Fitter is on Content GameObject
