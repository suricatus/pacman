using System;
using System.Collections.Generic;
using TMPro;
using Tools.PrizeManager.Services;
using UnityEngine;
using Zenject;

public class PrizeEditController : MonoBehaviour
{
    [Serializable]
    public struct PrizeRow
    {
        public string prizeId;
        public TMP_Text totalText;
        public TMP_InputField todayInput;
        public TMP_InputField totalInput;
    }

    [Header("Prize Rows")]
    [SerializeField] private PrizeRow[] prizeRows;

    [Header("Summary UI")]
    [SerializeField] private TMP_Text totalTodayLabel;
    [SerializeField] private TMP_Text dateLabel;
    [SerializeField] private TMP_Text totalOverallLabel;

    private IPrizeManagerService _prizeManager;

    [Inject]
    public void Construct(IPrizeManagerService prizeManager)
    {
        _prizeManager = prizeManager;
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_prizeManager == null) return;

        int totalToday = 0;
        int totalOverall = 0;

        foreach (var row in prizeRows)
        {
            if (string.IsNullOrEmpty(row.prizeId)) continue;

            int remaining = _prizeManager.GetRemainingPrizes(row.prizeId);
            var prize = _prizeManager.GetPrize(row.prizeId);

            if (row.totalText != null)
            {
                row.totalText.text = remaining.ToString();
            }

            if (row.totalInput != null)
            {
                row.totalInput.text = remaining.ToString();
            }

            totalToday += remaining;
            totalOverall += remaining;
        }

        if (totalTodayLabel != null)
            totalTodayLabel.text = totalToday.ToString();

        if (totalOverallLabel != null)
            totalOverallLabel.text = totalOverall.ToString();

        if (dateLabel != null)
            dateLabel.text = DateTime.Today.ToString("d");
    }

    public void ApplyChanges()
    {
        foreach (var row in prizeRows)
        {
            if (string.IsNullOrEmpty(row.prizeId)) continue;

            if (row.totalInput != null && int.TryParse(row.totalInput.text, out int newQuantity))
            {
                _prizeManager.UpdatePrizeQuantity(row.prizeId, newQuantity);
            }
        }

        RefreshUI();
    }

    public void ResetAll()
    {
        _prizeManager.ResetAllPrizes();
        RefreshUI();
    }
}
