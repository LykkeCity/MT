using System.Collections.Generic;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingInstrumentsCacheService
    {
        void InitCache(IEnumerable<ITradingInstrument> tradingInstruments);

        void UpdateCache(ITradingInstrument tradingInstrument);
        
        ITradingInstrument GetTradingInstrument(string tradingConditionId, string instrument);

        (decimal MarginInit, decimal MarginMaintenance) GetMarginRates(ITradingInstrument tradingInstrument,
            bool isWarnCheck = false);

        void RemoveFromCache(string tradingConditionId, string instrument);
    }
}
