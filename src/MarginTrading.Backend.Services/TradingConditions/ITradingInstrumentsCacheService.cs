using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingInstrumentsCacheService
    {
        ITradingInstrument GetTradingInstrument(string tradingConditionId, string instrument);

        (decimal MarginInit, decimal MarginMaintenance) GetMargins(ITradingInstrument tradingInstrument,
            decimal volumeForCalculation, decimal marginRate, bool isWarnCheck = false);
    }
}
