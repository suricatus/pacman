# Prize Manager Migration Guide

## Files Using RewardService.I That Need Updates

### 1. GameManager.cs
**Location**: `/Assets/Scripts/GameManager.cs`

**Changes Required**:
```csharp
// OLD - Remove these lines:
if (RewardService.I != null)
    RewardService.I.TopPrizeItemId = labubuItemId;

var svc = RewardService.I;
var res = svc.Evaluate(eval);
svc.Decrement(res);

// NEW - Add injection and use ScoreBasedPrizeEvaluator:
[Inject] private ScoreBasedPrizeEvaluator _prizeEvaluator;

// Replace Evaluate calls with:
var res = await _prizeEvaluator.EvaluateAndAwardPrize(score);
// Note: PrizeManager already decrements, no need to call Decrement

// Replace TryForceItem:
if (_prizeEvaluator.TryForceItem(prizeId, out var result))
{
    var award = await _prizeEvaluator.ForceSpecificPrize(prizeId, "reason");
}

// Replace GetPrize info:
var prize = _prizeEvaluator.GetPrizeInfo(prizeId);
```

**Result Structure Change**:
```csharp
// OLD RewardService.RewardResult:
result.categoryId (int)
result.categoryName (string)
result.itemId (string)
result.itemName (string)
result.sprite (Sprite)
result.percentUsed (float)

// NEW PrizeEvaluationResult:
result.CategoryId (int)
result.CategoryName (string)
result.PrizeId (string)
result.PrizeName (string)
result.PrizeSprite (Sprite)
result.PercentUsed (float)
result.Success (bool) // NEW!
result.Message (string) // NEW!
```

### 2. EditItems.cs
**Location**: `/Assets/Scripts/EditItems.cs`

**Status**: REPLACED by `PrizeEditController.cs`
- Do NOT update this file
- It will be disabled after PrizeEditController is configured

### 3. InfoScreenController.cs
**Location**: `/Assets/Scripts/InfoScreenController.cs`

**Changes Required**:
```csharp
// OLD:
using Rewards;
if (RewardService.I != null)
    RewardService.I.OnConfigLoaded += OnConfigLoaded;
var remaining = svc.RemainingForItem(row.itemId);

// NEW:
using Tools.PrizeManager.Services;
[Inject] private IPrizeManagerService _prizeManager;

// Replace all RewardService.I calls with _prizeManager
private void RefreshUI()
{
    var remaining = _prizeManager.GetRemainingPrizes(row.prizeId);
    // totalLabel calculation:
    var prizes = _prizeManager.GetAllPrizes();
    int total = prizes.Sum(p => p.RemainingQuantity);
}
```

### 4. MainMenuController.cs
**Location**: `/Assets/Scripts/MainMenuController.cs`

**Changes Required**:
```csharp
// OLD:
[SerializeField] private RewardConfig rewardConfig;
RewardService.I.LoadConfig(rewardConfig);

// NEW:
// Remove rewardConfig field entirely
// PrizeManager is loaded via installer in scene
// No initialization needed!
```

### 5. LowStockBanner.cs
**Location**: `/Assets/Scripts/Rewards/LowStockBanner.cs`

**Status**: REPLACED by `PrizeLowStockMonitor.cs`
- Do NOT update this file
- It will be disabled after PrizeLowStockMonitor is configured

---

## Quick Reference: RewardService → PrizeManager API Mapping

| RewardService.I Method | PrizeManager Equivalent |
|------------------------|-------------------------|
| `Evaluate(score)` | `await _prizeEvaluator.EvaluateAndAwardPrize(score)` |
| `Decrement(result)` | Not needed (auto-decremented on award) |
| `TryForceItem(id, out result)` | `_prizeEvaluator.TryForceItem(id, out result)` |
| `RemainingForItem(id)` | `_prizeManager.GetRemainingPrizes(id)` |
| `TotalRemaining()` | `_prizeEvaluator.GetTotalRemaining()` |
| `GetPrize(id)` | `_prizeManager.GetPrize(id)` or `_prizeEvaluator.GetPrizeInfo(id)` |
| `TopPrizeItemId = "x"` | Configured in ScoreBasedPrizeEvaluator Inspector |
| `OnConfigLoaded` event | Not needed (config loaded via installer) |
| `LoadConfig(config)` | Not needed (auto-loaded via installer) |

---

## Namespace Changes

| Old | New |
|-----|-----|
| `using Rewards;` | `using Tools.PrizeManager.Models;`<br>`using Tools.PrizeManager.Services;` |
| `RewardService.RewardResult` | `PrizeEvaluationResult` |
| `RewardConfig` | `PrizeConfig` |

---

## Next Steps

1. ✅ **Created**: `ScoreBasedPrizeEvaluator.cs` - Complete
2. ⏳ **Update**: `GameManager.cs` - See detailed changes below
3. ⏳ **Update**: `InfoScreenController.cs` - Simple API swap
4. ⏳ **Update**: `MainMenuController.cs` - Remove RewardService init
5. ✅ **Create**: `PrizeEditController.cs` - Ready to use
6. ✅ **Create**: `PrizeLowStockMonitor.cs` - Ready to use

---

## Scene Setup Checklist

### MainMenu Scene:
1. [ ] Add `PrizeManagerInstaller` GameObject to `/SceneContext`
2. [ ] Assign `PrizeConfig` to the installer
3. [ ] Add `ScoreBasedPrizeEvaluator` component to a GameObject
4. [ ] Configure categories in ScoreBasedPrizeEvaluator Inspector
5. [ ] Replace `EditItems` with `PrizeEditController`
6. [ ] Replace `LowStockBanner` with `PrizeLowStockMonitor`

### Game Scene (Pacman):
1. [ ] Add `ScoreBasedPrizeEvaluator` component
2. [ ] Configure the same categories as MainMenu

### Victory Scene:
1. [ ] Add `PrizeLowStockMonitor` if needed

---

## Testing Plan

After migration:
1. Test prize awarding with different scores
2. Verify stock decrements correctly
3. Test low stock warnings
4. Test prize editing in MainMenu
5. Verify persistence (close/reopen Unity)
6. Test special cases (zero score, perfect score, labubu logic)

