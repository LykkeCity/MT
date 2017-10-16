using System;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.Services
{
    public interface IPriceCalcSettingsService
    {
        bool IsStepEnabled(OrderbookGeneratorStepEnum step, string assetPairId);
        string GetPresetPrimaryExchange(string assetPairId);
        decimal GetVolumeMultiplier(string assetPairId, string exchangeName);
        TimeSpan GetOrderbookAgeThreshold(string assetPairId, string exchangeName, DateTime now);
        TimeSpan GetMaxOutlierEventsAge();
        int GetMaxOutlierSequenceLength();
        TimeSpan GetMaxOutlierSequenceAge();
    }
}
