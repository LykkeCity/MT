using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface ICommissionService
    {
        void SetCommissionRates(string accountAssetId, Position order);
        decimal GetSwaps(Position order);
        decimal GetOvernightSwap(Position order, decimal swapRate);
    }
}
