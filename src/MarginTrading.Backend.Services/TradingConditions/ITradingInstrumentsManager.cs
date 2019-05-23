using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingInstrumentsManager
    {
        Task UpdateTradingInstrumentsCacheAsync(string id = null);
    }
}