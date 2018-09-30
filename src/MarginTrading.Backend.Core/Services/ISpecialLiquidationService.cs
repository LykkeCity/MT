using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISpecialLiquidationService
    {
        void FakeGetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume);
    }
}