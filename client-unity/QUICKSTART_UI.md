# Race UI Quick Start Guide

## Automated Setup (Recommended)

The easiest way to set up the UI is using the automated helper:

1. Open Unity
2. In the menu bar, go to **Tools > Race UI > Create Race UI Structure**
3. This will create the entire UI hierarchy automatically
4. Go to **Tools > Race UI > Create Reward Item Prefab**
5. Drag the created "RewardItemPrefab" from Hierarchy to your Assets/Prefabs folder
6. Delete the RewardItemPrefab from the Hierarchy

### Assign References

After creating the UI structure:

1. Find the "RaceUIManager" GameObject in the Canvas
2. In the Inspector, assign all the serialized fields:
   - Drag NakamaConnectionManager from scene
   - Drag RaceManager from scene
   - Drag all UI elements to their respective fields (they should match the names)
   - Drag the RewardItemPrefab from Assets/Prefabs folder

3. In the RewardItemPrefab:
   - Select it in Assets/Prefabs
   - In the Inspector, assign all TextMeshProUGUI components to the RewardItemUI script fields

## Manual Setup

If you prefer to create the UI manually, follow the detailed instructions in `UI_SETUP_GUIDE.md`.

## Testing the UI

### 1. Start Race
1. Press Play in Unity
2. (Optional) Enter a track ID in the input field
3. Click "Start Race" button
4. Watch the timer count up

### 2. Finish Race
1. Click "Finish Race" button after a few seconds
2. The result will be submitted to Nakama
3. A result panel will appear showing your time

### 3. View Rewards
1. Click "View Rewards" button
2. Your reward history will load from Nakama
3. Each reward shows:
   - Cycle number
   - Rank achieved
   - Reward type
   - Race time
   - Date awarded

## File Structure

```
Assets/
├── Scripts/
│   ├── RaceUIManager.cs          # Main UI controller
│   ├── RewardItemUI.cs            # Individual reward item display
│   ├── RaceManager.cs             # Race logic (existing)
│   ├── NakamaConnectionManager.cs # Network layer (existing)
│   └── Editor/
│       └── UISetupHelper.cs       # Automated UI creation tool
└── Prefabs/
    └── RewardItemPrefab.prefab    # Reward list item template
```

## Features

### Race Panel
- **Race Status**: Shows "Ready to Race" or "Race In Progress"
- **Timer**: Real-time race timer
- **Track ID Input**: Enter custom track identifier
- **Start Race**: Begins a new race session
- **Finish Race**: Submits race result to server
- **View Rewards**: Opens reward history panel

### Rewards Panel
- **Scrollable List**: View all your earned rewards
- **Rank Colors**:
  - Gold (#1)
  - Silver (#2)
  - Bronze (#3)
  - Gray (other ranks)
- **Reward Details**: Cycle, rank, reward type, time, date
- **Close Button**: Return to race panel

### Result Panel
- Shows completion message
- Displays final race time
- Confirms submission success

## Customization

### Colors
Edit colors in `RaceUIManager.cs` or directly in Unity:
- Button colors
- Panel backgrounds
- Text colors

### Fonts
1. Import custom fonts to Unity
2. Create TextMesh Pro font assets
3. Assign to TextMeshProUGUI components

### Layout
Adjust RectTransform properties:
- Position
- Size
- Anchors
- Pivot

### Rank Colors
Edit in `RewardItemUI.cs`:
```csharp
[SerializeField] private Color rank1Color = new Color(1f, 0.84f, 0f); // Gold
[SerializeField] private Color rank2Color = new Color(0.75f, 0.75f, 0.75f); // Silver
[SerializeField] private Color rank3Color = new Color(0.8f, 0.5f, 0.2f); // Bronze
```

## Common Issues

### UI not responding to clicks
- Ensure EventSystem exists in scene (auto-created with Canvas)
- Check that buttons have the Button component
- Verify Canvas GraphicRaycaster is enabled

### Rewards not loading
- Check Nakama server is running and accessible
- Verify authentication succeeded (check console logs)
- Ensure reward history RPC is registered on server

### Layout looks wrong
- Check Canvas Scaler settings (Scale With Screen Size)
- Verify anchor settings on UI elements
- Ensure Layout Groups are properly configured

### Text not visible
- Import TextMesh Pro Essentials (Window > TextMesh Pro > Import TMP Essential Resources)
- Check text color contrasts with background
- Verify font size is appropriate

## Next Steps

1. **Style the UI**: Add images, icons, and custom fonts
2. **Add Animations**: Use Unity's Animator for smooth transitions
3. **Sound Effects**: Add button click sounds and race completion audio
4. **Leaderboard**: Extend UI to show global leaderboard
5. **Race Types**: Add dropdown to select different race types/tracks

## API Reference

### RaceUIManager Public Methods

```csharp
// Control panel visibility
void ShowRacePanel(bool show)
void ShowRewardsPanel(bool show)
void ShowResultPanel(bool show)

// Control race state
void SetRaceActive(bool active)
```

### RewardItemUI Public Methods

```csharp
// Set reward data to display
void SetRewardData(RewardData reward)
```

## Support

For detailed setup instructions, see `UI_SETUP_GUIDE.md`

For backend/server setup, see the `apex-racer-backend` repository
