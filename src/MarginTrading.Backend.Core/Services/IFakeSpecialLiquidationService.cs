namespace MarginTrading.Backend.Core.Services
{
    public interface IFakeSpecialLiquidationService
    {
        void GetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume);
        
        void ExecuteSpecialLiquidationOrder(string operationId, string instrument, decimal volume, decimal price);
    }
}