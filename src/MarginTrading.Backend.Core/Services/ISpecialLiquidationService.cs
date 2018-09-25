using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISpecialLiquidationService
    {
        void FakeGetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume);
        
        void ExecuteSpecialLiquidationOrder(string operationId, string instrument, decimal volume, decimal price,
            string externalProviderId, bool executeByApi);
    }
}