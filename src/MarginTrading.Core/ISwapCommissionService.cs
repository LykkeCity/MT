using System;

namespace MarginTrading.Core
{
    public interface ISwapCommissionService
    {
        void SetCommissions(string accountAssetId, string tradingConditionId, Order order);
        decimal GetSwapCount(DateTime startDate, DateTime endDate);
        decimal GetSwaps(string tradingConditionId, string accountId, string accountAssetId, string instrument, OrderDirection type, DateTime? openDate, DateTime? closeDate, decimal volume);
        decimal GetSwaps(IOrder order);
    }
}
