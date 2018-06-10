using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface ICommissionService
    {
        void SetCommissionRates(string accountAssetId, Position order);
        decimal GetSwaps(IPosition order);
        decimal GetOvernightSwap(IPosition order, decimal swapRate);
    }
}
