using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;
using MarginTrading.MarketMaker.Models.Api;

namespace MarginTrading.MarketMaker.Services
{
    public interface IPriceCalcSettingsService
    {
        bool IsStepEnabled(OrderbookGeneratorStepEnum step, string assetPairId);
        string GetPresetPrimaryExchange(string assetPairId);
        decimal GetVolumeMultiplier(string assetPairId, string exchangeName);
        TimeSpan GetOrderbookOutdatingThreshold(string assetPairId, string exchangeName, DateTime now);
        RepeatedOutliersParams GetRepeatedOutliersParams(string assetPairId);
        decimal GetOutlierThreshold(string assetPairId);
        ImmutableDictionary<string, decimal> GetHedgingPreferences(string assetPairId);
        Task<IReadOnlyList<AssetPairExtPriceSettingsModel>> GetAllAsync(string assetPairId = null);
        Task Set(AssetPairExtPriceSettingsModel model);
        (decimal Bid, decimal Ask) GetPriceMarkups(string assetPairId);
    }
}
