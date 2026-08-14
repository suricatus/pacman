# ✅ Prize Manager Migration - COMPLETE

## Files Successfully Migrated

### ✅ 1. GameManager.cs
**Status**: Fully migrated to PrizeManager
- ✅ Removed `using Rewards;`
- ✅ Added `using Tools.PrizeManager.Models;`
- ✅ Injected `ScoreBasedPrizeEvaluator` via Zenject
- ✅ Replaced `RewardService.I.Evaluate()` with `await _prizeEvaluator.EvaluateAndAwardPrize(score)`
- ✅ Replaced `svc.TryForceItem()` with `_prizeEvaluator.TryForceItem()` and `ForceSpecificPrize()`
- ✅ Replaced `svc.Decrement()` - no longer needed (auto-decremented)
- ✅ Replaced `svc.RemainingForItem()` with `_prizeEvaluator.GetRemainingForPrize()`
- ✅ Replaced `CheckOnlyTopPrizeLeft()` custom method with `_prizeEvaluator.CheckOnlyTopPrizeLeft()`
- ✅ Updated all result properties (`itemId` → `PrizeId`, `sprite` → `PrizeSprite`)
- ✅ Made `PrepareVictoryPayload()` async: `PrepareVictoryPayloadAsync()`
- ✅ Created `PrizeTelemetry.LogPrizeGranted()` to replace `RewardTelemetry`

### ✅ 2. InfoScreenController.cs
**Status**: Fully migrated to PrizeManager
- ✅ Removed `using Rewards;`
- ✅ Added `using Tools.PrizeManager.Services;` and `using Zenject;`
- ✅ Injected `IPrizeManagerService` via Zenject
- ✅ Replaced `RewardService.I.RemainingForItem()` with `_prizeManager.GetRemainingPrizes()`
- ✅ Replaced `RewardService.I.TotalRemaining()` with LINQ sum over `_prizeManager.GetAllPrizes()`
- ✅ Removed event subscription to `OnConfigLoaded` (not needed)
- ✅ Updated field: `itemId` → `prizeId`

### ✅ 3. MainMenuController.cs
**Status**: Fully migrated to PrizeManager
- ✅ Removed `using Rewards;`
- ✅ Removed `[SerializeField] private RewardConfig rewardConfig;` field
- ✅ Removed `RewardService.I.LoadConfig(rewardConfig);` initialization
- Config is now loaded automatically via `PrizeManagerInstaller` in the scene

### ✅ 4. ScoreBasedPrizeEvaluator.cs
**Status**: Enhanced with complete API
- ✅ `EvaluateAndAwardPrize(score)` - Main evaluation method
- ✅ `ForceSpecificPrize(prizeId, reason)` - Force a specific prize
- ✅ `TryForceItem(prizeId, out result)` - Check & prepare forced prize
- ✅ `DecrementPrize(result)` - Award a prepared prize
- ✅ `CheckOnlyTopPrizeLeft()` - Check if only top prize remains
- ✅ `GetTotalRemaining()` - Get total remaining prizes
- ✅ `GetRemainingForPrize(prizeId)` - Get remaining for specific prize
- ✅ `GetPrizeInfo(prizeId)` - Get Prize object
- ✅ Fallback logic to other categories if prizes are out of stock
- ✅ Category-based prize distribution
- ✅ Special prize configuration (topPrizeId, zeroPrizeId)

### ✅ 5. PrizeTelemetry.cs
**Status**: Created
- Simple static logger for prize awarding events
- Replaces `RewardTelemetry.LogRewardGranted()`

---

## Files NOT Migrated (Will Be Disabled)

### ⚠️ EditItems.cs
**Status**: To be replaced by `PrizeEditController.cs`
- You should use the new `PrizeEditController` component instead
- Disable or delete `EditItems` from scenes after setting up the new component

### ⚠️ LowStockBanner.cs
**Status**: To be replaced by `PrizeLowStockMonitor.cs`
- You should use the new `PrizeLowStockMonitor` component instead
- Disable or delete `LowStockBanner` from scenes after setting up the new component

### ⚠️ RewardBootstrap.cs, RewardService.cs, etc.
**Status**: Old reward system files
- Can be deleted once migration is verified working
- Keep them temporarily in case you need to reference the old logic

---

## Next Steps to Complete Migration

### 1. Scene Setup - MainMenu Scene

Open the MainMenu scene and:

1. **Add PrizeManager to SceneContext**:
   - Find or create `/SceneContext` GameObject
   - Add component: Find the `PrizeManagerInstaller` script from the package
   - Assign your `PrizeConfig` ScriptableObject to the installer

2. **Add ScoreBasedPrizeEvaluator**:
   - Create a new GameObject called `PrizeEvaluator`
   - Add component: `ScoreBasedPrizeEvaluator`
   - Configure categories in the Inspector:
     ```
     Categories:
     - ID: 1, Name: "Top Tier", Min: 90%, Max: 100%, Prize IDs: [labubu]
     - ID: 2, Name: "High Tier", Min: 70%, Max: 89%, Prize IDs: [garrafa, item1]
     - ID: 3, Name: "Mid Tier", Min: 40%, Max: 69%, Prize IDs: [item2, item3]
     - ID: 4, Name: "Low Tier", Min: 0%, Max: 39%, Prize IDs: [item4, item5]
     ```
   - Set Special Prize IDs:
     - Top Prize ID: `labubu`
     - Zero Prize ID: `canetazero`
   - Set Max Score: `2770`

3. **Replace EditItems with PrizeEditController**:
   - Find GameObject with `EditItems` component
   - Disable the `EditItems` component
   - Add `PrizeEditController` component to same GameObject
   - Configure UI references in Inspector

4. **Replace LowStockBanner with PrizeLowStockMonitor**:
   - Find GameObject with `LowStockBanner` component
   - Disable the `LowStockBanner` component
   - Add `PrizeLowStockMonitor` component to same GameObject
   - Configure threshold and UI references

5. **Update InfoScreenController references**:
   - Find GameObjects with `InfoScreenController`
   - In Inspector, update Row fields:
     - Change field names from `itemId` to `prizeId`
     - Ensure prize IDs match exactly those in your `PrizeConfig`

### 2. Scene Setup - Pacman (Game) Scene

Open the Pacman/Game scene and:

1. **Add PrizeEvaluator** (if GameManager needs it during gameplay):
   - Create a new GameObject called `PrizeEvaluator`
   - Add component: `ScoreBasedPrizeEvaluator`
   - Configure the SAME categories as in MainMenu scene
   - OR: Make it a persistent singleton if you want to share config across scenes

2. **Ensure SceneContext has PrizeManager**:
   - If using Zenject SceneContext, add `PrizeManagerInstaller`
   - Assign same `PrizeConfig` as MainMenu

### 3. Scene Setup - Victory Scene

1. **Update Info Display** (if you have prize info here):
   - Update any `InfoScreenController` references
   - Change `itemId` → `prizeId` in Row configurations

2. **Add Prize Monitor** (optional):
   - Add `PrizeLowStockMonitor` if you want low stock warnings

### 4. Create PrizeConfig ScriptableObject

If you haven't already:

1. In Unity, go to: **Tools → Create PrizeConfig**
   - OR: **Assets → Create → Tools → Prize Manager → Config**

2. Configure your prizes:
   ```
   Prize List:
   - ID: "labubu", Name: "Labubu Figure", Icon: [sprite], Quantity: 10
   - ID: "garrafa", Name: "Water Bottle", Icon: [sprite], Quantity: 50
   - ID: "canetazero", Name: "Zero Pen", Icon: [sprite], Quantity: 100
   - [... add all your prizes ...]
   ```

3. Settings:
   - ✅ Persist Locally: true
   - Save Key: `prizes_state`
   - ✅ Allow Multiple Awards Per Prize: true
   - ✅ Auto Save On Award: true

### 5. Testing Checklist

After setup, test these scenarios:

- [ ] **Zero Score**: Should award `canetazero` if available
- [ ] **Perfect Win** (all pellets, no ghosts): Should award `labubu`
- [ ] **High Score Loss** (>2460): Should award `garrafa`
- [ ] **Normal Win**: Should award based on score category
- [ ] **Normal Loss**: Should award based on score category
- [ ] **Stock Depletion**: Verify fallback to other categories works
- [ ] **Low Stock Warning**: Verify banner appears at threshold
- [ ] **Prize Editing**: Test adding/removing stock in MainMenu
- [ ] **Persistence**: Close/reopen Unity, verify stock persists
- [ ] **Victory Screen**: Verify correct prize is shown
- [ ] **Google Sheets**: Verify prize name is sent correctly

---

## API Migration Reference

| Old RewardService API | New PrizeManager API |
|-----------------------|----------------------|
| `RewardService.I.Evaluate(score)` | `await _prizeEvaluator.EvaluateAndAwardPrize(score)` |
| `svc.Decrement(result)` | ~~Not needed~~ (auto-decremented on award) |
| `svc.TryForceItem(id, out res)` | `_prizeEvaluator.TryForceItem(id, out res)` |
| Force & decrement | `await _prizeEvaluator.ForceSpecificPrize(id, reason)` |
| `svc.RemainingForItem(id)` | `_prizeEvaluator.GetRemainingForPrize(id)` |
| `svc.TotalRemaining()` | `_prizeEvaluator.GetTotalRemaining()` |
| `svc.Config.categories[].items[]` | `_prizeManager.GetPrize(id)` or `GetPrizeInfo(id)` |
| `CheckOnlyTopPrizeLeft(svc, top, zero)` | `_prizeEvaluator.CheckOnlyTopPrizeLeft()` |
| `RewardService.I.LoadConfig(cfg)` | ~~Not needed~~ (loaded via installer) |
| `RewardService.I.OnConfigLoaded` | ~~Not needed~~ (no events) |
| `result.itemId` | `result.PrizeId` |
| `result.sprite` | `result.PrizeSprite` |
| `result.itemName` | `result.PrizeName` |
| `result.categoryId` | `result.CategoryId` |
| `result.categoryName` | `result.CategoryName` |

---

## Benefits of Migration

✅ **Cleaner Code**: No singleton pattern, uses proper dependency injection
✅ **Better Testability**: All dependencies are injected, easy to mock
✅ **Type Safety**: Strong typing with `PrizeEvaluationResult`
✅ **Async/Await**: Proper async handling with UniTask
✅ **No Manual Decrement**: Auto-decrements on award, prevents bugs
✅ **Flexible Categories**: Configure categories in Inspector, no code changes
✅ **Better Fallbacks**: Automatic fallback to other categories when out of stock
✅ **Persistence**: Built-in save/load with PrizeManager
✅ **Success Flag**: Know if prize awarding succeeded or failed
✅ **Extensible**: Easy to add new prize evaluation logic

---

## Troubleshooting

### "The type or namespace name 'PrizeManager' could not be found"

**Solution**: Make sure you have assembly definition files in the package:
1. `/Packages/com.suricatusgames.tools.prize-manager/Runtime/Tools.PrizeManager.asmdef`
2. `/Packages/com.suricatusgames.tools.prize-manager/Editor/Tools.PrizeManager.Editor.asmdef`

### "Could not find PrizeConfig type"

**Solution**: The package needs assembly definitions. See previous error.

### "NullReferenceException: _prizeEvaluator is null"

**Solutions**:
1. Make sure `ScoreBasedPrizeEvaluator` component exists in the scene
2. Make sure it's registered in Zenject (or make it a MonoBehaviour installer)
3. Alternative: Use `FindObjectOfType<ScoreBasedPrizeEvaluator>()` in Awake if not using DI

### "No prize category found for X%"

**Solution**: Make sure your categories cover 0% to 100%:
- Lowest category should have `minPercent: 0`
- Highest category should have `maxPercent: 100`
- No gaps between categories

### "Prize not found in config"

**Solution**: Make sure Prize IDs in:
- `ScoreBasedPrizeEvaluator` categories match exactly
- `InfoScreenController` rows match exactly
- `GameManager` special prize IDs (`labubu`, `garrafa`, `canetazero`) match exactly
- All IDs are case-sensitive!

---

## Rollback Plan (If Needed)

If you need to rollback:

1. Revert GameManager.cs to use `RewardService.I`
2. Revert InfoScreenController.cs to use `RewardService.I`
3. Revert MainMenuController.cs to use `RewardConfig`
4. Re-enable `EditItems` and `LowStockBanner` components
5. Keep the old Rewards folder intact until migration is stable

---

## Migration Complete! 🎉

All code using `RewardService.I.Evaluate` has been successfully migrated to PrizeManager.

**Next Action**: Follow the "Next Steps" section above to complete scene setup and testing.

