using System;

namespace MarginTrading.Core
{
    public interface ISwapCommissionService
    {
        void SetCommissions(string accountAssetId, string tradingConditionId, Order order);
        double GetSwapCount(DateTime startDate, DateTime endDate);
        double GetSwaps(string tradingConditionId, string accountId, string accountAssetId, string instrument, OrderDirection type, DateTime? openDate, DateTime? closeDate, double volume);
        double GetSwaps(IOrder order);
    }
}
