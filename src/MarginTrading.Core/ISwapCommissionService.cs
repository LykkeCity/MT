namespace MarginTrading.Core
{
    public interface ICommissionService
    {
        void SetCommissionRates(string accountAssetId, string tradingConditionId, Order order);
        decimal GetSwaps(IOrder order);
    }
}
