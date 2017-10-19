using System;
using System.Collections.Immutable;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IPriceCalcSettingsService
    {
        bool IsStepEnabled(OrderbookGeneratorStepEnum step, string assetPairId);
        string GetPresetPrimaryExchange(string assetPairId);
        decimal GetVolumeMultiplier(string assetPairId, string exchangeName);
        TimeSpan GetOrderbookAgeThreshold(string assetPairId, string exchangeName, DateTime now);
        RepeatedOutliersParams GetRepeatedOutliersParams(string assetPairId);
        decimal GetOutlierThreshold(string assetPairId);
        ImmutableDictionary<string, decimal> GetHedgingPreferences(string assetPairId);
    }
}
