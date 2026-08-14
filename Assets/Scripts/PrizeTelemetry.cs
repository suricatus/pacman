using UnityEngine;

public static class PrizeTelemetry
{
    public static void LogPrizeGranted(PrizeEvaluationResult result, int beforeCount, int afterCount, int totalRemaining, string cause)
    {
        if (!result.Success) return;

        Debug.Log($"[PrizeTelemetry] Granted: {result.PrizeName} (ID: {result.PrizeId}) | " +
                  $"Stock: {beforeCount} → {afterCount} | Total: {totalRemaining} | Cause: {cause}");
    }
}
