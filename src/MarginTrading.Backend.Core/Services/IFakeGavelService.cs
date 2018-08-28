using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface IFakeGavelService
    {
        void GetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume);
        
        void ExecuteSpecialLiquidationOrder(string operationId, string instrument, decimal volume, decimal price);
    }
}