using System;
using Tools.PrizeManager.Services;
using UnityEngine;
using Zenject;

public class PrizeManagerBootstrap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enablePrizes = true;
    
    private IPrizeManagerService _prizeManager;
    
    [Inject]
    public void Construct(IPrizeManagerService prizeManager)
    {
        _prizeManager = prizeManager;
    }

    private void Awake()
    {
        if (!enablePrizes)
            return;

        LogPrizeStatus();
    }

    private void LogPrizeStatus()
    {
        var prizes =  _prizeManager.GetAllPrizes();

        foreach (var prize in prizes)
        {
            Debug.Log($"- {prize.PrizeName}: {prize.RemainingQuantity}/{prize.TotalQuantity}");
        }
    }
}