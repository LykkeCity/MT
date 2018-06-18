using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingInstrumentsCacheService
    {
        ITradingInstrument GetTradingInstrument(string tradingConditionId, string instrument);
    }
}
