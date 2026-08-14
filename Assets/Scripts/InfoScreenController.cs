using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Tools.PrizeManager.Services;
using TMPro;
using Zenject;

public class InfoScreenController : MonoBehaviour
{
    [System.Serializable]
    public struct Row
    {
        [Tooltip("PrizeId must match exactly as in PrizeConfig")]
        public string prizeId;
        
        public TMP_Text valueLabel;
    }
    
    [Header("Bindings")]
    [SerializeField] private Row[] rows;
    
    [SerializeField] private TMP_Text totalLabel;
    [SerializeField] private string numberFormat = "N0";
    
    [Header("Auto-refresh")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 2f;
    
    private Coroutine _loop;
    private IPrizeManagerService _prizeManager;

    [Inject]
    public void Construct(IPrizeManagerService prizeManager)
    {
        _prizeManager = prizeManager;
    }

    private void OnEnable()
    {
        Refresh();

        if (autoRefresh)
            _loop = StartCoroutine(AutoRefreshLoop());
    }

    private void OnDisable()
    {
        if (_loop != null)
            StopCoroutine(_loop);
        _loop = null;
    }

    private IEnumerator AutoRefreshLoop()
    {
        var wait = new WaitForSecondsRealtime(refreshInterval);
        while (enabled)
        {
            Refresh();
            yield return wait;
        }
    }

    public void Refresh()
    {
        if (_prizeManager == null)
            return;

        if (rows != null)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row.valueLabel == null || string.IsNullOrWhiteSpace(row.prizeId))
                    continue;

                var remaining = _prizeManager.GetRemainingPrizes(row.prizeId);
                row.valueLabel.text = remaining.ToString(numberFormat);
            }
        }

        if (totalLabel != null)
        {
            var allPrizes = _prizeManager.GetAllPrizes();
            int total = allPrizes.Sum(p => p.RemainingQuantity);
            totalLabel.text = total.ToString(numberFormat);
        }
    }
}
