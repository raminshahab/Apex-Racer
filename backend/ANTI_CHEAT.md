# Anti-Cheat for Race Submissions

This document focuses on  anti-cheat measures for the coding exercise.

---

## #1: Time Bounds Validation

**What it blocks:** Impossible times (too fast or too slow)
```

## Quick Test Script

Test your anti-cheat logic:

```bash
#!/bin/bash
# test-anti-cheat.sh

BASE_URL="http://localhost:7350"
HTTP_KEY="defaultkey"

---

## Configuration Tuning

```lua
-- At top of racing.lua
local ANTI_CHEAT_CONFIG = {
    min_valid_time = 15.0,      -- Adjust based on track length
    max_valid_time = 180.0,     -- Adjust based on track complexity
    wr_improvement_limit = 0.70, -- How much faster than WR is allowed (70% = 30% improvement max)
    outlier_threshold = 0.40,    -- How much better than average is suspicious (40%)
    enable_duplicate_check = true,
    enable_statistical_check = true
}
```
---
