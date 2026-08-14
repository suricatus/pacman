using System.Collections;
using TMPro;
using Tools.PrizeManager.Services;
using UnityEngine;
using Zenject;

public class PrizeLowStockMonitor : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Settings")]
    [SerializeField] private int lowStockThreshold = 10;
    [SerializeField] private float refreshInterval = 5f;
    [SerializeField] private string specificPrizeId = "";

    private IPrizeManagerService _prizeManager;
    private Coroutine _checkRoutine;

    [Inject]
    public void Construct(IPrizeManagerService prizeManager)
    {
        _prizeManager = prizeManager;
    }

    private void OnEnable()
    {
        if (panel != null)
            panel.SetActive(false);

        _checkRoutine = StartCoroutine(CheckStockRoutine());
    }

    private void OnDisable()
    {
        if (_checkRoutine != null)
        {
            StopCoroutine(_checkRoutine);
            _checkRoutine = null;
        }
    }

    private IEnumerator CheckStockRoutine()
    {
        var wait = new WaitForSecondsRealtime(refreshInterval);

        while (enabled)
        {
            CheckStock();
            yield return wait;
        }
    }

    public void CheckStock()
    {
        if (_prizeManager == null || panel == null || label == null)
            return;

        int remaining;
        string message;

        if (string.IsNullOrEmpty(specificPrizeId))
        {
            var totalAwarded = _prizeManager.GetTotalPrizesAwarded();
            var allPrizes = _prizeManager.GetAllPrizes();
            int totalRemaining = 0;
            
            foreach (var prize in allPrizes)
            {
                totalRemaining += prize.RemainingQuantity;
            }

            remaining = totalRemaining;
            message = $"Restam apenas {remaining} prêmios hoje.";
        }
        else
        {
            remaining = _prizeManager.GetRemainingPrizes(specificPrizeId);
            var prize = _prizeManager.GetPrize(specificPrizeId);
            string prizeName = prize != null ? prize.PrizeName : specificPrizeId;
            message = $"Restam apenas {remaining} {prizeName} hoje.";
        }

        bool isLowStock = remaining <= lowStockThreshold;
        panel.SetActive(isLowStock);

        if (isLowStock && label != null)
        {
            label.text = message;
        }
    }

    public void SetSpecificPrize(string prizeId)
    {
        specificPrizeId = prizeId;
        CheckStock();
    }
}
