using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingInstrumnentsCacheService
    {
        ITradingInstrument GetTradingInstrument(string tradingConditionId, string instrument);
    }
}
