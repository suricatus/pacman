using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tools.PrizeManager.Models;
using Tools.PrizeManager.Services;
using UnityEngine;
using Zenject;

public class ScoreBasedPrizeEvaluator : MonoBehaviour
{
    [System.Serializable]
    public class PrizeCategory
    {
        public int id;
        public string name;
        public float minPercent;
        public float maxPercent;
        public List<string> prizeIds = new List<string>();
    }

    [Header("Score Configuration")]
    [SerializeField] private int maxScore = 2770;
    
    [Header("Categories (Ordered from Best to Worst)")]
    [SerializeField] private List<PrizeCategory> categories = new List<PrizeCategory>();

    [Header("Special Prize IDs")]
    [SerializeField] private string topPrizeId = "labubu";
    [SerializeField] private string zeroPrizeId = "canetazero";

    private IPrizeManagerService _prizeManager;

    [Inject]
    public void Construct(IPrizeManagerService prizeManager)
    {
        _prizeManager = prizeManager;
    }

    public async UniTask<PrizeEvaluationResult> EvaluateAndAwardPrize(int score)
    {
        float percent = (score / (float)Mathf.Max(1, maxScore)) * 100f;
        Debug.Log($"[ScoreBasedPrizeEvaluator] Score: {score}, Percent: {percent:F2}%");

        var category = GetCategoryForPercent(percent);
        if (category == null || category.prizeIds == null || category.prizeIds.Count == 0)
        {
            Debug.LogWarning($"No prize category found for {percent:F2}%");
            return PrizeEvaluationResult.Failed("No prize available for this score", percent);
        }

        var availablePrizes = category.prizeIds
            .Where(id => _prizeManager.GetRemainingPrizes(id) > 0)
            .ToList();

        if (availablePrizes.Count == 0)
        {
            var fallback = await TryFallbackToOtherCategory(category, percent);
            if (fallback.Success) return fallback;

            Debug.LogWarning($"No prizes available in category {category.name}");
            return PrizeEvaluationResult.Failed("No prizes remaining in this category", percent, category.id, category.name);
        }

        string selectedPrizeId = availablePrizes[UnityEngine.Random.Range(0, availablePrizes.Count)];
        var result = await _prizeManager.AwardPrizeAsync(selectedPrizeId);

        if (result.Success)
        {
            Debug.Log($"Awarded prize: {result.Prize.PrizeName}");
            return PrizeEvaluationResult.FromAwardResult(result, category.id, category.name, percent);
        }

        return PrizeEvaluationResult.Failed(result.Message, percent, category.id, category.name);
    }

    public async UniTask<PrizeEvaluationResult> ForceSpecificPrize(string prizeId, string reason = "forced")
    {
        var result = await _prizeManager.AwardPrizeAsync(prizeId);
        
        if (result.Success)
        {
            return PrizeEvaluationResult.FromAwardResult(result, -1, "Forced", -1f, reason);
        }

        return PrizeEvaluationResult.Failed(result.Message, -1f, -1, "Forced");
    }

    public bool TryForceItem(string prizeId, out PrizeEvaluationResult result)
    {
        if (_prizeManager.GetRemainingPrizes(prizeId) > 0)
        {
            var prize = _prizeManager.GetPrize(prizeId);
            result = new PrizeEvaluationResult
            {
                Success = true,
                CategoryId = -1,
                CategoryName = "Forced",
                PrizeId = prize.Id,
                PrizeName = prize.PrizeName,
                PrizeSprite = prize.PrizeIcon,
                PercentUsed = -1f,
                Message = $"Forced: {prize.PrizeName}"
            };
            return true;
        }

        result = PrizeEvaluationResult.Failed("Prize not available", -1f, -1, "Forced");
        return false;
    }

    public void DecrementPrize(PrizeEvaluationResult result)
    {
        if (string.IsNullOrEmpty(result.PrizeId)) return;
        _prizeManager.AwardPrizeAsync(result.PrizeId).Forget();
    }

    public bool CheckOnlyTopPrizeLeft()
    {
        var allPrizes = _prizeManager.GetAllPrizes();
        bool topHasStock = false;

        foreach (var prize in allPrizes)
        {
            if (prize.Id.Equals(zeroPrizeId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (prize.Id.Equals(topPrizeId, StringComparison.OrdinalIgnoreCase))
            {
                if (prize.RemainingQuantity > 0)
                    topHasStock = true;
            }
            else
            {
                if (prize.RemainingQuantity > 0)
                    return false;
            }
        }

        return topHasStock;
    }

    public int GetTotalRemaining()
    {
        var prizes = _prizeManager.GetAllPrizes();
        return prizes.Sum(p => p.RemainingQuantity);
    }

    public int GetRemainingForPrize(string prizeId)
    {
        return _prizeManager.GetRemainingPrizes(prizeId);
    }

    public Prize GetPrizeInfo(string prizeId)
    {
        return _prizeManager.GetPrize(prizeId);
    }

    public string GetTopPrizeId() => topPrizeId;
    public string GetZeroPrizeId() => zeroPrizeId;

    private PrizeCategory GetCategoryForPercent(float percent)
    {
        foreach (var category in categories.OrderByDescending(c => c.minPercent))
        {
            if (percent >= category.minPercent && percent <= category.maxPercent)
            {
                return category;
            }
        }

        return categories.LastOrDefault();
    }

    private async UniTask<PrizeEvaluationResult> TryFallbackToOtherCategory(PrizeCategory originalCategory, float percent)
    {
        int originalIndex = categories.IndexOf(originalCategory);
        if (originalIndex < 0) return PrizeEvaluationResult.Failed("No fallback", percent);

        for (int i = originalIndex + 1; i < categories.Count; i++)
        {
            var fallbackCategory = categories[i];
            var availablePrizes = fallbackCategory.prizeIds
                .Where(id => _prizeManager.GetRemainingPrizes(id) > 0)
                .ToList();

            if (availablePrizes.Count > 0)
            {
                string selectedPrizeId = availablePrizes[UnityEngine.Random.Range(0, availablePrizes.Count)];
                var result = await _prizeManager.AwardPrizeAsync(selectedPrizeId);

                if (result.Success)
                {
                    Debug.Log($"Fallback to category {fallbackCategory.name}, awarded: {result.Prize.PrizeName}");
                    return PrizeEvaluationResult.FromAwardResult(result, fallbackCategory.id, fallbackCategory.name, percent, "Fallback");
                }
            }
        }

        for (int i = originalIndex - 1; i >= 0; i--)
        {
            var fallbackCategory = categories[i];
            var availablePrizes = fallbackCategory.prizeIds
                .Where(id => _prizeManager.GetRemainingPrizes(id) > 0)
                .ToList();

            if (availablePrizes.Count > 0)
            {
                string selectedPrizeId = availablePrizes[UnityEngine.Random.Range(0, availablePrizes.Count)];
                var result = await _prizeManager.AwardPrizeAsync(selectedPrizeId);

                if (result.Success)
                {
                    Debug.Log($"Fallback to category {fallbackCategory.name}, awarded: {result.Prize.PrizeName}");
                    return PrizeEvaluationResult.FromAwardResult(result, fallbackCategory.id, fallbackCategory.name, percent, "Fallback");
                }
            }
        }

        return PrizeEvaluationResult.Failed("No fallback available", percent);
    }
}

[System.Serializable]
public struct PrizeEvaluationResult
{
    public bool Success;
    public int CategoryId;
    public string CategoryName;
    public string PrizeId;
    public string PrizeName;
    public Sprite PrizeSprite;
    public float PercentUsed;
    public string Message;

    public static PrizeEvaluationResult FromAwardResult(PrizeAwardResult award, int catId, string catName, float percent, string prefix = "")
    {
        return new PrizeEvaluationResult
        {
            Success = award.Success,
            CategoryId = catId,
            CategoryName = catName,
            PrizeId = award.Prize?.Id,
            PrizeName = award.Prize?.PrizeName,
            PrizeSprite = award.Prize?.PrizeIcon,
            PercentUsed = percent,
            Message = string.IsNullOrEmpty(prefix) ? award.Message : $"{prefix}: {award.Message}"
        };
    }

    public static PrizeEvaluationResult Failed(string message, float percent, int catId = -1, string catName = "")
    {
        return new PrizeEvaluationResult
        {
            Success = false,
            CategoryId = catId,
            CategoryName = catName,
            PrizeId = null,
            PrizeName = null,
            PrizeSprite = null,
            PercentUsed = percent,
            Message = message
        };
    }
}