namespace MarginTrading.Backend.Core
{
    public interface ICommissionService
    {
        void SetCommissionRates(string accountAssetId, Order order);
        decimal GetSwaps(IOrder order);
        decimal GetOvernightSwap(IOrder order, decimal swapRate);
    }
}
